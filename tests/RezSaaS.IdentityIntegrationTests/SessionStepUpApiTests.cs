using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RezSaaS.Modules.Identity.Infrastructure.Security;

namespace RezSaaS.IdentityIntegrationTests;

public sealed class SessionStepUpApiTests : IClassFixture<IdentityApiFixture>
{
    private readonly IdentityApiFixture fixture;

    public SessionStepUpApiTests(IdentityApiFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task PlatformAdminCanUseMfaStepUpSessionForPrivilegedEndpoints()
    {
        string email = $"step-up-admin-{Guid.NewGuid():N}@example.test";
        const string password = "RezSaaS!PlatformAdmin1234";
        PlatformAdminBootstrapResult bootstrapResult =
            await fixture.BootstrapPlatformAdminAsync(
                email,
                password,
                "test-bootstrap-token");
        Assert.True(bootstrapResult.Succeeded);

        using HttpClient client = fixture.CreateClient();
        string accessToken = await LoginWithBearerTokenAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage beforeStepUp = await client.GetAsync("/api/admin/tenants");
        Assert.Equal(HttpStatusCode.Forbidden, beforeStepUp.StatusCode);

        string recoveryCode = await fixture.EnableTwoFactorAndGenerateRecoveryCodeAsync(email);
        HttpResponseMessage stepUpResponse = await client.PostAsJsonAsync(
            "/api/session/step-up",
            new
            {
                password,
                recoveryCode,
            });

        string stepUpResponseBody = await stepUpResponse.Content.ReadAsStringAsync();
        Assert.True(
            stepUpResponse.StatusCode == HttpStatusCode.OK,
            $"Expected OK but got {stepUpResponse.StatusCode}: {stepUpResponseBody}");

        using JsonDocument stepUpBody =
            JsonDocument.Parse(stepUpResponseBody);
        Assert.True(stepUpBody.RootElement.GetProperty("isSatisfied").GetBoolean());
        Assert.Equal("mfa", stepUpBody.RootElement.GetProperty("method").GetString());

        HttpResponseMessage afterStepUp = await client.GetAsync("/api/admin/tenants");
        Assert.Equal(HttpStatusCode.OK, afterStepUp.StatusCode);

        HttpResponseMessage bootstrap = await client.GetAsync("/api/session/bootstrap");
        Assert.Equal(HttpStatusCode.OK, bootstrap.StatusCode);

        using JsonDocument bootstrapBody =
            JsonDocument.Parse(await bootstrap.Content.ReadAsStringAsync());
        Assert.True(bootstrapBody.RootElement.GetProperty("stepUp").GetProperty("isSatisfied").GetBoolean());
    }

    private static async Task<string> LoginWithBearerTokenAsync(
        HttpClient client,
        string email,
        string password)
    {
        HttpResponseMessage login = await client.PostAsJsonAsync(
            "/api/auth/login?useCookies=false",
            new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        using JsonDocument body = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("The access token was not returned.");
    }
}
