using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RezSaaS.Modules.Admin.Domain;

namespace RezSaaS.IdentityIntegrationTests;

public sealed class AdminControlPlaneApiTests : IClassFixture<IdentityApiFixture>
{
    private readonly IdentityApiFixture fixture;

    public AdminControlPlaneApiTests(IdentityApiFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task PlatformAdminBootstrapEndpointCreatesInitialAdminOnce()
    {
        HttpResponseMessage response = await fixture.Client.PostAsJsonAsync(
            "/api/admin/bootstrap/platform-admin",
            new
            {
                email = $"platform-admin-http-{Guid.NewGuid():N}@example.test",
                password = "RezSaaS!PlatformAdmin1234",
                bootstrapToken = "test-bootstrap-token",
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, await fixture.GetPlatformRoleCountAsync());
        Assert.Equal(1, await fixture.GetPlatformAdminAssignmentCountAsync());
        Assert.Equal(1, await fixture.GetIdentityAuditLogCountAsync());

        HttpResponseMessage duplicateResponse = await fixture.Client.PostAsJsonAsync(
            "/api/admin/bootstrap/platform-admin",
            new
            {
                email = $"platform-admin-http-second-{Guid.NewGuid():N}@example.test",
                password = "RezSaaS!PlatformAdmin1234",
                bootstrapToken = "test-bootstrap-token",
            });

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        Assert.Equal(1, await fixture.GetPlatformAdminAssignmentCountAsync());
    }

    [Fact]
    public async Task TenantProvisioningRequiresAuthentication()
    {
        HttpResponseMessage response = await fixture.Client.PostAsJsonAsync(
            "/api/admin/tenants",
            new
            {
                slug = $"tenant-{Guid.NewGuid():N}"[..18],
                displayName = "Tenant Demo",
                ownerUserAccountId = Guid.CreateVersion7(),
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        HttpResponseMessage suspendResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/admin/tenants/{Guid.CreateVersion7()}/suspend",
            new { reason = "Unauthorized lifecycle attempt" });
        HttpResponseMessage closeResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/admin/tenants/{Guid.CreateVersion7()}/close",
            new { reason = "Unauthorized lifecycle attempt" });
        HttpResponseMessage reactivateResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/admin/tenants/{Guid.CreateVersion7()}/reactivate",
            new { reason = "Unauthorized lifecycle attempt" });

        Assert.Equal(HttpStatusCode.Unauthorized, suspendResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, closeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, reactivateResponse.StatusCode);

        HttpResponseMessage abuseEventsResponse = await fixture.Client.GetAsync(
            "/api/admin/abuse/events");
        HttpResponseMessage abuseReportsResponse = await fixture.Client.GetAsync(
            "/api/admin/abuse/reports");
        HttpResponseMessage reviewAbuseReportResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/admin/abuse/reports/{Guid.CreateVersion7()}/confirm",
            new { reason = "Unauthorized review attempt" });
        HttpResponseMessage sanctionResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/admin/abuse/users/{Guid.CreateVersion7()}/sanctions",
            new
            {
                type = "Cooldown",
                reason = "Unauthorized sanction attempt",
                endsAtUtc = DateTimeOffset.UtcNow.AddHours(1),
            });

