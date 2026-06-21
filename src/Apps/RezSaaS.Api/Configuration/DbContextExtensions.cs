using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RezSaaS.Api.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring DbContext with tenant stamping interceptor.
/// </summary>
public static class DbContextExtensions
{
    /// <summary>
    /// Helper method to configure DbContext options with interceptor.
    /// Use this in module DbContext registration: options.AddTenantStampingInterceptor(serviceProvider)
    /// </summary>
    public static DbContextOptionsBuilder AddTenantStampingInterceptor(
        this DbContextOptionsBuilder optionsBuilder,
        IServiceProvider serviceProvider)
    {
        var interceptor = serviceProvider.GetRequiredService<TenantStampingInterceptor>();
        optionsBuilder.AddInterceptors(interceptor);
        return optionsBuilder;
    }
}