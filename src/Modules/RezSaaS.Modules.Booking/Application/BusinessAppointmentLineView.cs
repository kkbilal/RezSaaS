namespace RezSaaS.Modules.Booking.Application;

public sealed record BusinessAppointmentLineView(
    Guid ServiceVariantId,
    string ServiceNameSnapshot,
    int DurationMinutes,
    decimal PriceAmount,
    string CurrencyCode);
