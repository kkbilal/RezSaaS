using RezSaaS.BuildingBlocks.Modularity;
using RezSaaS.Modules.Resources.Application;
using RezSaaS.Modules.Resources.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RezSaaS.Modules.Resources;

public sealed class ResourcesModule : ModuleBase
{
    public override string Name => "Resources";

    public override void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(ResourcesDbContext.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ResourcesDbContext.ConnectionStringName}' is required.");

        services.AddDbContext<ResourcesDbContext>(
            options => options.UseNpgsql(connectionString));
        services.AddScoped<PublicResourceAvailabilityQueryService>();
        services.AddScoped<ResourceLabelQueryService>();
        services.AddScoped<ResourceOperationalBlockService>();
        services.AddScoped<ResourceTypeManagementService>();
        services.AddScoped<ResourceManagementService>();
    }
}
