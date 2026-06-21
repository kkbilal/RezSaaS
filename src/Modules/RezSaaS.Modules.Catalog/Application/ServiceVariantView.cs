namespace RezSaaS.Modules.Catalog.Application;

public sealed record ServiceVariantView(
    Guid Id,
    Guid ServiceId,
    string Name,
    int DurationMinutes,
    decimal PriceAmount,
    string CurrencyCode,
    Guid? RequiredResourceTypeId,
    DateTimeOffset CreatedAtUtc);
