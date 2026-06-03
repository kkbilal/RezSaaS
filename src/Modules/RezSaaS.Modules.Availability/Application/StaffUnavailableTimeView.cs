namespace RezSaaS.Modules.Availability.Application;

public sealed record StaffUnavailableTimeView(
    Guid Id,
    Guid StaffMemberId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    string Reason);
