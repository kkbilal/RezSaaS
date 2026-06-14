using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RezSaaS.BuildingBlocks.Modularity;
using RezSaaS.Modules.Payments.Application;
using RezSaaS.Modules.Payments.Infrastructure.Persistence;

namespace RezSaaS.Modules.Payments;

public sealed class PaymentsModule : ModuleBase
{
    public override string Name => "Payments";

    public override void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(PaymentsDbContext.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{PaymentsDbContext.ConnectionStringName}' is required.");

        services.AddDbContext<PaymentsDbContext>(
            options => options.UseNpgsql(connectionString));
        services.AddOptions<PaymentReadinessOptions>()
            .Bind(configuration.GetSection(PaymentReadinessOptions.SectionName))
            .Validate(
                options => !options.OnlineCollectionEnabled
                    || !string.IsNullOrWhiteSpace(options.ProviderKey),
                "Online payment collection requires a configured provider key.")
            .Validate(
                options => options.WebhookMaxPayloadBytes > 0
                    && options.WebhookMaxPayloadBytes <= 256_000,
                "Payment webhook max payload size must be between 1 and 256000 bytes.")
            .ValidateOnStart();
        services.AddScoped<PaymentReadinessService>();
    }
}
