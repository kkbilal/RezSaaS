namespace RezSaaS.Modules.Availability.Application;

public sealed record BranchWorkingHoursView(
    Guid Id,
    DayOfWeek DayOfWeek,
    TimeOnly OpensAt,
    TimeOnly ClosesAt,
    bool IsClosed);
