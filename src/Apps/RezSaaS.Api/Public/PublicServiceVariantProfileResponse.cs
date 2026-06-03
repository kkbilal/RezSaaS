namespace RezSaaS.Api.PublicApi;

public sealed record PublicServiceVariantProfileResponse(
    Guid Id,
    string Name,
    int DurationMinutes,
    decimal PriceAmount,
    string CurrencyCode,
    Guid? RequiredResourceTypeId);
