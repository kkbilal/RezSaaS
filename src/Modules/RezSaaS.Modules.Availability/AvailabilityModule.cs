using RezSaaS.BuildingBlocks.Modularity;
using RezSaaS.Modules.Availability.Application;
using RezSaaS.Modules.Availability.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RezSaaS.Modules.Availability;

public sealed class AvailabilityModule : ModuleBase
{
    public override string Name => "Availability";

    public override void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(AvailabilityDbContext.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{AvailabilityDbContext.ConnectionStringName}' is required.");

        services.AddDbContext<AvailabilityDbContext>(
            options => options.UseNpgsql(connectionString));
        services.AddScoped<AvailabilityQueryService>();
    }
}
