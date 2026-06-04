using Microsoft.Extensions.Options;
using RezSaaS.Modules.Admin.Application;
using RezSaaS.Modules.Messaging.Application;

namespace RezSaaS.Api.Configuration;

public sealed class PlatformOperationsReconciliationService
{
    private readonly AccountClosureReconciliationQueryService accountClosureQueryService;
    private readonly PlatformNotificationReconciliationQueryService notificationQueryService;
    private readonly PlatformOperationsReconciliationOptions options;
    private readonly TimeProvider timeProvider;

    public PlatformOperationsReconciliationService(
        PlatformNotificationReconciliationQueryService notificationQueryService,
        AccountClosureReconciliationQueryService accountClosureQueryService,
        IOptions<PlatformOperationsReconciliationOptions> options,
        TimeProvider timeProvider)
    {
        this.notificationQueryService = notificationQueryService;
        this.accountClosureQueryService = accountClosureQueryService;
        this.options = options.Value;
        this.timeProvider = timeProvider;
    }

    public async Task<PlatformOperationsReconciliationSnapshot> InspectAsync(
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        Task<PlatformNotificationReconciliationSnapshot> notificationTask =
            notificationQueryService.InspectAsync(
                new PlatformNotificationReconciliationQuery(
                    now.Subtract(options.CallbackPendingThreshold),
                    now.Subtract(options.StaleProcessingThreshold),
                    options.SampleSize),
                cancellationToken);
        Task<AccountClosureReconciliationSnapshot> accountClosureTask =
            accountClosureQueryService.InspectAsync(
                new AccountClosureReconciliationQuery(
                    now.Subtract(options.NotificationOverdueThreshold),
                    now.Subtract(options.ClosureExecutionStallThreshold),
                    options.SampleSize),
                cancellationToken);

        await Task.WhenAll(notificationTask, accountClosureTask);

        return new PlatformOperationsReconciliationSnapshot(
            now,
            await notificationTask,
            await accountClosureTask);
    }
}
