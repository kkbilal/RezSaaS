using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

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

        JsonElement branch = root.GetProperty("branches").EnumerateArray().Single();
        Assert.Equal("Kadikoy", branch.GetProperty("displayName").GetString());
        JsonElement staffMember = branch.GetProperty("staffMembers").EnumerateArray().Single();
        Assert.Equal("Ayse Usta", staffMember.GetProperty("displayName").GetString());

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
