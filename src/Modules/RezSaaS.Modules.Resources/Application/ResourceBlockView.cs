namespace RezSaaS.Modules.Resources.Application;

public sealed record ResourceBlockView(
    Guid Id,
    Guid ResourceId,
    Guid BranchId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    string Reason);
