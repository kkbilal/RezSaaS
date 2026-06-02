namespace RezSaaS.Modules.Identity.Infrastructure.Security;

public interface IPlatformAdminBootstrapService
{
    Task<PlatformAdminBootstrapResult> BootstrapAsync(
        PlatformAdminBootstrapRequest request,
        CancellationToken cancellationToken = default);
}
