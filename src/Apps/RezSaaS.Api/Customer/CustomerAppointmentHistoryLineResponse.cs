namespace RezSaaS.Api.Customer;

public sealed record CustomerAppointmentHistoryLineResponse(
    Guid ServiceVariantId,
    string ServiceNameSnapshot,
    int DurationMinutes,
    decimal PriceAmount,
    string CurrencyCode);
