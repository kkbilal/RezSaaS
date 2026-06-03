namespace RezSaaS.Modules.Booking.Application;

public sealed record BookingIdempotencyContext(
    string KeyHash,
    string RequestHash);
