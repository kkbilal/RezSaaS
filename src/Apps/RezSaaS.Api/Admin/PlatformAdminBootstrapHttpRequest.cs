namespace RezSaaS.Api.Admin;

public sealed record PlatformAdminBootstrapHttpRequest(
    string Email,
    string Password,
    string BootstrapToken);
