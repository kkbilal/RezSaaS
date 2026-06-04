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
using RezSaaS.Modules.Identity.Application;
using RezSaaS.Modules.Identity.Configuration;
using RezSaaS.Modules.Identity.Domain;
using RezSaaS.Modules.Identity.Infrastructure.Email;
using RezSaaS.Modules.Identity.Infrastructure.Persistence;
using RezSaaS.Modules.Identity.Infrastructure.Security;

namespace RezSaaS.Modules.Identity;

public sealed class IdentityModule : ModuleBase
{
    private const string AuthenticationRateLimitPolicy = "identity-authentication";

    public override string Name => "Identity";

    public override void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(IdentityDbContext.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{IdentityDbContext.ConnectionStringName}' is required.");
        IdentitySecurityOptions identityOptions =
            configuration.GetSection(IdentitySecurityOptions.SectionName)
                .Get<IdentitySecurityOptions>()
            ?? throw new InvalidOperationException(
                $"Configuration section '{IdentitySecurityOptions.SectionName}' is required.");

        identityOptions.Validate();
        IdentitySmtpEmailOptions smtpOptions =
            configuration.GetSection(IdentitySmtpEmailOptions.SectionName)
                .Get<IdentitySmtpEmailOptions>()
            ?? new IdentitySmtpEmailOptions();

        if (identityOptions.DeliveryMode == EmailDeliveryMode.Smtp)
        {
            smtpOptions.Validate();
        }

        services.AddDbContext<IdentityDbContext>(options => options.UseNpgsql(connectionString));
        services.Configure<PlatformAdminBootstrapOptions>(
            configuration.GetSection(PlatformAdminBootstrapOptions.SectionName));
        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuthorizationPolicies.PlatformAdminOnly,
                policy => policy.RequireRole(PlatformRoleNames.Administrator));
            options.AddPolicy(
                AuthorizationPolicies.PlatformAdminWithStepUp,
                policy => policy
                    .RequireRole(PlatformRoleNames.Administrator)
                    .RequireClaim("amr", "mfa"));
            options.AddPolicy(
                AuthorizationPolicies.PlatformSupportOrAdmin,
                policy => policy.RequireRole(
                    PlatformRoleNames.Administrator,
                    PlatformRoleNames.Support));
        });
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthenticationRateLimitPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = identityOptions.AuthenticationPermitLimit,
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(identityOptions.AuthenticationWindowMinutes),
                    }));
        });

        services
            .AddIdentityApiEndpoints<UserAccount>(options =>
            {
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(identityOptions.LockoutMinutes);
                options.Lockout.MaxFailedAccessAttempts = identityOptions.MaxFailedAccessAttempts;
                options.Password.RequiredLength = identityOptions.PasswordRequiredLength;
                options.Password.RequiredUniqueChars = identityOptions.PasswordRequiredUniqueChars;
                options.SignIn.RequireConfirmedEmail = identityOptions.RequireConfirmedEmail;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<IdentityDbContext>();

        services.AddScoped<SignInManager<UserAccount>, UserAccountSignInManager>();
        services.AddScoped<CustomerAccountLookupService>();
        services.AddScoped<UserAccountClosureService>();
        services.AddScoped<UserAccountExistenceService>();
        services.AddScoped<UserTransactionalEmailService>();
        services.AddScoped<IPlatformAdminBootstrapService, PlatformAdminBootstrapService>();
        object emailSender = identityOptions.DeliveryMode switch
        {
            EmailDeliveryMode.DevelopmentSink => new DevelopmentSinkEmailSender(),
            EmailDeliveryMode.Smtp => new SmtpEmailSender(smtpOptions),
            _ => new UnconfiguredEmailSender(),
        };
        services.AddSingleton((IEmailSender<UserAccount>)emailSender);
        services.AddSingleton((IUserTransactionalEmailSender)emailSender);
    }

    public override void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder authentication = endpoints
            .MapGroup("/api/auth")
            .RequireRateLimiting(AuthenticationRateLimitPolicy);

        authentication.MapIdentityApi<UserAccount>();
    }
}
