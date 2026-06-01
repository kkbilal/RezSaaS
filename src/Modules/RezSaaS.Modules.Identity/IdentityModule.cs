using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RezSaaS.BuildingBlocks.Modularity;
using RezSaaS.Modules.Identity.Domain;
using RezSaaS.Modules.Identity.Infrastructure.Email;
using RezSaaS.Modules.Identity.Infrastructure.Persistence;
using RezSaaS.Modules.Identity.Infrastructure.Security;

namespace RezSaaS.Modules.Identity;

public sealed class IdentityModule : ModuleBase
{
    private const string AuthenticationRateLimitPolicy = "identity-authentication";
    private const int DefaultAuthenticationPermitLimit = 10;

    public override string Name => "Identity";

    public override void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(IdentityDbContext.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{IdentityDbContext.ConnectionStringName}' is required.");
        bool requireConfirmedEmail =
            configuration.GetValue("Identity:RequireConfirmedEmail", defaultValue: true);
        string emailDeliveryMode =
            configuration.GetValue<string>("Identity:EmailDeliveryMode") ?? "Unconfigured";
        int authenticationPermitLimit =
            configuration.GetValue("Identity:AuthenticationPermitLimit", DefaultAuthenticationPermitLimit);

        if (requireConfirmedEmail && emailDeliveryMode == "Unconfigured")
        {
            throw new InvalidOperationException(
                "An email provider must be configured when confirmed email is required.");
        }

        services.AddDbContext<IdentityDbContext>(options => options.UseNpgsql(connectionString));
        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuthorizationPolicies.PlatformAdminOnly,
                policy => policy.RequireRole(PlatformRoles.PlatformAdmin));
            options.AddPolicy(
                AuthorizationPolicies.PlatformSupportOrAdmin,
                policy => policy.RequireRole(
                    PlatformRoles.PlatformAdmin,
                    PlatformRoles.PlatformSupport));
        });
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthenticationRateLimitPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = authenticationPermitLimit,
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(1),
                    }));
        });

        services
            .AddIdentityApiEndpoints<UserAccount>(options =>
            {
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Password.RequiredLength = 12;
                options.Password.RequiredUniqueChars = 4;
                options.SignIn.RequireConfirmedEmail = requireConfirmedEmail;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<IdentityDbContext>();

        services.AddScoped<SignInManager<UserAccount>, UserAccountSignInManager>();
        services.AddSingleton<IEmailSender<UserAccount>>(
            emailDeliveryMode == "DevelopmentSink"
                ? new DevelopmentSinkEmailSender()
                : new UnconfiguredEmailSender());
    }

    public override void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder authentication = endpoints
            .MapGroup("/api/auth")
            .RequireRateLimiting(AuthenticationRateLimitPolicy);

        authentication.MapIdentityApi<UserAccount>();
    }
}
