using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RezSaaS.IdentityIntegrationTests;

public sealed class IdentityApiTests : IClassFixture<IdentityApiFixture>
{
    private readonly IdentityApiFixture fixture;

    public IdentityApiTests(IdentityApiFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task SwaggerDocumentIsAvailableInDevelopment()
    {
        HttpResponseMessage response = await fixture.Client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnsafePostWithMismatchedOriginIsRejected()
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/auth/register");
        request.Headers.Add("Origin", "https://evil.example");
        request.Content = JsonContent.Create(
            new
            {
                email = CreateEmail(),
                password = "RezSaaS!Auth1234",
            });

        HttpResponseMessage response = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RegistrationAndBearerLoginAllowProtectedAccountAccess()
    {
        string email = CreateEmail();
        const string password = "RezSaaS!Auth1234";

        HttpResponseMessage registration = await RegisterAsync(email, password);
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);

        string accessToken = await LoginWithBearerTokenAsync(email, password);

        using HttpRequestMessage request = new(HttpMethod.Get, "/api/auth/manage/info");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage accountInfo = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, accountInfo.StatusCode);

        using JsonDocument body =
            JsonDocument.Parse(await accountInfo.Content.ReadAsStringAsync());
        Assert.Equal(email, body.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task DefaultLoginDoesNotRequireOneTimeCode()
    {
        string email = CreateEmail();
        const string password = "RezSaaS!Auth1234";
        await RegisterAsync(email, password);

        HttpResponseMessage login = await fixture.Client.PostAsJsonAsync(
            "/api/auth/login?useCookies=false",
            new { email, password });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        using JsonDocument body = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.TryGetProperty("accessToken", out JsonElement accessToken));
        Assert.False(string.IsNullOrWhiteSpace(accessToken.GetString()));
        Assert.False(body.RootElement.TryGetProperty("requiresTwoFactor", out _));
    }

    [Fact]
    public async Task InvalidPasswordIsRejected()
    {
        string email = CreateEmail();
        const string password = "RezSaaS!Auth1234";
        await RegisterAsync(email, password);

        HttpResponseMessage login = await fixture.Client.PostAsJsonAsync(
            "/api/auth/login?useCookies=false",
            new { email, password = "Wrong!Password123" });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task SuspendedAccountCannotSignIn()
    {
        string email = CreateEmail();
        const string password = "RezSaaS!Auth1234";
        await RegisterAsync(email, password);
        await fixture.SuspendUserAsync(email);

        HttpResponseMessage login = await fixture.Client.PostAsJsonAsync(
            "/api/auth/login?useCookies=false",
            new { email, password });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task MigrationDoesNotProvisionPlatformRoles()
    {
        Assert.Equal(0, await fixture.GetPlatformRoleCountAsync());
    }

    [Fact]
    public async Task CookieLoginAllowsProtectedAccountAccess()
    {
        string email = CreateEmail();
        const string password = "RezSaaS!Auth1234";
        await RegisterAsync(email, password);

        using HttpClient cookieClient = fixture.CreateClient();
        HttpResponseMessage login = await cookieClient.PostAsJsonAsync(
            "/api/auth/login?useCookies=true",
            new { email, password });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        HttpResponseMessage accountInfo = await cookieClient.GetAsync("/api/auth/manage/info");
        Assert.Equal(HttpStatusCode.OK, accountInfo.StatusCode);
    }

    private async Task<HttpResponseMessage> RegisterAsync(string email, string password)
    {
        return await fixture.Client.PostAsJsonAsync(
            "/api/auth/register",
            new { email, password });
    }

    private async Task<string> LoginWithBearerTokenAsync(string email, string password)
    {
        HttpResponseMessage login = await fixture.Client.PostAsJsonAsync(
            "/api/auth/login?useCookies=false",
            new { email, password });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        using JsonDocument body = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("The access token was not returned.");
    }

    private static string CreateEmail()
    {
        return $"identity-test-{Guid.NewGuid():N}@example.test";
    }
}
