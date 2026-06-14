using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RezSaaS.BuildingBlocks.Modularity;
using RezSaaS.Modules.Integrations.Application;
using RezSaaS.Modules.Integrations.Infrastructure.Persistence;

namespace RezSaaS.Modules.Integrations;

public sealed class IntegrationsModule : ModuleBase
{
    public override string Name => "Integrations";

    public override void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(IntegrationsDbContext.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{IntegrationsDbContext.ConnectionStringName}' is required.");

        services.AddDbContext<IntegrationsDbContext>(
            options => options.UseNpgsql(connectionString));
        services.AddOptions<IntegrationReadinessOptions>()
            .Bind(configuration.GetSection(IntegrationReadinessOptions.SectionName))
            .Validate(
                options => !options.WebhookDeliveryEnabled || options.ExternalApiEnabled,
                "Webhook delivery cannot be enabled before external API foundation is enabled.")
            .Validate(
                options => options.WebhookMaxPayloadBytes > 0
                    && options.WebhookMaxPayloadBytes <= 256_000,
                "Integration webhook max payload size must be between 1 and 256000 bytes.")
            .ValidateOnStart();
        services.AddScoped<IntegrationReadinessService>();
    }
}
