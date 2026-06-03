using RezSaaS.BuildingBlocks.Modularity;
using RezSaaS.Modules.Catalog.Application;
using RezSaaS.Modules.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RezSaaS.Modules.Catalog;

public sealed class CatalogModule : ModuleBase
{
    public override string Name => "Catalog";

    public override void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(CatalogDbContext.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{CatalogDbContext.ConnectionStringName}' is required.");

        services.AddDbContext<CatalogDbContext>(
            options => options.UseNpgsql(connectionString));
        services.AddScoped<PublicCatalogMenuService>();
        services.AddScoped<PublicCatalogSchedulingService>();
    }
}
