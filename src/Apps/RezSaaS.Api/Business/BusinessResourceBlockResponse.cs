namespace RezSaaS.Api.Business;

public sealed record BusinessResourceBlockResponse(
    Guid ResourceBlockId,
    Guid ResourceId,
    Guid BranchId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    string Reason);
