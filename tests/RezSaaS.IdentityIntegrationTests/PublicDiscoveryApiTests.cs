using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.TenantManagement.Domain;

namespace RezSaaS.IdentityIntegrationTests;

public sealed class PublicDiscoveryApiTests : IClassFixture<IdentityApiFixture>
{
    private readonly IdentityApiFixture fixture;

    public PublicDiscoveryApiTests(IdentityApiFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task PublicBusinessProfileIncludesMenuStaffAndWorkingHours()
    {
        PublicBusinessProfileSeed seed = await fixture.SeedPublicBusinessProfileAsync();

        HttpResponseMessage response = await fixture.Client.GetAsync(
            $"/api/public/businesses/{seed.Slug}/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement root = body.RootElement;
        Assert.Equal(seed.Slug, root.GetProperty("slug").GetString());
        Assert.Equal("Atlas Hair", root.GetProperty("displayName").GetString());
        Assert.Single(root.GetProperty("services").EnumerateArray());

        JsonElement service = root.GetProperty("services").EnumerateArray().Single();
        Assert.Equal("Sac Kesimi", service.GetProperty("name").GetString());
        JsonElement variant = service.GetProperty("variants").EnumerateArray().Single();
        Assert.Equal("Standart Kesim", variant.GetProperty("name").GetString());
        Assert.Equal(45, variant.GetProperty("durationMinutes").GetInt32());
        Assert.Equal(750, variant.GetProperty("priceAmount").GetDecimal());
        Assert.Equal("TRY", variant.GetProperty("currencyCode").GetString());

        JsonElement metadata = root.GetProperty("metadata");
        Assert.Equal("Atlas Hair Kadikoy", metadata.GetProperty("seoTitle").GetString());
        Assert.Equal(4.8m, metadata.GetProperty("ratingAverage").GetDecimal());
        Assert.Equal(12, metadata.GetProperty("reviewCount").GetInt32());
        Assert.Single(metadata.GetProperty("galleryImages").EnumerateArray());

        JsonElement branch = root.GetProperty("branches").EnumerateArray().Single();
        Assert.Equal("Kadikoy", branch.GetProperty("displayName").GetString());
        JsonElement staffMember = branch.GetProperty("staffMembers").EnumerateArray().Single();
        Assert.Equal("Ayse Usta", staffMember.GetProperty("displayName").GetString());
        Assert.Equal(seed.RequiredSkillId, staffMember.GetProperty("skillIds").EnumerateArray().Single().GetGuid());

        JsonElement workingHours = branch.GetProperty("workingHours").EnumerateArray().Single();
        Assert.Equal("Monday", workingHours.GetProperty("dayOfWeek").GetString());
        Assert.False(workingHours.GetProperty("isClosed").GetBoolean());
    }

    [Fact]
    public async Task PublicBusinessSlotsExcludeConfirmedAppointmentsUnavailableTimesAndResourceBlocks()
    {
        PublicBusinessProfileSeed seed = await fixture.SeedPublicBusinessProfileAsync();

        HttpResponseMessage response = await fixture.Client.GetAsync(
            $"/api/public/businesses/{seed.Slug}/slots"
            + $"?branchSlug={seed.BranchSlug}"
            + $"&serviceVariantIds={seed.ServiceVariantId}"
            + "&date=2026-01-05");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement root = body.RootElement;
        Assert.Equal(seed.Slug, root.GetProperty("businessSlug").GetString());
        Assert.Equal(seed.BranchSlug, root.GetProperty("branchSlug").GetString());
        Assert.Equal("Europe/Istanbul", root.GetProperty("branchTimeZoneId").GetString());
        Assert.Equal(45, root.GetProperty("durationMinutes").GetInt32());

        JsonElement slot = root.GetProperty("slots").EnumerateArray().Single();
        Assert.Equal("2026-01-05T08:15:00+00:00", slot.GetProperty("startUtc").GetString());
        Assert.Equal("2026-01-05T09:00:00+00:00", slot.GetProperty("endUtc").GetString());
        Assert.Equal("2026-01-05T11:15:00", slot.GetProperty("localStart").GetString());
        Assert.Equal("2026-01-05T12:00:00", slot.GetProperty("localEnd").GetString());

        JsonElement staff = slot.GetProperty("staffCandidates").EnumerateArray().Single();
        Assert.Equal(seed.StaffMemberId, staff.GetProperty("id").GetGuid());

        JsonElement resource = slot.GetProperty("resourceCandidates").EnumerateArray().Single();
        Assert.Equal("Chair 1", resource.GetProperty("displayName").GetString());
    }

    [Fact]
    public async Task PublicBusinessSlotsExcludeStaffWithoutRequiredSkills()
    {
        PublicBusinessProfileSeed seed =
            await fixture.SeedPublicBusinessProfileAsync(includeUnqualifiedStaff: true);

        HttpResponseMessage response = await fixture.Client.GetAsync(
            $"/api/public/businesses/{seed.Slug}/slots"
            + $"?branchSlug={seed.BranchSlug}"
            + $"&serviceVariantIds={seed.ServiceVariantId}"
            + "&date=2026-01-05");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement slot = body.RootElement.GetProperty("slots").EnumerateArray().Single();
        Guid[] staffIds = slot.GetProperty("staffCandidates")
            .EnumerateArray()
            .Select(entity => entity.GetProperty("id").GetGuid())
            .ToArray();

        Assert.Contains(seed.StaffMemberId, staffIds);
        Assert.DoesNotContain(seed.UnqualifiedStaffMemberId!.Value, staffIds);
    }

    [Fact]
    public async Task AuthenticatedCustomerCanCreatePendingAppointmentRequestFromPublicBusiness()
    {
        string email = $"public-booking-{Guid.NewGuid():N}@example.test";
        const string password = "RezSaaS!Auth1234";
        PublicBusinessProfileSeed seed =
            await fixture.SeedPublicBusinessProfileAsync(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)));
        string accessToken = await RegisterAndLoginWithBearerTokenAsync(email, password);

        using HttpRequestMessage request = new(
            HttpMethod.Post,
            $"/api/public/businesses/{seed.Slug}/appointment-requests");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(
            new
            {
                branchSlug = seed.BranchSlug,
                serviceVariantIds = new[] { seed.ServiceVariantId },
                staffMemberId = seed.StaffMemberId,
                resourceId = seed.ResourceId,
                startUtc = seed.AvailableSlotStartUtc,
            });

        HttpResponseMessage response = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement root = body.RootElement;
        Assert.NotEqual(Guid.Empty, root.GetProperty("appointmentRequestId").GetGuid());
        Assert.Equal("PendingApproval", root.GetProperty("status").GetString());
        Assert.True(root.GetProperty("expiresAtUtc").GetDateTimeOffset() > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task PublicAppointmentRequestCreateIsIdempotentForSameKey()
    {
        string email = $"idempotent-booking-{Guid.NewGuid():N}@example.test";
        const string password = "RezSaaS!Auth1234";
        PublicBusinessProfileSeed seed =
            await fixture.SeedPublicBusinessProfileAsync(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(19)));
        string accessToken = await RegisterAndLoginWithBearerTokenAsync(email, password);
        string idempotencyKey = $"idem-{Guid.NewGuid():N}";

        using HttpRequestMessage firstRequest = CreatePublicAppointmentRequestMessage(
            seed,
            accessToken,
            idempotencyKey);
        HttpResponseMessage firstResponse = await fixture.Client.SendAsync(firstRequest);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        using JsonDocument firstBody =
            JsonDocument.Parse(await firstResponse.Content.ReadAsStringAsync());
        Guid firstId = firstBody.RootElement.GetProperty("appointmentRequestId").GetGuid();

        using HttpRequestMessage secondRequest = CreatePublicAppointmentRequestMessage(
            seed,
            accessToken,
            idempotencyKey);
        HttpResponseMessage secondResponse = await fixture.Client.SendAsync(secondRequest);

        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        using JsonDocument secondBody =
            JsonDocument.Parse(await secondResponse.Content.ReadAsStringAsync());
        Assert.Equal(firstId, secondBody.RootElement.GetProperty("appointmentRequestId").GetGuid());
    }

    [Fact]
    public async Task PublicAppointmentRequestCreateRejectsStartOutsideSlotGrid()
    {
        string email = $"off-grid-booking-{Guid.NewGuid():N}@example.test";
        const string password = "RezSaaS!Auth1234";
        PublicBusinessProfileSeed seed =
            await fixture.SeedPublicBusinessProfileAsync(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(21)));
        string accessToken = await RegisterAndLoginWithBearerTokenAsync(email, password);

        using HttpRequestMessage request = new(
            HttpMethod.Post,
            $"/api/public/businesses/{seed.Slug}/appointment-requests");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(
            new
            {
                branchSlug = seed.BranchSlug,
                serviceVariantIds = new[] { seed.ServiceVariantId },
                staffMemberId = seed.StaffMemberId,
                resourceId = seed.ResourceId,
                startUtc = seed.AvailableSlotStartUtc.AddMinutes(1),
            });

        HttpResponseMessage response = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PublicAppointmentRequestRequiresAuthentication()
    {
        PublicBusinessProfileSeed seed =
            await fixture.SeedPublicBusinessProfileAsync(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15)));

