namespace RezSaaS.Api.Business;

public sealed record BusinessWorkingHoursResponse(
    Guid Id, string DayOfWeek, string OpensAt, string ClosesAt, bool IsClosed);
