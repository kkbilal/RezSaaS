namespace RezSaaS.Modules.Catalog.Application;

public sealed record CreateServiceCommand(
    Guid ActorUserAccountId,
    string Name,
    string CategoryKey);

public sealed record UpdateServiceCommand(
    Guid ActorUserAccountId,
    Guid ServiceId,
    string Name,
    string CategoryKey);
