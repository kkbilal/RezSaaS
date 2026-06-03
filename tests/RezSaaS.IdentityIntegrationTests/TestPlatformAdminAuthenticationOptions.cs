using Microsoft.AspNetCore.Authentication;

namespace RezSaaS.IdentityIntegrationTests;

public sealed class TestPlatformAdminAuthenticationOptions : AuthenticationSchemeOptions
{
    public Guid UserAccountId { get; set; }
}
