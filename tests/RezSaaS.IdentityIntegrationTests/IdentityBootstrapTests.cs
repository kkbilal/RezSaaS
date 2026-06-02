namespace RezSaaS.IdentityIntegrationTests;

public sealed class IdentityBootstrapTests : IClassFixture<IdentityApiFixture>
{
    private readonly IdentityApiFixture fixture;

    public IdentityBootstrapTests(IdentityApiFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task PlatformAdminBootstrapCreatesRolesAdminAndAuditEntryOnce()
    {
        string email = $"platform-admin-{Guid.NewGuid():N}@example.test";

        var result = await fixture.BootstrapPlatformAdminAsync(
            email,
            "RezSaaS!PlatformAdmin1234",
            "test-bootstrap-token");

        Assert.True(result.Succeeded);
        Assert.Equal(2, await fixture.GetPlatformRoleCountAsync());
        Assert.Equal(1, await fixture.GetPlatformAdminAssignmentCountAsync());
        Assert.Equal(1, await fixture.GetIdentityAuditLogCountAsync());

        var secondResult = await fixture.BootstrapPlatformAdminAsync(
            $"platform-admin-second-{Guid.NewGuid():N}@example.test",
            "RezSaaS!PlatformAdmin1234",
            "test-bootstrap-token");

        Assert.False(secondResult.Succeeded);
        Assert.Equal(1, await fixture.GetPlatformAdminAssignmentCountAsync());
    }

}
