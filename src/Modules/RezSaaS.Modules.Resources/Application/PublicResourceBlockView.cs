namespace RezSaaS.Modules.Resources.Application;

public sealed record PublicResourceBlockView(
    Guid ResourceId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc);
