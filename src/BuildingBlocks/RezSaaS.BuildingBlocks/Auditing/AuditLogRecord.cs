namespace RezSaaS.BuildingBlocks.Auditing;

public sealed record AuditLogRecord(
    Guid? TenantId,
    Guid ActorUserAccountId,
    string Action,
    string DetailsJson,
    DateTimeOffset OccurredAtUtc);
