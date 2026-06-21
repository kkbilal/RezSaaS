namespace RezSaaS.Modules.Catalog.Application;

public sealed record CreateServiceVariantCommand(
    Guid ActorUserAccountId,
    Guid ServiceId,
    string Name,
    int DurationMinutes,
    decimal PriceAmount,
    string CurrencyCode,
    Guid? RequiredResourceTypeId);

public sealed record UpdateServiceVariantCommand(
    Guid ActorUserAccountId,
    Guid ServiceId,
    Guid VariantId,
    string Name,
    int DurationMinutes,
    decimal PriceAmount,
    string CurrencyCode,
    Guid? RequiredResourceTypeId);
