namespace RezSaaS.Modules.Booking.Application;

public sealed record CreateAppointmentRequestCommand(
    Guid CustomerUserAccountId,
    Guid BranchId,
    Guid StaffMemberId,
    Guid ResourceId,
    DateTimeOffset RequestedStartUtc,
    DateTimeOffset RequestedEndUtc,
    IReadOnlyCollection<AppointmentRequestLineInput> Lines,
    TimeSpan? ResponseBuffer = null);
