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
}
