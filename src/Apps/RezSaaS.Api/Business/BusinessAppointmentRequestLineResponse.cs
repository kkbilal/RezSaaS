namespace RezSaaS.Api.Business;

public sealed record BusinessAppointmentRequestLineResponse(
    Guid ServiceVariantId,
    string ServiceNameSnapshot,
    int DurationMinutes,
    decimal PriceAmount,
    string CurrencyCode);
