namespace RezSaaS.Modules.Catalog.Application;

public sealed record PublicServiceVariantView(
    Guid Id,
    string Name,
    int DurationMinutes,
    decimal PriceAmount,
    string CurrencyCode,
    Guid? RequiredResourceTypeId);
