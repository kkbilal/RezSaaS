namespace RezSaaS.Modules.Catalog.Application;

public sealed record ServiceView(
    Guid Id,
    string Name,
    string CategoryKey,
    string Status,
    DateTimeOffset CreatedAtUtc);
