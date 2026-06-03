using System.Net;
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
}
