namespace RezSaaS.Api.PublicApi;

public sealed record PublicBranchWorkingHoursProfileResponse(
    string DayOfWeek,
    TimeOnly OpensAt,
    TimeOnly ClosesAt,
    bool IsClosed);
