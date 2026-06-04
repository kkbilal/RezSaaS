using Microsoft.Extensions.Options;

namespace RezSaaS.Api.Configuration;

public sealed class PlatformNotificationHostedService : BackgroundService
{
    private static readonly Action<ILogger, Exception?> LogWorkerFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1, nameof(PlatformNotificationHostedService)),
            "Platform notification worker failed.");

    private readonly ILogger<PlatformNotificationHostedService> logger;
    private readonly PlatformNotificationWorkerOptions options;
    private readonly IServiceScopeFactory scopeFactory;

    public PlatformNotificationHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<PlatformNotificationWorkerOptions> options,
        ILogger<PlatformNotificationHostedService> logger)
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
            await DispatchDueAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task DispatchDueAsync(CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            PlatformNotificationDispatchService service =
                scope.ServiceProvider.GetRequiredService<PlatformNotificationDispatchService>();
            await service.DispatchDueAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            LogWorkerFailed(logger, exception);
        }
    }
}
