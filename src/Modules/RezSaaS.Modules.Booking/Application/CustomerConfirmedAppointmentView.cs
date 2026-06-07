namespace RezSaaS.Modules.Booking.Application;

public sealed record CustomerConfirmedAppointmentView(
    Guid Id,
    Guid? AppointmentRequestId,
    Guid BranchId,
    Guid StaffMemberId,
    Guid ResourceId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    string Status,
    IReadOnlyCollection<CustomerConfirmedAppointmentLineView> Lines);
