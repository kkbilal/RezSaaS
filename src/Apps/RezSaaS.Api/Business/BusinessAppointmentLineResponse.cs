namespace RezSaaS.Api.Business;

public sealed record BusinessAppointmentLineResponse(
    Guid ServiceVariantId,
    string ServiceNameSnapshot,
    int DurationMinutes,
    decimal PriceAmount,
    string CurrencyCode);
