using Microsoft.Extensions.Options;

namespace RezSaaS.Api.Configuration;

public sealed class PlatformOperationsReconciliationHostedService : BackgroundService
{
    private static readonly Action<ILogger, int, string, Exception?> LogCallbackPending =
        LoggerMessage.Define<int, string>(
            LogLevel.Warning,
            new EventId(3, nameof(PlatformOperationsReconciliationHostedService)),
            "Platform notification callbacks are pending. Count: {Count}; SampleIds: {SampleIds}");

    private static readonly Action<ILogger, int, string, Exception?> LogExecutionStalled =
        LoggerMessage.Define<int, string>(
            LogLevel.Critical,
            new EventId(5, nameof(PlatformOperationsReconciliationHostedService)),
            "Account closure executions are stalled. Count: {Count}; SampleIds: {SampleIds}");

    private static readonly Action<ILogger, int, string, Exception?> LogFailedNotifications =
        LoggerMessage.Define<int, string>(
            LogLevel.Error,
            new EventId(1, nameof(PlatformOperationsReconciliationHostedService)),
            "Platform notifications reached terminal failure. Count: {Count}; SampleIds: {SampleIds}");

    private static readonly Action<ILogger, Exception?> LogInspectionFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(6, nameof(PlatformOperationsReconciliationHostedService)),
            "Platform operations reconciliation inspection failed.");

    private static readonly Action<ILogger, int, string, Exception?> LogNotificationOverdue =
        LoggerMessage.Define<int, string>(
            LogLevel.Warning,
            new EventId(4, nameof(PlatformOperationsReconciliationHostedService)),
            "Account closure notifications are overdue. Count: {Count}; SampleIds: {SampleIds}");

    private static readonly Action<ILogger, int, string, Exception?> LogStaleProcessing =
        LoggerMessage.Define<int, string>(
            LogLevel.Warning,
            new EventId(2, nameof(PlatformOperationsReconciliationHostedService)),
            "Platform notifications have stale processing leases. Count: {Count}; SampleIds: {SampleIds}");

    private readonly ILogger<PlatformOperationsReconciliationHostedService> logger;
    private readonly PlatformOperationsReconciliationOptions options;
    private readonly IServiceScopeFactory scopeFactory;

    public PlatformOperationsReconciliationHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<PlatformOperationsReconciliationOptions> options,
        ILogger<PlatformOperationsReconciliationHostedService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.options = options.Value;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled)
        {
            return;
        }

        if (options.InitialDelay > TimeSpan.Zero)
        {
            await Task.Delay(options.InitialDelay, stoppingToken);
        }

        using PeriodicTimer timer = new(options.Interval);

        do
        {
            await InspectAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task InspectAsync(CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            PlatformOperationsReconciliationService service =
                scope.ServiceProvider.GetRequiredService<PlatformOperationsReconciliationService>();
            PlatformOperationsReconciliationSnapshot snapshot =
                await service.InspectAsync(cancellationToken);

            LogIssues(snapshot);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            LogInspectionFailed(logger, exception);
        }
    }

    private void LogIssues(PlatformOperationsReconciliationSnapshot snapshot)
    {
        if (snapshot.Notifications.FailedCount > 0)
        {
            LogFailedNotifications(
                logger,
                snapshot.Notifications.FailedCount,
                FormatIds(snapshot.Notifications.FailedMessageIds),
                null);
        }

        if (snapshot.Notifications.StaleProcessingCount > 0)
        {
            LogStaleProcessing(
                logger,
                snapshot.Notifications.StaleProcessingCount,
                FormatIds(snapshot.Notifications.StaleProcessingMessageIds),
                null);
        }

        if (snapshot.Notifications.CallbackPendingCount > 0)
        {
            LogCallbackPending(
                logger,
                snapshot.Notifications.CallbackPendingCount,
                FormatIds(snapshot.Notifications.CallbackPendingMessageIds),
                null);
        }

        if (snapshot.AccountClosures.NotificationOverdueCount > 0)
        {
            LogNotificationOverdue(
                logger,
                snapshot.AccountClosures.NotificationOverdueCount,
                FormatIds(snapshot.AccountClosures.NotificationOverdueClosureCaseIds),
                null);
        }

        if (snapshot.AccountClosures.ExecutionStalledCount > 0)
        {
            LogExecutionStalled(
                logger,
                snapshot.AccountClosures.ExecutionStalledCount,
                FormatIds(snapshot.AccountClosures.ExecutionStalledClosureCaseIds),
                null);
        }
    }

    private static string FormatIds(IReadOnlyCollection<Guid> ids)
    {
        return string.Join(',', ids.Select(id => id.ToString("D")));
    }
}
