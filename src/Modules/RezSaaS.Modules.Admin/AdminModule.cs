using RezSaaS.BuildingBlocks.Abuse;
using RezSaaS.BuildingBlocks.Auditing;
using RezSaaS.BuildingBlocks.Modularity;
using RezSaaS.Modules.Admin.Infrastructure.Abuse;
using RezSaaS.Modules.Admin.Infrastructure.Auditing;
using RezSaaS.Modules.Admin.Infrastructure.Persistence;
using RezSaaS.Modules.Admin.Application;
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
        services.AddOptions<AbuseRiskOptions>()
            .Bind(configuration.GetSection(AbuseRiskOptions.SectionName))
            .Validate(
                options => options.StrikeLifetimeDays > 0
                    && options.MaxBusinessReportsPerActorPerDay > 0
                    && options.ClosureAppealWindowDays > 0
                    && options.MaxOpenAppealsPerUser > 0
                    && options.ElevatedStrikeThreshold > 0
                    && options.HighStrikeThreshold > options.ElevatedStrikeThreshold,
                "Abuse risk options must use positive and ordered values.")
            .ValidateOnStart();
        services.AddScoped<AbuseControlPlaneQueryService>();
        services.AddScoped<AbuseReportQueryService>();
        services.AddScoped<AbuseWorkflowQueryService>();
        services.AddScoped<AccountClosureNoticeDeliveryService>();
        services.AddScoped<AccountClosureExecutionService>();
        services.AddScoped<ApplyUserSanctionService>();
        services.AddScoped<CreateAbuseAppealService>();
        services.AddScoped<CreateBusinessAbuseReportService>();
        services.AddScoped<ProposeAccountClosureService>();
        services.AddScoped<ReviewAbuseAppealService>();
        services.AddScoped<ReviewAccountClosureService>();
        services.AddScoped<ReviewBusinessAbuseReportService>();
        services.AddScoped<RevokeUserSanctionService>();
        services.AddScoped<RevokeUserStrikeService>();
        services.AddScoped<IAbuseEventRecorder, AdminAbuseEventRecorder>();
        services.AddScoped<IUserBookingRestrictionEvaluator, AdminUserBookingRestrictionEvaluator>();
        services.AddScoped<IAuditLogRecorder, AdminAuditLogRecorder>();
    }
}
