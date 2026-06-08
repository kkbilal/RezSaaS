namespace RezSaaS.Modules.Booking.Application;

public sealed record BusinessAppointmentAuthorizationContext(
    Guid Id,
    Guid BranchId,
    Guid CustomerUserAccountId,
    string Status);
