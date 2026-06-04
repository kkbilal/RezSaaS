namespace RezSaaS.Api.Admin;

public sealed record AdminOperationsReconciliationResponse(
    DateTimeOffset EvaluatedAtUtc,
    string Status,
    int FailedNotificationCount,
    int StaleProcessingNotificationCount,
    int CallbackPendingNotificationCount,
    int NotificationOverdueClosureCount,
    int ExecutionStalledClosureCount,
    IReadOnlyCollection<Guid> FailedNotificationIds,
    IReadOnlyCollection<Guid> StaleProcessingNotificationIds,
    IReadOnlyCollection<Guid> CallbackPendingNotificationIds,
    IReadOnlyCollection<Guid> NotificationOverdueClosureCaseIds,
    IReadOnlyCollection<Guid> ExecutionStalledClosureCaseIds);
