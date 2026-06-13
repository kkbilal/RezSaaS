using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.TenantManagement.Domain;

namespace RezSaaS.IdentityIntegrationTests;

public sealed class BusinessAppointmentOperationsApiTests : IClassFixture<IdentityApiFixture>
{
    private readonly IdentityApiFixture fixture;

    public BusinessAppointmentOperationsApiTests(IdentityApiFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task BusinessOwnerCanUpdatePublicProfileSettings()
    {
        PublicBusinessProfileSeed seed = await fixture.SeedPublicBusinessProfileAsync();
        string ownerToken = await RegisterBusinessUserAsync(
            seed,
            TenantMembershipRole.BusinessOwner);

        using HttpRequestMessage updateRequest = CreateBusinessRequest(
            HttpMethod.Patch,
            seed.TenantId,
            ownerToken,
            "/api/business/settings/profile",
            new
            {
                displayName = "Atlas Hair Studio",
                description = "Kadikoy'de guncellenmis salon aciklamasi.",
                publicRules = "Randevu saatinden 5 dakika once salonda olunuz.",
                seoTitle = "Atlas Hair Studio",
                seoDescription = "Kadikoy'de modern sac bakimi.",
                staffDisplayPolicy = "HideNames",
            });

        HttpResponseMessage updateResponse = await fixture.Client.SendAsync(updateRequest);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using JsonDocument updateBody =
            JsonDocument.Parse(await updateResponse.Content.ReadAsStringAsync());
        Assert.Equal("Atlas Hair Studio", updateBody.RootElement.GetProperty("displayName").GetString());
        Assert.Equal("HideNames", updateBody.RootElement.GetProperty("staffDisplayPolicy").GetString());

        HttpResponseMessage profileResponse = await fixture.Client.GetAsync(
            $"/api/public/businesses/{seed.Slug}/profile");

        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);

        using JsonDocument profileBody =
            JsonDocument.Parse(await profileResponse.Content.ReadAsStringAsync());
        Assert.Equal("Atlas Hair Studio", profileBody.RootElement.GetProperty("displayName").GetString());
        Assert.Equal(
            "Randevu saatinden 5 dakika once salonda olunuz.",
            profileBody.RootElement.GetProperty("metadata").GetProperty("publicRules").GetString());
        Assert.Equal(
            "HideNames",
            profileBody.RootElement.GetProperty("metadata").GetProperty("staffDisplayPolicy").GetString());
    }

    [Fact]
    public async Task BranchManagerCannotUpdateTenantWideProfileSettings()
    {
        PublicBusinessProfileSeed seed = await fixture.SeedPublicBusinessProfileAsync();
        string managerToken = await RegisterBusinessUserAsync(
            seed,
            TenantMembershipRole.BranchManager,
            seed.BranchId);

        using HttpRequestMessage updateRequest = CreateBusinessRequest(
            HttpMethod.Patch,
            seed.TenantId,
            managerToken,
            "/api/business/settings/profile",
            new
            {
                displayName = "Unauthorized Update",
                description = "Branch manager should not update tenant-wide profile.",
                publicRules = "No rule.",
                seoTitle = "Unauthorized",
                seoDescription = "Unauthorized",
                staffDisplayPolicy = "ShowNames",
            });

        HttpResponseMessage updateResponse = await fixture.Client.SendAsync(updateRequest);

        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }

