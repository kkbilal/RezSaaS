using System.Net;
using System.Net.Http.Json;

namespace RezSaaS.IdentityIntegrationTests;

public sealed class IdentityLockoutTests : IClassFixture<IdentityApiFixture>
{
    private readonly IdentityApiFixture fixture;

    public IdentityLockoutTests(IdentityApiFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task AccountIsLockedAfterRepeatedInvalidPasswords()
    {
        string email = $"lockout-test-{Guid.NewGuid():N}@example.test";
        const string password = "RezSaaS!Auth1234";
        await fixture.Client.PostAsJsonAsync("/api/auth/register", new { email, password });

        for (int attempt = 0; attempt < 5; attempt++)
        {
            HttpResponseMessage invalidLogin = await fixture.Client.PostAsJsonAsync(
                "/api/auth/login?useCookies=false",
                new { email, password = "Wrong!Password123" });

            Assert.Equal(HttpStatusCode.Unauthorized, invalidLogin.StatusCode);
        }

        HttpResponseMessage lockedLogin = await fixture.Client.PostAsJsonAsync(
            "/api/auth/login?useCookies=false",
            new { email, password });

        Assert.Equal(HttpStatusCode.Unauthorized, lockedLogin.StatusCode);
    }
}
