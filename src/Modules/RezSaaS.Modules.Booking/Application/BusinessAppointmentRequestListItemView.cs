namespace RezSaaS.Modules.Booking.Application;

public sealed record BusinessAppointmentRequestListItemView(
    Guid Id,
    Guid CustomerUserAccountId,
    Guid BranchId,
    Guid StaffMemberId,
    Guid ResourceId,
    DateTimeOffset RequestedStartUtc,
    DateTimeOffset RequestedEndUtc,
    DateTimeOffset ExpiresAtUtc,
    string Status,
    IReadOnlyCollection<BusinessAppointmentRequestLineView> Lines);
