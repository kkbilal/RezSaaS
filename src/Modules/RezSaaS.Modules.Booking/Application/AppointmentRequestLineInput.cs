namespace RezSaaS.Modules.Booking.Application;

public sealed record AppointmentRequestLineInput(
    Guid ServiceVariantId,
    string ServiceNameSnapshot,
    int DurationMinutes,
    decimal PriceAmount,
    string CurrencyCode);
