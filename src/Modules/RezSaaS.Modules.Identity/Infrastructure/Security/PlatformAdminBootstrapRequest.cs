namespace RezSaaS.Modules.Identity.Infrastructure.Security;

public sealed record PlatformAdminBootstrapRequest(
    string Email,
    string Password,
    string BootstrapToken);
