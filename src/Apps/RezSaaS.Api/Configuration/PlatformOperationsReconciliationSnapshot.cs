using RezSaaS.Modules.Admin.Application;
using RezSaaS.Modules.Messaging.Application;

namespace RezSaaS.Api.Configuration;

public sealed record PlatformOperationsReconciliationSnapshot(
    DateTimeOffset EvaluatedAtUtc,
    PlatformNotificationReconciliationSnapshot Notifications,
    AccountClosureReconciliationSnapshot AccountClosures)
{
    public bool HasCriticalIssues =>
        Notifications.FailedCount > 0
        || AccountClosures.ExecutionStalledCount > 0;

    public bool HasIssues =>
        HasCriticalIssues
        || Notifications.StaleProcessingCount > 0
        || Notifications.CallbackPendingCount > 0
        || AccountClosures.NotificationOverdueCount > 0;
}
