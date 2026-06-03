using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RezSaaS.Modules.Identity.Domain;

namespace RezSaaS.IdentityIntegrationTests;

public sealed class TestPlatformAdminAuthenticationHandler
    : AuthenticationHandler<TestPlatformAdminAuthenticationOptions>
{
    public const string AuthenticationScheme = "TestPlatformAdmin";

    public TestPlatformAdminAuthenticationHandler(
        IOptionsMonitor<TestPlatformAdminAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        Guid userAccountId = Options.UserAccountId == Guid.Empty
            ? Guid.CreateVersion7()
            : Options.UserAccountId;
        Claim[] claims =
        [
            new("sub", userAccountId.ToString()),
            new(ClaimTypes.NameIdentifier, userAccountId.ToString()),
            new(ClaimTypes.Role, PlatformRoleNames.Administrator),
            new("amr", "mfa"),
        ];
        ClaimsIdentity identity = new(claims, AuthenticationScheme);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
