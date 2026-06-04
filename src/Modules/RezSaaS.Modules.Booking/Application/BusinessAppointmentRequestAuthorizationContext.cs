namespace RezSaaS.Modules.Booking.Application;

public sealed record BusinessAppointmentRequestAuthorizationContext(
    Guid Id,
    Guid BranchId,
    Guid CustomerUserAccountId,
    string Status);
