namespace RezSaaS.Modules.Availability.Application;

public sealed record BranchWorkingHoursManagementResult(
    bool Succeeded,
    string? ErrorCode,
    BranchWorkingHoursView? WorkingHours,
    IReadOnlyCollection<BranchWorkingHoursView>? WorkingHoursList)
{
    public static BranchWorkingHoursManagementResult Success(BranchWorkingHoursView workingHours)
        => new(true, null, workingHours, null);
    public static BranchWorkingHoursManagementResult SuccessList(IReadOnlyCollection<BranchWorkingHoursView> workingHoursList)
        => new(true, null, null, workingHoursList);
    public static BranchWorkingHoursManagementResult Failure(string errorCode)
        => new(false, errorCode, null, null);
}
