using RezSaaS.BuildingBlocks.Messaging;
using RezSaaS.BuildingBlocks.Modularity;
using RezSaaS.Modules.Messaging.Application;
using RezSaaS.Modules.Messaging.Infrastructure.Persistence;
using RezSaaS.Modules.Messaging.Infrastructure.Queue;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RezSaaS.Modules.Messaging;

public sealed class MessagingModule : ModuleBase
{
    public override string Name => "Messaging";

    public override void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(MessagingDbContext.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{MessagingDbContext.ConnectionStringName}' is required.");

        services.AddDbContext<MessagingDbContext>(
            options => options.UseNpgsql(connectionString));
        services.AddScoped<ITransactionalMessageOutbox, TransactionalMessageOutbox>();
        services.AddScoped<PlatformNotificationReconciliationQueryService>();
        services.AddScoped<PlatformTransactionalMessageQueueService>();
    }
}
