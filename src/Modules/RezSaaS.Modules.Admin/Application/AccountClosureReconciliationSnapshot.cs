namespace RezSaaS.Modules.Admin.Application;

public sealed record AccountClosureReconciliationSnapshot(
    int NotificationOverdueCount,
    int ExecutionStalledCount,
    IReadOnlyCollection<Guid> NotificationOverdueClosureCaseIds,
    IReadOnlyCollection<Guid> ExecutionStalledClosureCaseIds);
