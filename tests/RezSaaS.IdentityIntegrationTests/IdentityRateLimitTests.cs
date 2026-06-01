using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RezSaaS.IdentityIntegrationTests;

public sealed class IdentityRateLimitTests
{
    [Fact]
    public async Task AuthenticationEndpointsAreRateLimitedPerIpAddress()
    {
        await using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>();
        using HttpClient client = factory.CreateClient();

        for (int attempt = 0; attempt < 10; attempt++)
        {
            HttpResponseMessage unauthorized = await LoginWithInvalidCredentialsAsync(client);
            Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        }

        HttpResponseMessage rateLimited = await LoginWithInvalidCredentialsAsync(client);
        Assert.Equal(HttpStatusCode.TooManyRequests, rateLimited.StatusCode);
    }

    private static async Task<HttpResponseMessage> LoginWithInvalidCredentialsAsync(HttpClient client)
    {
        return await client.PostAsJsonAsync(
            "/api/auth/login?useCookies=false",
            new
            {
                email = "missing-user@example.test",
                password = "Wrong!Password123",
            });
    }
}
