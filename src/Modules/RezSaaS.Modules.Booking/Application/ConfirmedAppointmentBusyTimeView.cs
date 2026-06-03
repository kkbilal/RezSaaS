namespace RezSaaS.Modules.Booking.Application;

public sealed record ConfirmedAppointmentBusyTimeView(
    Guid StaffMemberId,
    Guid ResourceId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc);
