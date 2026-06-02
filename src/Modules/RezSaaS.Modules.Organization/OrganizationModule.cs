using RezSaaS.BuildingBlocks.Modularity;
using RezSaaS.Modules.Organization.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RezSaaS.Modules.Organization;

public sealed class OrganizationModule : ModuleBase
{
    public override string Name => "Organization";

    public override void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(OrganizationDbContext.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{OrganizationDbContext.ConnectionStringName}' is required.");

        services.AddDbContext<OrganizationDbContext>(
            options => options.UseNpgsql(connectionString));
    }
}
