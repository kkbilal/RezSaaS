namespace RezSaaS.Modules.TenantManagement.Application;

public sealed record ChangeTenantLifecycleCommand(
    Guid TenantId,
    Guid ActorUserAccountId,
    string Reason);
