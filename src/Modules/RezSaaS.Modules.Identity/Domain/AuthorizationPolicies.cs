namespace RezSaaS.Modules.Identity.Domain;

public static class AuthorizationPolicies
{
    public const string PlatformAdminOnly = nameof(PlatformAdminOnly);
    public const string PlatformSupportOrAdmin = nameof(PlatformSupportOrAdmin);
}
