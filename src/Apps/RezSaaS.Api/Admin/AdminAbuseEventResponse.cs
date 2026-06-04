namespace RezSaaS.Api.Admin;

public sealed record AdminAbuseEventResponse(
    Guid EventId,
    Guid? TenantId,
    Guid UserAccountId,
    string EventType,
    string Severity,
    string DetailsJson,
    DateTimeOffset OccurredAtUtc);
