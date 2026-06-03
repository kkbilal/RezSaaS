namespace RezSaaS.Modules.TenantManagement.Application;

public sealed record CreateTenantWithOwnerResult(
    bool Succeeded,
    Guid? TenantId,
    string? ErrorCode)
{
    public static CreateTenantWithOwnerResult Success(Guid tenantId)
    {
        return new CreateTenantWithOwnerResult(
            true,
            tenantId,
            ErrorCode: null);
    }

    public static CreateTenantWithOwnerResult Failure(string errorCode)
    {
        return new CreateTenantWithOwnerResult(
            false,
            TenantId: null,
            errorCode);
    }
}
