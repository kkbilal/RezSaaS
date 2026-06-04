namespace RezSaaS.Modules.TenantManagement.Application;

public sealed record TenantLifecycleCommandResult(
    bool Succeeded,
    Guid? TenantId,
    string? ErrorCode)
{
    public static TenantLifecycleCommandResult Success(Guid tenantId)
    {
        return new TenantLifecycleCommandResult(
            true,
            tenantId,
            ErrorCode: null);
    }

    public static TenantLifecycleCommandResult Failure(string errorCode)
    {
        return new TenantLifecycleCommandResult(
            false,
            TenantId: null,
            errorCode);
    }
}
