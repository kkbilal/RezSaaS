using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RezSaaS.BuildingBlocks.Modularity;

public static class ModuleCollectionExtensions
{
    public static IServiceCollection AddModules(
        this IServiceCollection services,
        IEnumerable<IModule> modules,
        IConfiguration configuration)
    {
        foreach (IModule module in modules)
        {
            module.AddServices(services, configuration);
        }

        return services;
    }

    public static IEndpointRouteBuilder MapModuleEndpoints(
        this IEndpointRouteBuilder endpoints,
        IEnumerable<IModule> modules)
    {
        foreach (IModule module in modules)
        {
            module.MapEndpoints(endpoints);
        }

        return endpoints;
    }
}