        HttpResponseMessage response = await fixture.Client.PostAsJsonAsync(
            $"/api/public/businesses/{seed.Slug}/appointment-requests",
            new
            {
                branchSlug = seed.BranchSlug,
                serviceVariantIds = new[] { seed.ServiceVariantId },
                staffMemberId = seed.StaffMemberId,
                resourceId = seed.ResourceId,
                startUtc = seed.AvailableSlotStartUtc,
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PublicAppointmentRequestListRejectsInvalidStatusFilter()
    {
        string email = $"invalid-status-customer-{Guid.NewGuid():N}@example.test";
        const string password = "RezSaaS!Auth1234";
        PublicBusinessProfileSeed seed =
            await fixture.SeedPublicBusinessProfileAsync(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(22)));
        string accessToken = await RegisterAndLoginWithBearerTokenAsync(email, password);

        using HttpRequestMessage request = new(
            HttpMethod.Get,
            $"/api/public/businesses/{seed.Slug}/appointment-requests?status=NotAStatus");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedCustomerCanListViewAndCancelOwnAppointmentRequest()
    {
        string email = $"public-booking-owner-{Guid.NewGuid():N}@example.test";
        const string password = "RezSaaS!Auth1234";
        PublicBusinessProfileSeed seed =
            await fixture.SeedPublicBusinessProfileAsync(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)));
        string accessToken = await RegisterAndLoginWithBearerTokenAsync(email, password);
        Guid appointmentRequestId = await CreatePublicAppointmentRequestAsync(seed, accessToken);

        using HttpRequestMessage listRequest = new(
            HttpMethod.Get,
            $"/api/public/businesses/{seed.Slug}/appointment-requests?status=PendingApproval");
        listRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage listResponse = await fixture.Client.SendAsync(listRequest);

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        using JsonDocument listBody =
            JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        JsonElement listedRequest = listBody.RootElement
            .GetProperty("requests")
            .EnumerateArray()
            .Single();
        Assert.Equal(appointmentRequestId, listedRequest.GetProperty("id").GetGuid());

        using HttpRequestMessage detailRequest = new(
            HttpMethod.Get,
            $"/api/public/businesses/{seed.Slug}/appointment-requests/{appointmentRequestId}");
        detailRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage detailResponse = await fixture.Client.SendAsync(detailRequest);

        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);

