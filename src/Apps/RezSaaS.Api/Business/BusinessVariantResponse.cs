namespace RezSaaS.Api.Business;

public sealed record BusinessVariantResponse(
    Guid Id, Guid ServiceId, string Name, int DurationMinutes,
    decimal PriceAmount, string CurrencyCode, Guid? RequiredResourceTypeId, DateTimeOffset CreatedAtUtc);
