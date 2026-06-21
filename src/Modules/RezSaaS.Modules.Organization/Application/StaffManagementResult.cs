namespace RezSaaS.Modules.Organization.Application;

public sealed record StaffManagementResult(
    bool Succeeded,
    string? ErrorCode,
    StaffView? Staff,
    IReadOnlyCollection<StaffView>? StaffMembers)
{
    public static StaffManagementResult Success(StaffView staff)
    {
        return new StaffManagementResult(true, null, staff, null);
    }

    public static StaffManagementResult SuccessList(IReadOnlyCollection<StaffView> staffMembers)
    {
        return new StaffManagementResult(true, null, null, staffMembers);
    }

    public static StaffManagementResult Failure(string errorCode)
    {
        return new StaffManagementResult(false, errorCode, null, null);
    }
}
