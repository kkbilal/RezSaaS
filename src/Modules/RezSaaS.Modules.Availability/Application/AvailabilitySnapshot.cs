namespace RezSaaS.Modules.Availability.Application;

public sealed record AvailabilitySnapshot(
    Guid BranchId,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    IReadOnlyCollection<BranchWorkingHoursView> WorkingHours,
    IReadOnlyCollection<StaffUnavailableTimeView> StaffUnavailableTimes);