    [Fact]
    public async Task BusinessCalendarListsAppointmentsAndUpdatesInternalNote()
    {
        PublicBusinessProfileSeed seed = await fixture.SeedPublicBusinessProfileAsync();
        string ownerToken = await RegisterOwnerAsync(seed);

        using HttpRequestMessage listRequest = CreateBusinessRequest(
            HttpMethod.Get,
            seed.TenantId,
            ownerToken,
            $"/api/business/appointments?branchId={seed.BranchId}"
            + $"&fromUtc={Uri.EscapeDataString(seed.ConfirmedAppointmentStartUtc.AddMinutes(-1).ToString("O"))}"
            + $"&toUtc={Uri.EscapeDataString(seed.ConfirmedAppointmentEndUtc.AddMinutes(1).ToString("O"))}");

        HttpResponseMessage listResponse = await fixture.Client.SendAsync(listRequest);

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        using JsonDocument listBody = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        JsonElement appointment = listBody.RootElement
            .GetProperty("appointments")
            .EnumerateArray()
            .Single(entity => entity.GetProperty("appointmentId").GetGuid() == seed.ConfirmedAppointmentId);
        Assert.Equal("Confirmed", appointment.GetProperty("status").GetString());
        Assert.Equal("Kadikoy", appointment.GetProperty("branchDisplayName").GetString());
        Assert.Equal("Ayse Usta", appointment.GetProperty("staffMemberDisplayName").GetString());
        Assert.Equal("Chair 1", appointment.GetProperty("resourceDisplayName").GetString());

        using HttpRequestMessage noteRequest = CreateBusinessRequest(
            HttpMethod.Post,
            seed.TenantId,
            ownerToken,
            $"/api/business/appointments/{seed.ConfirmedAppointmentId}/notes",
            new { note = "Customer prefers quiet seat." },
            idempotencyKey: $"note-{Guid.NewGuid():N}");

        HttpResponseMessage noteResponse = await fixture.Client.SendAsync(noteRequest);

        Assert.Equal(HttpStatusCode.OK, noteResponse.StatusCode);

        using HttpRequestMessage detailRequest = CreateBusinessRequest(
            HttpMethod.Get,
            seed.TenantId,
            ownerToken,
            $"/api/business/appointments/{seed.ConfirmedAppointmentId}");

        HttpResponseMessage detailResponse = await fixture.Client.SendAsync(detailRequest);

        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);

