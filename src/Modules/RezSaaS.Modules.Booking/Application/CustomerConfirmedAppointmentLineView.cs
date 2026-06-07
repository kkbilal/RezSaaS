namespace RezSaaS.Modules.Booking.Application;

public sealed record CustomerConfirmedAppointmentLineView(
    Guid ServiceVariantId,
    string ServiceNameSnapshot,
    int DurationMinutes,
    decimal PriceAmount,
    string CurrencyCode);
