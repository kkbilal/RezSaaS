namespace RezSaaS.Modules.Booking.Application;

public sealed record CustomerAppointmentRequestLineView(
    Guid ServiceVariantId,
    string ServiceNameSnapshot,
    int DurationMinutes,
    decimal PriceAmount,
    string CurrencyCode);
