namespace RezSaaS.Modules.Booking.Application;

public sealed record BusinessAppointmentRequestLineView(
    Guid ServiceVariantId,
    string ServiceNameSnapshot,
    int DurationMinutes,
    decimal PriceAmount,
    string CurrencyCode);
