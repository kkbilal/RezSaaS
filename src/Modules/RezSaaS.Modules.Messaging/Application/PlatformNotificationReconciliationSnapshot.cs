namespace RezSaaS.Modules.Messaging.Application;

public sealed record PlatformNotificationReconciliationSnapshot(
    int FailedCount,
    int StaleProcessingCount,
    int CallbackPendingCount,
    IReadOnlyCollection<Guid> FailedMessageIds,
    IReadOnlyCollection<Guid> StaleProcessingMessageIds,
    IReadOnlyCollection<Guid> CallbackPendingMessageIds);
