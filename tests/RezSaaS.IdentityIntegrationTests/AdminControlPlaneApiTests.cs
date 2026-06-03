using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

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
        Assert.Equal(1, await fixture.GetTenantCountAsync());
        Assert.Equal(1, await fixture.GetTenantAuditLogCountAsync());
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
}
