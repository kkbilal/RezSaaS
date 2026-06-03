using RezSaaS.BuildingBlocks.Abuse;
using RezSaaS.BuildingBlocks.Auditing;
using RezSaaS.BuildingBlocks.Modularity;
using RezSaaS.Modules.Admin.Infrastructure.Abuse;
using RezSaaS.Modules.Admin.Infrastructure.Auditing;
using RezSaaS.Modules.Admin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RezSaaS.Modules.Admin;

public sealed class AdminModule : ModuleBase
{
    public override string Name => "Admin";

    public override void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(AdminDbContext.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{AdminDbContext.ConnectionStringName}' is required.");

        services.AddDbContext<AdminDbContext>(
            options => options.UseNpgsql(connectionString));
        services.AddScoped<IAbuseEventRecorder, AdminAbuseEventRecorder>();
        services.AddScoped<IAuditLogRecorder, AdminAuditLogRecorder>();
    }
}
