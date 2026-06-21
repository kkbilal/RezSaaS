namespace RezSaaS.Modules.Availability.Application;

public sealed record StaffUnavailableManagementResult(
    bool Succeeded,
    string? ErrorCode,
    StaffUnavailableTimeView? UnavailableTime,
    IReadOnlyCollection<StaffUnavailableTimeView>? UnavailableTimes)
{
    public static StaffUnavailableManagementResult Success(StaffUnavailableTimeView unavailableTime)
        => new(true, null, unavailableTime, null);
    public static StaffUnavailableManagementResult SuccessList(IReadOnlyCollection<StaffUnavailableTimeView> unavailableTimes)
        => new(true, null, null, unavailableTimes);
    public static StaffUnavailableManagementResult Failure(string errorCode)
        => new(false, errorCode, null, null);
}