        using HttpRequestMessage cancelRequest = new(
            HttpMethod.Post,
            $"/api/public/businesses/{seed.Slug}/appointment-requests/{appointmentRequestId}/cancel");
        cancelRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        cancelRequest.Headers.Add("Idempotency-Key", $"cancel-{Guid.NewGuid():N}");

        HttpResponseMessage cancelResponse = await fixture.Client.SendAsync(cancelRequest);

        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

        using JsonDocument cancelBody =
            JsonDocument.Parse(await cancelResponse.Content.ReadAsStringAsync());
        Assert.Equal("CancelledByCustomer", cancelBody.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task BusinessOwnerCanListAndApprovePendingAppointmentRequest()
    {
        PublicBusinessProfileSeed seed =
            await fixture.SeedPublicBusinessProfileAsync(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(16)));
        string customerEmail = $"booking-customer-{Guid.NewGuid():N}@example.test";
        string ownerEmail = $"booking-owner-{Guid.NewGuid():N}@example.test";
        const string password = "RezSaaS!Auth1234";
        string customerToken = await RegisterAndLoginWithBearerTokenAsync(customerEmail, password);
        string ownerToken = await RegisterAndLoginWithBearerTokenAsync(ownerEmail, password);
        Guid ownerUserAccountId = await fixture.GetUserAccountIdAsync(ownerEmail);
        await fixture.GrantTenantMembershipAsync(
            seed.TenantId,
            ownerUserAccountId,
            TenantMembershipRole.BusinessOwner);
        Guid appointmentRequestId = await CreatePublicAppointmentRequestAsync(seed, customerToken);

        using HttpRequestMessage pendingRequest = new(
            HttpMethod.Get,
            "/api/business/appointment-requests/pending");
        pendingRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        pendingRequest.Headers.Add(TenantContextHeaders.TenantId, seed.TenantId.ToString());

        HttpResponseMessage pendingResponse = await fixture.Client.SendAsync(pendingRequest);

        Assert.Equal(HttpStatusCode.OK, pendingResponse.StatusCode);

        using JsonDocument pendingBody =
            JsonDocument.Parse(await pendingResponse.Content.ReadAsStringAsync());
        JsonElement pendingItem = pendingBody.RootElement
            .GetProperty("requests")
            .EnumerateArray()
            .Single();
        Assert.Equal(appointmentRequestId, pendingItem.GetProperty("id").GetGuid());
        Assert.Equal("PendingApproval", pendingItem.GetProperty("status").GetString());
        Assert.Equal("b***@example.test", pendingItem.GetProperty("customer").GetProperty("maskedEmail").GetString());

        using HttpRequestMessage detailRequest = new(
            HttpMethod.Get,
            $"/api/business/appointment-requests/{appointmentRequestId}");
        detailRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        detailRequest.Headers.Add(TenantContextHeaders.TenantId, seed.TenantId.ToString());

        HttpResponseMessage detailResponse = await fixture.Client.SendAsync(detailRequest);

        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);

