using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RezSaaS.Api.Configuration;

public sealed class PlatformOperationsHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory scopeFactory;

    public PlatformOperationsHealthCheck(IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        PlatformOperationsReconciliationService service =
            scope.ServiceProvider.GetRequiredService<PlatformOperationsReconciliationService>();
        PlatformOperationsReconciliationSnapshot snapshot =
            await service.InspectAsync(cancellationToken);
        Dictionary<string, object> data = new()
        {
            ["callbackPendingNotifications"] = snapshot.Notifications.CallbackPendingCount,
            ["failedNotifications"] = snapshot.Notifications.FailedCount,
            ["notificationOverdueClosures"] = snapshot.AccountClosures.NotificationOverdueCount,
            ["staleProcessingNotifications"] = snapshot.Notifications.StaleProcessingCount,
            ["stalledClosureExecutions"] = snapshot.AccountClosures.ExecutionStalledCount,
        };

        if (snapshot.HasCriticalIssues)
        {
            return HealthCheckResult.Unhealthy(
                "Critical platform operation reconciliation issues detected.",
                data: data);
        }

        return snapshot.HasIssues
            ? HealthCheckResult.Degraded(
                "Platform operation reconciliation issues detected.",
                data: data)
            : HealthCheckResult.Healthy(
                "Platform operation reconciliation is healthy.",
                data);
    }
}
