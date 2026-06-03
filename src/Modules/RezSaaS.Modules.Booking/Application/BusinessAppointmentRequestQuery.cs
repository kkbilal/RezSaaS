namespace RezSaaS.Modules.Booking.Application;

public sealed record BusinessAppointmentRequestQuery(
    Guid? BranchId,
    string? Status,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    int Take);
