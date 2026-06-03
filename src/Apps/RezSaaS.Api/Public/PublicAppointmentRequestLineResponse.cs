namespace RezSaaS.Api.PublicApi;

public sealed record PublicAppointmentRequestLineResponse(
    Guid ServiceVariantId,
    string ServiceNameSnapshot,
    int DurationMinutes,
    decimal PriceAmount,
    string CurrencyCode);
