namespace RezSaaS.Modules.Messaging.Application;

public sealed record PlatformNotificationReconciliationQuery(
    DateTimeOffset CallbackPendingBeforeUtc,
    DateTimeOffset StaleProcessingBeforeUtc,
    int SampleSize);
