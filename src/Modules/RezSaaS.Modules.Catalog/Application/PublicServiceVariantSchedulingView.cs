namespace RezSaaS.Modules.Catalog.Application;

public sealed record PublicServiceVariantSchedulingView(
    Guid Id,
    Guid ServiceId,
    string ServiceName,
    string VariantName,
    int DurationMinutes,
    decimal PriceAmount,
    string CurrencyCode,
    Guid? RequiredResourceTypeId);