        Assert.Equal(HttpStatusCode.Unauthorized, abuseEventsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, abuseReportsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, reviewAbuseReportResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, sanctionResponse.StatusCode);
    }

    [Fact]
    public async Task StepUpPlatformAdminCanProvisionTenantWithBusinessOwner()
    {
        string ownerEmail = $"tenant-owner-{Guid.NewGuid():N}@example.test";
        const string password = "RezSaaS!Auth1234";
        HttpResponseMessage registration = await fixture.Client.PostAsJsonAsync(
            "/api/auth/register",
            new { email = ownerEmail, password });
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);
        Guid ownerUserAccountId = await fixture.GetUserAccountIdAsync(ownerEmail);

        using HttpClient adminClient =
            fixture.CreatePlatformAdminStepUpClient(Guid.CreateVersion7());
        string slug = $"tenant-{Guid.NewGuid():N}"[..18];
        HttpResponseMessage response = await adminClient.PostAsJsonAsync(
            "/api/admin/tenants",
            new
            {
                slug,
                displayName = "Tenant Demo",
                ownerUserAccountId,
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using JsonDocument body =
            JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Guid tenantId = body.RootElement.GetProperty("tenantId").GetGuid();
        Assert.NotEqual(Guid.Empty, tenantId);
        Assert.Equal(slug, body.RootElement.GetProperty("slug").GetString());
        Assert.Equal(ownerUserAccountId, body.RootElement.GetProperty("ownerUserAccountId").GetGuid());
        Assert.True(await fixture.GetTenantCountAsync() >= 1);
        Assert.True(await fixture.GetTenantAuditLogCountAsync() >= 1);
        Assert.True(await fixture.HasBusinessOwnerMembershipAsync(tenantId, ownerUserAccountId));

        HttpResponseMessage duplicateResponse = await adminClient.PostAsJsonAsync(
            "/api/admin/tenants",
            new
            {
                slug = slug.ToUpperInvariant(),
                displayName = "Tenant Demo Copy",
                ownerUserAccountId,
            });

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task StepUpPlatformAdminCanListDetailAndManageTenantMemberships()
    {
        const string password = "RezSaaS!Auth1234";
        string ownerEmail = $"tenant-owner-manage-{Guid.NewGuid():N}@example.test";
        string managerEmail = $"tenant-manager-{Guid.NewGuid():N}@example.test";
        HttpResponseMessage ownerRegistration = await fixture.Client.PostAsJsonAsync(
            "/api/auth/register",
            new { email = ownerEmail, password });
        HttpResponseMessage managerRegistration = await fixture.Client.PostAsJsonAsync(
            "/api/auth/register",
            new { email = managerEmail, password });
        Assert.Equal(HttpStatusCode.OK, ownerRegistration.StatusCode);
        Assert.Equal(HttpStatusCode.OK, managerRegistration.StatusCode);
        Guid ownerUserAccountId = await fixture.GetUserAccountIdAsync(ownerEmail);
        Guid managerUserAccountId = await fixture.GetUserAccountIdAsync(managerEmail);

        using HttpClient adminClient =
            fixture.CreatePlatformAdminStepUpClient(Guid.CreateVersion7());
        string slug = $"tenant-{Guid.NewGuid():N}"[..18];
        Guid tenantId = await ProvisionTenantAsync(
            adminClient,
            slug,
            "Tenant Manage Demo",
            ownerUserAccountId);

        HttpResponseMessage listResponse = await adminClient.GetAsync(
            $"/api/admin/tenants?search={slug}&status=Active");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        using JsonDocument listBody =
            JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        JsonElement listedTenant = listBody.RootElement
            .GetProperty("tenants")
            .EnumerateArray()
            .Single(entity => entity.GetProperty("tenantId").GetGuid() == tenantId);
        Assert.Equal(slug, listedTenant.GetProperty("slug").GetString());
        Assert.Equal(1, listedTenant.GetProperty("activeMembershipCount").GetInt32());

        HttpResponseMessage detailResponse = await adminClient.GetAsync(
            $"/api/admin/tenants/{tenantId}");

        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);

        using JsonDocument detailBody =
            JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync());
        Guid ownerMembershipId = detailBody.RootElement
            .GetProperty("memberships")
            .EnumerateArray()
            .Single(entity => entity.GetProperty("userAccountId").GetGuid() == ownerUserAccountId)
            .GetProperty("membershipId")
            .GetGuid();

        Guid branchId = Guid.CreateVersion7();
        HttpResponseMessage addMembershipResponse = await adminClient.PostAsJsonAsync(
            $"/api/admin/tenants/{tenantId}/memberships",
            new
            {
                userAccountId = managerUserAccountId,
                role = "BranchManager",
                branchId,
            });

        string addMembershipContent = await addMembershipResponse.Content.ReadAsStringAsync();
        Assert.True(
            addMembershipResponse.StatusCode == HttpStatusCode.Created,
            addMembershipContent);

        using JsonDocument addMembershipBody =
            JsonDocument.Parse(addMembershipContent);
        Guid managerMembershipId = addMembershipBody.RootElement.GetProperty("membershipId").GetGuid();
        Assert.Equal("BranchManager", addMembershipBody.RootElement.GetProperty("role").GetString());
        Assert.Equal("Active", addMembershipBody.RootElement.GetProperty("status").GetString());

        HttpResponseMessage suspendResponse = await adminClient.PostAsync(
            $"/api/admin/tenants/{tenantId}/memberships/{managerMembershipId}/suspend",
            content: null);

        Assert.Equal(HttpStatusCode.OK, suspendResponse.StatusCode);

        using JsonDocument suspendBody =
            JsonDocument.Parse(await suspendResponse.Content.ReadAsStringAsync());
        Assert.Equal("Suspended", suspendBody.RootElement.GetProperty("status").GetString());

        HttpResponseMessage revokeResponse = await adminClient.PostAsync(
            $"/api/admin/tenants/{tenantId}/memberships/{managerMembershipId}/revoke",
            content: null);

        Assert.Equal(HttpStatusCode.OK, revokeResponse.StatusCode);

        using JsonDocument revokeBody =
            JsonDocument.Parse(await revokeResponse.Content.ReadAsStringAsync());
        Assert.Equal("Revoked", revokeBody.RootElement.GetProperty("status").GetString());

        HttpResponseMessage suspendRevokedResponse = await adminClient.PostAsync(
            $"/api/admin/tenants/{tenantId}/memberships/{managerMembershipId}/suspend",
            content: null);

        Assert.Equal(HttpStatusCode.Conflict, suspendRevokedResponse.StatusCode);

        HttpResponseMessage revokeLastOwnerResponse = await adminClient.PostAsync(
            $"/api/admin/tenants/{tenantId}/memberships/{ownerMembershipId}/revoke",
            content: null);

        Assert.Equal(HttpStatusCode.Conflict, revokeLastOwnerResponse.StatusCode);
    }

    [Fact]
    public async Task StepUpPlatformAdminCanSuspendAndCloseTenant()
    {
        const string password = "RezSaaS!Auth1234";
        string ownerEmail = $"tenant-owner-lifecycle-{Guid.NewGuid():N}@example.test";
        HttpResponseMessage registration = await fixture.Client.PostAsJsonAsync(
            "/api/auth/register",
            new { email = ownerEmail, password });
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);
        Guid ownerUserAccountId = await fixture.GetUserAccountIdAsync(ownerEmail);
        using HttpClient adminClient =
            fixture.CreatePlatformAdminStepUpClient(Guid.CreateVersion7());
        Guid tenantId = await ProvisionTenantAsync(
            adminClient,
            $"tenant-{Guid.NewGuid():N}"[..18],
            "Tenant Lifecycle Demo",
            ownerUserAccountId);

        HttpResponseMessage invalidResponse = await adminClient.PostAsJsonAsync(
            $"/api/admin/tenants/{tenantId}/suspend",
            new { reason = string.Empty });
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);

        HttpResponseMessage suspendResponse = await adminClient.PostAsJsonAsync(
            $"/api/admin/tenants/{tenantId}/suspend",
            new { reason = "Operational investigation" });
        Assert.Equal(HttpStatusCode.OK, suspendResponse.StatusCode);

        using JsonDocument suspendBody =
            JsonDocument.Parse(await suspendResponse.Content.ReadAsStringAsync());
        Assert.Equal("Suspended", suspendBody.RootElement.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.String, suspendBody.RootElement.GetProperty("suspendedAtUtc").ValueKind);

        HttpResponseMessage duplicateSuspendResponse = await adminClient.PostAsJsonAsync(
            $"/api/admin/tenants/{tenantId}/suspend",
            new { reason = "Retry" });
        Assert.Equal(HttpStatusCode.OK, duplicateSuspendResponse.StatusCode);

        HttpResponseMessage reactivateResponse = await adminClient.PostAsJsonAsync(
            $"/api/admin/tenants/{tenantId}/reactivate",
            new { reason = "Investigation completed" });
        Assert.Equal(HttpStatusCode.OK, reactivateResponse.StatusCode);

        using JsonDocument reactivateBody =
            JsonDocument.Parse(await reactivateResponse.Content.ReadAsStringAsync());
        Assert.Equal("Active", reactivateBody.RootElement.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, reactivateBody.RootElement.GetProperty("suspendedAtUtc").ValueKind);

        HttpResponseMessage closeResponse = await adminClient.PostAsJsonAsync(
            $"/api/admin/tenants/{tenantId}/close",
            new { reason = "Closure approved" });
        Assert.Equal(HttpStatusCode.OK, closeResponse.StatusCode);

        using JsonDocument closeBody =
            JsonDocument.Parse(await closeResponse.Content.ReadAsStringAsync());
        Assert.Equal("Closed", closeBody.RootElement.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.String, closeBody.RootElement.GetProperty("closedAtUtc").ValueKind);

        HttpResponseMessage suspendClosedResponse = await adminClient.PostAsJsonAsync(
            $"/api/admin/tenants/{tenantId}/suspend",
            new { reason = "Invalid reopening attempt" });
        HttpResponseMessage reactivateClosedResponse = await adminClient.PostAsJsonAsync(
            $"/api/admin/tenants/{tenantId}/reactivate",
            new { reason = "Invalid reopening attempt" });
        Assert.Equal(HttpStatusCode.Conflict, suspendClosedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, reactivateClosedResponse.StatusCode);
    }

    [Fact]
    public async Task StepUpPlatformAdminCanInspectAbuseAndApplyBookingCooldown()
    {
        string email = $"abuse-customer-{Guid.NewGuid():N}@example.test";
        const string password = "RezSaaS!Auth1234";
        HttpResponseMessage registration = await fixture.Client.PostAsJsonAsync(
            "/api/auth/register",
            new { email, password });
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);
        Guid userAccountId = await fixture.GetUserAccountIdAsync(email);
        PublicBusinessProfileSeed seed =
            await fixture.SeedPublicBusinessProfileAsync(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(26)));
        await fixture.SeedAbuseEventAsync(
            seed.TenantId,
            userAccountId,
            "booking.pending_limit_exceeded",
            AbuseEventSeverity.High);
        using HttpClient adminClient =
            fixture.CreatePlatformAdminStepUpClient(Guid.CreateVersion7());

        HttpResponseMessage eventsResponse = await adminClient.GetAsync(
            $"/api/admin/abuse/events?userAccountId={userAccountId}&severity=High");
        Assert.Equal(HttpStatusCode.OK, eventsResponse.StatusCode);

        using JsonDocument eventsBody =
            JsonDocument.Parse(await eventsResponse.Content.ReadAsStringAsync());
        JsonElement abuseEvent = eventsBody.RootElement
            .GetProperty("events")
            .EnumerateArray()
            .Single();
        Assert.Equal("High", abuseEvent.GetProperty("severity").GetString());
        Assert.Equal(userAccountId, abuseEvent.GetProperty("userAccountId").GetGuid());

        HttpResponseMessage sanctionResponse = await adminClient.PostAsJsonAsync(
            $"/api/admin/abuse/users/{userAccountId}/sanctions",
            new
            {
                type = "Cooldown",
                reason = "Repeated slot spam",
                endsAtUtc = DateTimeOffset.UtcNow.AddHours(2),
            });
        Assert.Equal(HttpStatusCode.Created, sanctionResponse.StatusCode);

        using JsonDocument sanctionBody =
            JsonDocument.Parse(await sanctionResponse.Content.ReadAsStringAsync());
        Guid sanctionId = sanctionBody.RootElement.GetProperty("sanctionId").GetGuid();
        Assert.Equal("Cooldown", sanctionBody.RootElement.GetProperty("type").GetString());
        Assert.True(sanctionBody.RootElement.GetProperty("isActive").GetBoolean());

        HttpResponseMessage duplicateSanctionResponse = await adminClient.PostAsJsonAsync(
            $"/api/admin/abuse/users/{userAccountId}/sanctions",
            new
            {
                type = "TemporaryBan",
                reason = "Duplicate blocking sanction",
                endsAtUtc = DateTimeOffset.UtcNow.AddHours(48),
            });
        Assert.Equal(HttpStatusCode.Conflict, duplicateSanctionResponse.StatusCode);

        HttpResponseMessage permanentClosureResponse = await adminClient.PostAsJsonAsync(
            $"/api/admin/abuse/users/{userAccountId}/sanctions",
            new
            {
                type = "PermanentClosure",
                reason = "Requires manual account closure workflow",
                endsAtUtc = (DateTimeOffset?)null,
            });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, permanentClosureResponse.StatusCode);

        HttpResponseMessage overviewResponse = await adminClient.GetAsync(
            $"/api/admin/abuse/users/{userAccountId}");
        Assert.Equal(HttpStatusCode.OK, overviewResponse.StatusCode);

        using JsonDocument overviewBody =
            JsonDocument.Parse(await overviewResponse.Content.ReadAsStringAsync());
        Assert.Single(overviewBody.RootElement.GetProperty("events").EnumerateArray());
        Assert.Single(overviewBody.RootElement.GetProperty("sanctions").EnumerateArray());

        HttpResponseMessage login = await fixture.Client.PostAsJsonAsync(
            "/api/auth/login?useCookies=false",
            new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        using JsonDocument loginBody =
            JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        string accessToken = loginBody.RootElement.GetProperty("accessToken").GetString()!;
        using HttpRequestMessage bookingRequest = new(
            HttpMethod.Post,
            $"/api/public/businesses/{seed.Slug}/appointment-requests");
        bookingRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        bookingRequest.Content = JsonContent.Create(
            new
            {
                branchSlug = seed.BranchSlug,
                serviceVariantIds = new[] { seed.ServiceVariantId },
                staffMemberId = seed.StaffMemberId,
                resourceId = seed.ResourceId,
                startUtc = seed.AvailableSlotStartUtc,
            });

        HttpResponseMessage bookingResponse = await fixture.Client.SendAsync(bookingRequest);

        Assert.Equal(HttpStatusCode.Forbidden, bookingResponse.StatusCode);

        HttpResponseMessage revokeResponse = await adminClient.PostAsJsonAsync(
            $"/api/admin/abuse/users/{userAccountId}/sanctions/{sanctionId}/revoke",
            new { reason = "Manual review cleared the restriction" });
        Assert.Equal(HttpStatusCode.OK, revokeResponse.StatusCode);

        using JsonDocument revokeBody =
            JsonDocument.Parse(await revokeResponse.Content.ReadAsStringAsync());
        Assert.False(revokeBody.RootElement.GetProperty("isActive").GetBoolean());
        Assert.Equal(
            JsonValueKind.String,
            revokeBody.RootElement.GetProperty("revokedAtUtc").ValueKind);

        using HttpRequestMessage bookingAfterRevokeRequest = new(
            HttpMethod.Post,
            $"/api/public/businesses/{seed.Slug}/appointment-requests");
        bookingAfterRevokeRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        bookingAfterRevokeRequest.Content = JsonContent.Create(
            new
            {
                branchSlug = seed.BranchSlug,
                serviceVariantIds = new[] { seed.ServiceVariantId },
                staffMemberId = seed.StaffMemberId,
                resourceId = seed.ResourceId,
                startUtc = seed.AvailableSlotStartUtc,
            });
        HttpResponseMessage bookingAfterRevokeResponse =
            await fixture.Client.SendAsync(bookingAfterRevokeRequest);

        Assert.Equal(HttpStatusCode.Created, bookingAfterRevokeResponse.StatusCode);
    }

    private static async Task<Guid> ProvisionTenantAsync(
        HttpClient adminClient,
        string slug,
        string displayName,
        Guid ownerUserAccountId)
    {
        HttpResponseMessage response = await adminClient.PostAsJsonAsync(
            "/api/admin/tenants",
            new
            {
                slug,
                displayName,
                ownerUserAccountId,
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using JsonDocument body =
            JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("tenantId").GetGuid();
    }
}
