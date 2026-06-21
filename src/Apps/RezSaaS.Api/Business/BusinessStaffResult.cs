namespace RezSaaS.Api.Business;

public sealed record BusinessStaffResult(
    BusinessStaffOutcome Outcome,
    string? ErrorCode,
    BusinessStaffResponse? Staff,
    IReadOnlyCollection<BusinessStaffResponse>? StaffMembers)
{
    public static BusinessStaffResult Success(BusinessStaffResponse staff)
    {
        return new BusinessStaffResult(BusinessStaffOutcome.Success, null, staff, null);
    }

    public static BusinessStaffResult SuccessList(IReadOnlyCollection<BusinessStaffResponse> staffMembers)
    {
        return new BusinessStaffResult(BusinessStaffOutcome.Success, null, null, staffMembers);
    }

    public static BusinessStaffResult Failure(BusinessStaffOutcome outcome, string errorCode)
    {
        return new BusinessStaffResult(outcome, errorCode, null, null);
    }
}
