using RezSaaS.BuildingBlocks.Modularity;
using RezSaaS.Modules.TenantManagement.Application;
using RezSaaS.Modules.TenantManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RezSaaS.Modules.TenantManagement;

public sealed class TenantManagementModule : ModuleBase
{
    public override string Name => "TenantManagement";

    public override void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(TenantManagementDbContext.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{TenantManagementDbContext.ConnectionStringName}' is required.");

        services.AddDbContext<TenantManagementDbContext>(
            options => options.UseNpgsql(connectionString));
        services.AddScoped<AddTenantMembershipService>();
        services.AddScoped<ChangeTenantMembershipStatusService>();
        services.AddScoped<TenantBookingAuthorizationService>();
        services.AddScoped<TenantControlPlaneQueryService>();
        services.AddScoped<CreateTenantWithOwnerService>();
        services.AddScoped<TenantLifecycleQueryService>();
    }
}
