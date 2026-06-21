namespace RezSaaS.Api.Business;

public sealed record BusinessStaffUnavailableResult(
    BusinessStaffUnavailableOutcome Outcome,
    string? ErrorCode,
    BusinessStaffUnavailableResponse? UnavailableTime,
    IReadOnlyCollection<BusinessStaffUnavailableResponse>? UnavailableTimes)
{
    public static BusinessStaffUnavailableResult Success(BusinessStaffUnavailableResponse unavailableTime)
        => new(BusinessStaffUnavailableOutcome.Success, null, unavailableTime, null);
    public static BusinessStaffUnavailableResult SuccessList(IReadOnlyCollection<BusinessStaffUnavailableResponse> unavailableTimes)
        => new(BusinessStaffUnavailableOutcome.Success, null, null, unavailableTimes);
    public static BusinessStaffUnavailableResult Failure(BusinessStaffUnavailableOutcome outcome, string errorCode)
        => new(outcome, errorCode, null, null);
}
