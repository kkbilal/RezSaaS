namespace RezSaaS.Modules.Admin.Application;

public sealed record AccountClosureReconciliationQuery(
    DateTimeOffset NotificationOverdueBeforeUtc,
    DateTimeOffset ExecutionStalledBeforeUtc,
    int SampleSize);
