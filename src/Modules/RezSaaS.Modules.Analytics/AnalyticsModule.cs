using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RezSaaS.BuildingBlocks.Modularity;

namespace RezSaaS.Modules.Analytics;

public sealed class AnalyticsModule : IModule
{
    public string Name => "Analytics";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        // Analytics module services will be registered here
        // Domain and infrastructure services will be added later
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Analytics read-only endpoints will be mapped here in a later phase.
    }
}