namespace RezSaaS.IdentityIntegrationTests;

public sealed class IdentityBootstrapTokenTests : IClassFixture<IdentityApiFixture>
{
    private readonly IdentityApiFixture fixture;

    public IdentityBootstrapTokenTests(IdentityApiFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task PlatformAdminBootstrapRejectsInvalidToken()
    {
        var result = await fixture.BootstrapPlatformAdminAsync(
            $"platform-admin-invalid-{Guid.NewGuid():N}@example.test",
            "RezSaaS!PlatformAdmin1234",
            "wrong-token");

        Assert.False(result.Succeeded);
        Assert.Equal(0, await fixture.GetPlatformRoleCountAsync());
        Assert.Equal(0, await fixture.GetIdentityAuditLogCountAsync());
    }
}
