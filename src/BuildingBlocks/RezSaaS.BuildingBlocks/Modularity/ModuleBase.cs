using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RezSaaS.BuildingBlocks.Modularity;

public abstract class ModuleBase : IModule
{
    public abstract string Name { get; }

    public virtual void AddServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public virtual void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
