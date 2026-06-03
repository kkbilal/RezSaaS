namespace RezSaaS.Modules.TenantManagement.Application;

public sealed record TenantMembershipCommandResult(
    bool Succeeded,
    Guid? MembershipId,
    string? ErrorCode)
{
    public static TenantMembershipCommandResult Success(Guid membershipId)
    {
        return new TenantMembershipCommandResult(
            true,
            membershipId,
            ErrorCode: null);
    }

    public static TenantMembershipCommandResult Failure(string errorCode)
    {
        return new TenantMembershipCommandResult(
            false,
            MembershipId: null,
            errorCode);
    }
}
