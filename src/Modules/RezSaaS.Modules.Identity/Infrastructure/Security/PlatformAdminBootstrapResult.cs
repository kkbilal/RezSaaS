namespace RezSaaS.Modules.Identity.Infrastructure.Security;

public sealed record PlatformAdminBootstrapResult(
    bool Succeeded,
    string Reason)
{
    public static PlatformAdminBootstrapResult Success()
    {
        return new PlatformAdminBootstrapResult(true, "Bootstrapped");
    }

    public static PlatformAdminBootstrapResult Failed(string reason)
    {
        return new PlatformAdminBootstrapResult(false, reason);
    }
}