        string approveIdempotencyKey = $"approve-{Guid.NewGuid():N}";
        using HttpRequestMessage approveRequest = new(
            HttpMethod.Post,
            $"/api/business/appointment-requests/{appointmentRequestId}/approve");
        approveRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        approveRequest.Headers.Add(TenantContextHeaders.TenantId, seed.TenantId.ToString());
        approveRequest.Headers.Add("Idempotency-Key", approveIdempotencyKey);

        HttpResponseMessage approveResponse = await fixture.Client.SendAsync(approveRequest);

        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        using JsonDocument approveBody =
            JsonDocument.Parse(await approveResponse.Content.ReadAsStringAsync());
        JsonElement approval = approveBody.RootElement;
        Guid appointmentId = approval.GetProperty("appointmentId").GetGuid();
        Assert.NotEqual(Guid.Empty, appointmentId);
        Assert.Equal("Approved", approval.GetProperty("status").GetString());

        using HttpRequestMessage approveReplayRequest = new(
            HttpMethod.Post,
            $"/api/business/appointment-requests/{appointmentRequestId}/approve");
        approveReplayRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        approveReplayRequest.Headers.Add(TenantContextHeaders.TenantId, seed.TenantId.ToString());
        approveReplayRequest.Headers.Add("Idempotency-Key", approveIdempotencyKey);

        HttpResponseMessage approveReplayResponse = await fixture.Client.SendAsync(approveReplayRequest);

        Assert.Equal(HttpStatusCode.OK, approveReplayResponse.StatusCode);

