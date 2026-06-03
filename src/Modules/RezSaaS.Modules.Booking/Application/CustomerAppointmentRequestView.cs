namespace RezSaaS.Modules.Booking.Application;

public sealed record CustomerAppointmentRequestView(
    Guid Id,
    Guid BranchId,
    Guid StaffMemberId,
    Guid ResourceId,
    DateTimeOffset RequestedStartUtc,
    DateTimeOffset RequestedEndUtc,
    DateTimeOffset ExpiresAtUtc,
    string Status,
    IReadOnlyCollection<CustomerAppointmentRequestLineView> Lines);
