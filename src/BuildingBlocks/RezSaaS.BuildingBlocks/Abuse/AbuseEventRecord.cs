namespace RezSaaS.BuildingBlocks.Abuse;

public sealed record AbuseEventRecord(
    Guid? TenantId,
    Guid UserAccountId,
    string EventType,
    AbuseEventSeverityLevel Severity,
    string DetailsJson,
    DateTimeOffset OccurredAtUtc);