        using JsonDocument detailBody = JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync());
        Assert.Equal(
            "Customer prefers quiet seat.",
            detailBody.RootElement.GetProperty("businessNote").GetString());
    }

    [Fact]
    public async Task BusinessCanCompleteNoShowAndRebookAppointments()
    {
        PublicBusinessProfileSeed completeSeed = await fixture.SeedPublicBusinessProfileAsync();
        string completeOwnerToken = await RegisterOwnerAsync(completeSeed);

        using HttpRequestMessage completeRequest = CreateBusinessRequest(
            HttpMethod.Post,
            completeSeed.TenantId,
            completeOwnerToken,
            $"/api/business/appointments/{completeSeed.ConfirmedAppointmentId}/complete",
            new { note = "Service completed." },
            idempotencyKey: $"complete-{Guid.NewGuid():N}");

        HttpResponseMessage completeResponse = await fixture.Client.SendAsync(completeRequest);

        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);

        PublicBusinessProfileSeed noShowSeed = await fixture.SeedPublicBusinessProfileAsync();
        string noShowOwnerToken = await RegisterOwnerAsync(noShowSeed);

        using HttpRequestMessage noShowRequest = CreateBusinessRequest(
            HttpMethod.Post,
            noShowSeed.TenantId,
            noShowOwnerToken,
            $"/api/business/appointments/{noShowSeed.ConfirmedAppointmentId}/no-show",
            new { reason = "Customer did not arrive." },
            idempotencyKey: $"noshow-{Guid.NewGuid():N}");

        HttpResponseMessage noShowResponse = await fixture.Client.SendAsync(noShowRequest);

        Assert.Equal(HttpStatusCode.OK, noShowResponse.StatusCode);

        PublicBusinessProfileSeed rebookSeed = await fixture.SeedPublicBusinessProfileAsync(
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)));
        string rebookOwnerToken = await RegisterOwnerAsync(rebookSeed);
        string rebookKey = $"rebook-{Guid.NewGuid():N}";
        DateTimeOffset newStartUtc = rebookSeed.AvailableSlotStartUtc.AddDays(1);
        DateTimeOffset newEndUtc = newStartUtc.AddMinutes(45);

        using HttpRequestMessage rebookRequest = CreateBusinessRequest(
            HttpMethod.Post,
            rebookSeed.TenantId,
            rebookOwnerToken,
            $"/api/business/appointments/{rebookSeed.ConfirmedAppointmentId}/rebook",
            new
            {
                startUtc = newStartUtc,
                endUtc = newEndUtc,
                reason = "Customer requested a new time.",
            },
            rebookKey);

        HttpResponseMessage rebookResponse = await fixture.Client.SendAsync(rebookRequest);

        Assert.Equal(HttpStatusCode.OK, rebookResponse.StatusCode);

        using JsonDocument rebookBody = JsonDocument.Parse(await rebookResponse.Content.ReadAsStringAsync());
        Guid newAppointmentId = rebookBody.RootElement.GetProperty("relatedAppointmentId").GetGuid();
        Assert.NotEqual(Guid.Empty, newAppointmentId);
        Assert.Equal("Rebooked", rebookBody.RootElement.GetProperty("status").GetString());

        using HttpRequestMessage replayRequest = CreateBusinessRequest(
            HttpMethod.Post,
            rebookSeed.TenantId,
            rebookOwnerToken,
            $"/api/business/appointments/{rebookSeed.ConfirmedAppointmentId}/rebook",
            new
            {
                startUtc = newStartUtc,
                endUtc = newEndUtc,
                reason = "Customer requested a new time.",
            },
            rebookKey);

        HttpResponseMessage replayResponse = await fixture.Client.SendAsync(replayRequest);

        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);

        using JsonDocument replayBody = JsonDocument.Parse(await replayResponse.Content.ReadAsStringAsync());
        Assert.Equal(newAppointmentId, replayBody.RootElement.GetProperty("relatedAppointmentId").GetGuid());
    }

    [Fact]
    public async Task BusinessResourceBlockRemovesPublicSlot()
    {
        DateOnly slotDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(9));
        PublicBusinessProfileSeed seed = await fixture.SeedPublicBusinessProfileAsync(slotDate);
        string ownerToken = await RegisterOwnerAsync(seed);

        using HttpRequestMessage blockRequest = CreateBusinessRequest(
            HttpMethod.Post,
            seed.TenantId,
            ownerToken,
            $"/api/business/resources/{seed.ResourceId}/blocks",
            new
            {
                startUtc = seed.AvailableSlotStartUtc,
                endUtc = seed.AvailableSlotStartUtc.AddMinutes(45),
                reason = "Out of service.",
            });

        HttpResponseMessage blockResponse = await fixture.Client.SendAsync(blockRequest);

        Assert.Equal(HttpStatusCode.Created, blockResponse.StatusCode);

        HttpResponseMessage slotsResponse = await fixture.Client.GetAsync(
            $"/api/public/businesses/{seed.Slug}/slots"
            + $"?branchSlug={seed.BranchSlug}"
            + $"&serviceVariantIds={seed.ServiceVariantId}"
            + $"&date={slotDate:yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.OK, slotsResponse.StatusCode);

        using JsonDocument slotsBody = JsonDocument.Parse(await slotsResponse.Content.ReadAsStringAsync());
        Assert.Empty(slotsBody.RootElement.GetProperty("slots").EnumerateArray());
    }

    private async Task<string> RegisterOwnerAsync(PublicBusinessProfileSeed seed)
    {
        return await RegisterBusinessUserAsync(seed, TenantMembershipRole.BusinessOwner);
    }

    private async Task<string> RegisterBusinessUserAsync(
        PublicBusinessProfileSeed seed,
        TenantMembershipRole role,
        Guid? branchId = null)
    {
        string email = $"business-ops-owner-{Guid.NewGuid():N}@example.test";
        const string password = "RezSaaS!Auth1234";
        string token = await RegisterAndLoginWithBearerTokenAsync(email, password);
        Guid userAccountId = await fixture.GetUserAccountIdAsync(email);
        await fixture.GrantTenantMembershipAsync(
            seed.TenantId,
            userAccountId,
            role,
            branchId);

        return token;
    }

    private static HttpRequestMessage CreateBusinessRequest(
        HttpMethod method,
        Guid tenantId,
        string accessToken,
        string requestUri,
        object? body = null,
        string? idempotencyKey = null)
    {
        HttpRequestMessage request = new(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add(TenantContextHeaders.TenantId, tenantId.ToString());

        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

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
