using Microsoft.Extensions.Options;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Booking.Application;
using RezSaaS.Modules.TenantManagement.Application;

namespace RezSaaS.Api.Business;

public sealed class AppointmentRequestExpiryHostedService : BackgroundService
{
    private static readonly Action<ILogger, Exception?> LogExpiryWorkerFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1, nameof(AppointmentRequestExpiryHostedService)),
            "Appointment request expiry worker failed.");

    private readonly ILogger<AppointmentRequestExpiryHostedService> logger;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IOptions<AppointmentRequestExpiryWorkerOptions> options;

    public AppointmentRequestExpiryHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<AppointmentRequestExpiryWorkerOptions> options,
        ILogger<AppointmentRequestExpiryHostedService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.options = options;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        AppointmentRequestExpiryWorkerOptions workerOptions = options.Value;

        if (!workerOptions.Enabled)
        {
            return;
        }

        if (workerOptions.InitialDelay > TimeSpan.Zero)
        {
            await Task.Delay(workerOptions.InitialDelay, stoppingToken);
        }

        using PeriodicTimer timer = new(workerOptions.Interval);

        do
        {
            await ExpireDueRequestsAsync(workerOptions, stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ExpireDueRequestsAsync(
        AppointmentRequestExpiryWorkerOptions workerOptions,
        CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyCollection<Guid> tenantIds;
            using (IServiceScope scope = scopeFactory.CreateScope())
            {
                TenantLifecycleQueryService tenantQuery =
                    scope.ServiceProvider.GetRequiredService<TenantLifecycleQueryService>();
                tenantIds =
                    await tenantQuery.GetActiveTenantIdsAsync(
                        workerOptions.TenantBatchSize,
                        cancellationToken);
            }

            foreach (Guid tenantId in tenantIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using IServiceScope tenantScope = scopeFactory.CreateScope();
                ITenantContextAccessor tenantContextAccessor =
                    tenantScope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
                ExpireAppointmentRequestsService expiryService =
                    tenantScope.ServiceProvider.GetRequiredService<ExpireAppointmentRequestsService>();
                Guid? previousTenantId = tenantContextAccessor.TenantId;
                tenantContextAccessor.TenantId = tenantId;

                try
                {
                    await expiryService.ExpireDueAsync(cancellationToken);
                }
                finally
                {
                    tenantContextAccessor.TenantId = previousTenantId;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            LogExpiryWorkerFailed(logger, exception);
        }
    }
}
