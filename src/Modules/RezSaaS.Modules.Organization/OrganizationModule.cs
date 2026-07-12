using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RezSaaS.BuildingBlocks.Modularity;
using RezSaaS.Modules.Organization.Application;
using RezSaaS.Modules.Organization.Infrastructure.Persistence;
using RezSaaS.Modules.TenantManagement.Application;
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
        services.AddOptions<PublicBusinessDirectoryOptions>()
            .Bind(configuration.GetSection(PublicBusinessDirectoryOptions.SectionName))
            .Validate(
                options => options.PermitLimit > 0
                    && options.WindowMinutes > 0
                    && options.DefaultTake > 0
                    && options.MaxTake >= options.DefaultTake,
                "Public discovery options must use positive values and MaxTake must be at least DefaultTake.")
            .ValidateOnStart();
        services.AddScoped<PublicBusinessDirectoryService>();
        services.AddScoped<BusinessEntityLabelQueryService>();
        services.AddScoped<BusinessProfileSettingsService>();
        services.AddScoped<BusinessProvisioningService>();
        services.AddScoped<BranchManagementService>();
        services.AddScoped<StaffManagementService>();
        services.AddScoped<SkillManagementService>();
        services.AddScoped<StaffSkillService>();
        services.AddScoped<TenantLifecycleQueryService>();
    }

    public override void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder publicBusinesses = endpoints
            .MapGroup("/api/public/businesses")
            .WithTags("Public Businesses")
            .RequireRateLimiting(OrganizationRateLimitPolicyNames.PublicDiscovery);

        publicBusinesses.MapGet(
                string.Empty,
                async (
                    string? searchText,
                    string? categoryKey,
                    string? city,
                    string? district,
                    int? take,
                    PublicBusinessDirectoryService directoryService,
                    TenantLifecycleQueryService tenantLifecycleService,
                    CancellationToken cancellationToken) =>
                {
                    IReadOnlyCollection<PublicBusinessSummaryView> businesses =
                        await directoryService.SearchAsync(
                            new PublicBusinessSearchQuery(
                                searchText,
                                categoryKey,
                                city,
                                district,
                                take),
                            cancellationToken);

                    IReadOnlyCollection<Guid> activeTenantIds =
                        await tenantLifecycleService.GetActiveTenantIdsAsync(500, cancellationToken);

                    IReadOnlyCollection<PublicBusinessSummaryView> activeBusinesses =
                        businesses
                            .Where(business => activeTenantIds.Contains(business.TenantId))
                            .ToList();

                    return Results.Ok(activeBusinesses);
                })
            .Produces<IReadOnlyCollection<PublicBusinessSummaryView>>();
    }
}
