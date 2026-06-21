namespace RezSaaS.Api.Business;

public sealed record BusinessWorkingHoursResult(
    BusinessWorkingHoursOutcome Outcome,
    string? ErrorCode,
    BusinessWorkingHoursResponse? WorkingHours,
    IReadOnlyCollection<BusinessWorkingHoursResponse>? WorkingHoursList)
{
    public static BusinessWorkingHoursResult Success(BusinessWorkingHoursResponse workingHours)
        => new(BusinessWorkingHoursOutcome.Success, null, workingHours, null);
    public static BusinessWorkingHoursResult SuccessList(IReadOnlyCollection<BusinessWorkingHoursResponse> workingHoursList)
        => new(BusinessWorkingHoursOutcome.Success, null, null, workingHoursList);
    public static BusinessWorkingHoursResult Failure(BusinessWorkingHoursOutcome outcome, string errorCode)
        => new(outcome, errorCode, null, null);
}
