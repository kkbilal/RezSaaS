using RezSaaS.Modules.Admin.Domain;

namespace RezSaaS.Modules.Admin.Application;

public sealed record AbuseEventView(
    Guid Id,
    Guid? TenantId,
    Guid UserAccountId,
    string EventType,
    AbuseEventSeverity Severity,
    string DetailsJson,
    DateTimeOffset OccurredAtUtc);