        using JsonDocument approveReplayBody =
            JsonDocument.Parse(await approveReplayResponse.Content.ReadAsStringAsync());
        Assert.Equal(
            appointmentId,
            approveReplayBody.RootElement.GetProperty("appointmentId").GetGuid());
    }

    [Fact]
    public async Task BusinessOwnerCanDeclinePendingAppointmentRequest()
    {
        PublicBusinessProfileSeed seed =
            await fixture.SeedPublicBusinessProfileAsync(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(18)));
        string customerEmail = $"decline-customer-{Guid.NewGuid():N}@example.test";
        string ownerEmail = $"decline-owner-{Guid.NewGuid():N}@example.test";
        const string password = "RezSaaS!Auth1234";
        string customerToken = await RegisterAndLoginWithBearerTokenAsync(customerEmail, password);
        string ownerToken = await RegisterAndLoginWithBearerTokenAsync(ownerEmail, password);
        Guid ownerUserAccountId = await fixture.GetUserAccountIdAsync(ownerEmail);
        await fixture.GrantTenantMembershipAsync(
            seed.TenantId,
            ownerUserAccountId,
            TenantMembershipRole.BusinessOwner);
        Guid appointmentRequestId = await CreatePublicAppointmentRequestAsync(seed, customerToken);

        using HttpRequestMessage declineRequest = new(
            HttpMethod.Post,
            $"/api/business/appointment-requests/{appointmentRequestId}/decline");
        declineRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        declineRequest.Headers.Add(TenantContextHeaders.TenantId, seed.TenantId.ToString());

        HttpResponseMessage declineResponse = await fixture.Client.SendAsync(declineRequest);

        Assert.Equal(HttpStatusCode.OK, declineResponse.StatusCode);

        using JsonDocument declineBody =
            JsonDocument.Parse(await declineResponse.Content.ReadAsStringAsync());
        JsonElement decline = declineBody.RootElement;
        Assert.Equal("Declined", decline.GetProperty("status").GetString());
        Assert.Equal(0, decline.GetProperty("affectedRequests").GetInt32());
    }

    [Fact]
    public async Task BusinessAppointmentRequestListRejectsInvalidStatusFilter()
    {
        PublicBusinessProfileSeed seed =
            await fixture.SeedPublicBusinessProfileAsync(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(23)));
        string ownerEmail = $"invalid-status-owner-{Guid.NewGuid():N}@example.test";
        const string password = "RezSaaS!Auth1234";
        string ownerToken = await RegisterAndLoginWithBearerTokenAsync(ownerEmail, password);
        Guid ownerUserAccountId = await fixture.GetUserAccountIdAsync(ownerEmail);
        await fixture.GrantTenantMembershipAsync(
            seed.TenantId,
            ownerUserAccountId,
            TenantMembershipRole.BusinessOwner);

        using HttpRequestMessage request = new(
            HttpMethod.Get,
            "/api/business/appointment-requests/?status=NotAStatus");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        request.Headers.Add(TenantContextHeaders.TenantId, seed.TenantId.ToString());

        HttpResponseMessage response = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BusinessAppointmentRequestsRequireTenantMembership()
    {
        PublicBusinessProfileSeed seed =
            await fixture.SeedPublicBusinessProfileAsync(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(17)));
        string email = $"not-member-{Guid.NewGuid():N}@example.test";
        const string password = "RezSaaS!Auth1234";
        string accessToken = await RegisterAndLoginWithBearerTokenAsync(email, password);

        using HttpRequestMessage request = new(
            HttpMethod.Get,
            "/api/business/appointment-requests/pending");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add(TenantContextHeaders.TenantId, seed.TenantId.ToString());

        HttpResponseMessage response = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SuspendedTenantBlocksPublicDiscoveryNewBookingAndBusinessManagement()
    {
        PublicBusinessProfileSeed seed =
            await fixture.SeedPublicBusinessProfileAsync(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(24)));
        const string password = "RezSaaS!Auth1234";
        string customerEmail = $"suspended-customer-{Guid.NewGuid():N}@example.test";
        string ownerEmail = $"suspended-owner-{Guid.NewGuid():N}@example.test";
        string customerToken = await RegisterAndLoginWithBearerTokenAsync(customerEmail, password);
        string ownerToken = await RegisterAndLoginWithBearerTokenAsync(ownerEmail, password);
        Guid ownerUserAccountId = await fixture.GetUserAccountIdAsync(ownerEmail);
        await fixture.GrantTenantMembershipAsync(
            seed.TenantId,
            ownerUserAccountId,
            TenantMembershipRole.BusinessOwner);
        await CreatePublicAppointmentRequestAsync(seed, customerToken);
        using HttpClient adminClient =
            fixture.CreatePlatformAdminStepUpClient(Guid.CreateVersion7());
        HttpResponseMessage suspendResponse = await adminClient.PostAsJsonAsync(
            $"/api/admin/tenants/{seed.TenantId}/suspend",
            new { reason = "Operational investigation" });
        Assert.Equal(HttpStatusCode.OK, suspendResponse.StatusCode);

        HttpResponseMessage profileResponse = await fixture.Client.GetAsync(
            $"/api/public/businesses/{seed.Slug}/profile");
        HttpResponseMessage slotsResponse = await fixture.Client.GetAsync(
            $"/api/public/businesses/{seed.Slug}/slots"
            + $"?branchSlug={seed.BranchSlug}"
            + $"&serviceVariantIds={seed.ServiceVariantId}"
            + $"&date={DateOnly.FromDateTime(seed.AvailableSlotStartUtc.UtcDateTime):yyyy-MM-dd}");
        using HttpRequestMessage createRequest =
            CreatePublicAppointmentRequestMessage(seed, customerToken);
        HttpResponseMessage createResponse = await fixture.Client.SendAsync(createRequest);
        using HttpRequestMessage businessRequest = new(
            HttpMethod.Get,
            "/api/business/appointment-requests/pending");
        businessRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        businessRequest.Headers.Add(TenantContextHeaders.TenantId, seed.TenantId.ToString());
        HttpResponseMessage businessResponse = await fixture.Client.SendAsync(businessRequest);
        using HttpRequestMessage customerHistoryRequest = new(
            HttpMethod.Get,
            $"/api/public/businesses/{seed.Slug}/appointment-requests");
        customerHistoryRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", customerToken);
        HttpResponseMessage customerHistoryResponse =
            await fixture.Client.SendAsync(customerHistoryRequest);

        Assert.Equal(HttpStatusCode.NotFound, profileResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, slotsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, businessResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, customerHistoryResponse.StatusCode);

        HttpResponseMessage reactivateResponse = await adminClient.PostAsJsonAsync(
            $"/api/admin/tenants/{seed.TenantId}/reactivate",
            new { reason = "Investigation completed" });
        Assert.Equal(HttpStatusCode.OK, reactivateResponse.StatusCode);

        HttpResponseMessage reactivatedProfileResponse = await fixture.Client.GetAsync(
            $"/api/public/businesses/{seed.Slug}/profile");
        using HttpRequestMessage reactivatedBusinessRequest = new(
            HttpMethod.Get,
            "/api/business/appointment-requests/pending");
        reactivatedBusinessRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", ownerToken);
        reactivatedBusinessRequest.Headers.Add(
            TenantContextHeaders.TenantId,
            seed.TenantId.ToString());
        HttpResponseMessage reactivatedBusinessResponse =
            await fixture.Client.SendAsync(reactivatedBusinessRequest);

        Assert.Equal(HttpStatusCode.OK, reactivatedProfileResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, reactivatedBusinessResponse.StatusCode);
    }

    private async Task<Guid> CreatePublicAppointmentRequestAsync(
        PublicBusinessProfileSeed seed,
        string accessToken)
    {
        using HttpRequestMessage request = CreatePublicAppointmentRequestMessage(seed, accessToken);
        HttpResponseMessage response = await fixture.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("appointmentRequestId").GetGuid();
    }

    private static HttpRequestMessage CreatePublicAppointmentRequestMessage(
        PublicBusinessProfileSeed seed,
        string accessToken,
        string? idempotencyKey = null)
    {
        HttpRequestMessage request = new(
            HttpMethod.Post,
            $"/api/public/businesses/{seed.Slug}/appointment-requests");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        request.Content = JsonContent.Create(
            new
            {
                branchSlug = seed.BranchSlug,
                serviceVariantIds = new[] { seed.ServiceVariantId },
                staffMemberId = seed.StaffMemberId,
                resourceId = seed.ResourceId,
                startUtc = seed.AvailableSlotStartUtc,
            });

        return request;
    }

    private async Task<string> RegisterAndLoginWithBearerTokenAsync(string email, string password)
    {
        HttpResponseMessage registration = await fixture.Client.PostAsJsonAsync(
            "/api/auth/register",
            new { email, password });
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);

        HttpResponseMessage login = await fixture.Client.PostAsJsonAsync(
            "/api/auth/login?useCookies=false",
            new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        using JsonDocument body = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("The access token was not returned.");
    }
}
