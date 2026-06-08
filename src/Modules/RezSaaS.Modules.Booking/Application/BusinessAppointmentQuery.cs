namespace RezSaaS.Modules.Booking.Application;

public sealed record BusinessAppointmentQuery(
    Guid? BranchId,
    string? Status,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    int Take);
