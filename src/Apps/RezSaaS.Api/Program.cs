using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using RezSaaS.Api.Admin;
using RezSaaS.Api.Business;
using RezSaaS.Api.Configuration;
using RezSaaS.Api.Customer;
using RezSaaS.Api.PublicApi;
using RezSaaS.Api.Session;
using RezSaaS.BuildingBlocks.Modularity;
using RezSaaS.BuildingBlocks.Persistence;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Admin;
using RezSaaS.Modules.Availability;
using RezSaaS.Modules.Booking;
using RezSaaS.Modules.Booking.Application;
using RezSaaS.Modules.Catalog;
using RezSaaS.Modules.Identity;
using RezSaaS.Modules.Messaging;
using RezSaaS.Modules.Organization;
using RezSaaS.Modules.Organization.Application;
using RezSaaS.Modules.Resources;
using RezSaaS.Modules.Reviews;
using RezSaaS.Modules.TenantManagement;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.HttpOverrides;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddLocalDevelopmentEnvironment(builder.Environment);

IModule[] modules =
[
    new IdentityModule(),
    new TenantManagementModule(),
    new OrganizationModule(),
    new CatalogModule(),
    new ResourcesModule(),
    new AvailabilityModule(),
    new BookingModule(),
    new MessagingModule(),
    new ReviewsModule(),
    // Integrations and Payments modules disabled until Phase 4/5
    // new IntegrationsModule(),
    // new PaymentsModule(),
    new AdminModule(),
];

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks()
    .AddCheck<PlatformOperationsHealthCheck>(
        "platform-operations",
        tags: ["operations"]);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "RezSaaS API",
            Version = "v1",
            Description = "RezSaaS modular monolith API. Swagger is exposed only in Development.",
        });
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Description = "Paste only the access token returned from /api/auth/login?useCookies=false.",
            In = ParameterLocation.Header,
            Name = "Authorization",
            Scheme = "bearer",
            Type = SecuritySchemeType.Http,
        });
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddModules(modules, builder.Configuration);

// Bekleyen migration'lari acilista uygular. AddModules'tan SONRA cagrilmali:
// DbContext'ler servis koleksiyonu taranarak bulunuyor, once kaydedilmis olmalilar.
// Coklu instance guvenli (session-level advisory lock). Kapatmak icin:
// Database:AutoMigrateOnStartup = false
builder.Services.AddDatabaseMigration(builder.Configuration);

builder.Services.AddOptions<PublicSlotSearchOptions>()
    .Bind(builder.Configuration.GetSection(PublicSlotSearchOptions.SectionName))
    .Validate(
        options => options.SlotIntervalMinutes > 0 && options.MaxSlots > 0,
        "Public slot search options must use positive values.")
    .ValidateOnStart();
builder.Services.AddOptions<AppointmentRequestExpiryWorkerOptions>()
    .Bind(builder.Configuration.GetSection(AppointmentRequestExpiryWorkerOptions.SectionName))
    .Validate(
        options => options.Interval > TimeSpan.Zero
            && options.InitialDelay >= TimeSpan.Zero
            && options.TenantBatchSize > 0,
        "Appointment request expiry worker options must use positive values.")
    .ValidateOnStart();
builder.Services.AddOptions<UnsafeRequestOriginOptions>()
    .Bind(builder.Configuration.GetSection(UnsafeRequestOriginOptions.SectionName));
builder.Services.AddOptions<CustomerAbuseRateLimitOptions>()
    .Bind(builder.Configuration.GetSection(CustomerAbuseRateLimitOptions.SectionName))
    .Validate(
        options => options.PermitLimit > 0 && options.WindowMinutes > 0,
        "Customer abuse action rate limit options must use positive values.")
    .ValidateOnStart();
builder.Services.AddOptions<SessionRateLimitOptions>()
    .Bind(builder.Configuration.GetSection(SessionRateLimitOptions.SectionName))
    .Validate(
        options => options.PermitLimit > 0 && options.WindowMinutes > 0,
        "Session rate limit options must use positive values.")
    .ValidateOnStart();
builder.Services.AddOptions<PlatformNotificationWorkerOptions>()
    .Bind(builder.Configuration.GetSection(PlatformNotificationWorkerOptions.SectionName))
    .Validate(
        options => options.BatchSize > 0
            && options.InitialDelay >= TimeSpan.Zero
            && options.Interval > TimeSpan.Zero
            && options.LockDuration > TimeSpan.Zero
            && options.MaxAttempts > 0
            && options.RetryDelay > TimeSpan.Zero,
        "Platform notification worker options must use positive values.")
    .ValidateOnStart();
builder.Services.AddOptions<PlatformOperationsReconciliationOptions>()
    .Bind(builder.Configuration.GetSection(PlatformOperationsReconciliationOptions.SectionName))
    .Validate(
        options => options.CallbackPendingThreshold > TimeSpan.Zero
            && options.ClosureExecutionStallThreshold > TimeSpan.Zero
            && options.InitialDelay >= TimeSpan.Zero
            && options.Interval > TimeSpan.Zero
            && options.NotificationOverdueThreshold > TimeSpan.Zero
            && options.SampleSize > 0
            && options.SampleSize <= 100
            && options.StaleProcessingThreshold > TimeSpan.Zero,
        "Platform operations reconciliation options must use positive values.")
    .ValidateOnStart();
builder.Services.AddScoped<PublicBusinessProfileComposer>();
builder.Services.AddScoped<PublicReviewComposer>();
builder.Services.AddScoped<PublicSlotSearchComposer>();
builder.Services.AddScoped<RezSaaS.Api.Customer.CustomerCreateReviewComposer>();
builder.Services.AddScoped<RezSaaS.Api.Business.BusinessReviewComposer>();

// Reviews cross-module contract adapters (composition root only).
// These bridge Reviews -> Booking/Organization/Identity without direct module references.
builder.Services.AddScoped<RezSaaS.BuildingBlocks.Reviews.ICompletedAppointmentLookup, RezSaaS.Api.Reviews.BookingCompletedAppointmentLookupAdapter>();
builder.Services.AddScoped<RezSaaS.BuildingBlocks.Reviews.IBusinessRatingSummarySink, RezSaaS.Api.Reviews.OrganizationBusinessRatingSummarySinkAdapter>();
builder.Services.AddScoped<RezSaaS.BuildingBlocks.Reviews.ICustomerDisplayNameResolver, RezSaaS.Api.Reviews.IdentityCustomerDisplayNameResolverAdapter>();
builder.Services.AddScoped<PublicAppointmentRequestComposer>();
builder.Services.AddScoped<PublicAppointmentCancellationComposer>();
// Booking -> Organization modul-arasi sozlesmesi (iptal politikasi). Moduller birbirini
// tanimaz; adapter composition root'ta baglanir (Reviews adapter'leriyle ayni kalip).
builder.Services.AddScoped<
    RezSaaS.BuildingBlocks.Booking.IBusinessCancellationPolicyLookup,
    RezSaaS.Api.Booking.OrganizationBusinessCancellationPolicyAdapter>();
builder.Services.AddScoped<SessionBootstrapComposer>();
builder.Services.AddScoped<BusinessContextComposer>();
builder.Services.AddScoped<BusinessAppointmentRequestComposer>();
builder.Services.AddScoped<BusinessAbuseReportComposer>();
builder.Services.AddScoped<BusinessAppointmentComposer>();
builder.Services.AddScoped<BusinessResourceComposer>();
builder.Services.AddScoped<BusinessResourceTypeComposer>();
builder.Services.AddScoped<BusinessBranchComposer>();
builder.Services.AddScoped<BusinessStaffComposer>();
builder.Services.AddScoped<BusinessSkillComposer>();
builder.Services.AddScoped<BusinessServiceComposer>();
builder.Services.AddScoped<BusinessVariantComposer>();
builder.Services.AddScoped<BusinessSettingsComposer>();
builder.Services.AddScoped<BusinessWorkingHoursComposer>();
builder.Services.AddScoped<BusinessStaffUnavailableComposer>();
builder.Services.AddScoped<AdminTenantProvisioningComposer>();
builder.Services.AddScoped<AdminAbuseControlPlaneComposer>();
builder.Services.AddScoped<AdminAbuseReportComposer>();
builder.Services.AddScoped<AdminAbuseWorkflowComposer>();
builder.Services.AddScoped<CustomerAbuseComposer>();
builder.Services.AddScoped<CustomerAppointmentHistoryComposer>();
builder.Services.AddScoped<PlatformNotificationDispatchService>();
builder.Services.AddScoped<PlatformOperationsReconciliationService>();
builder.Services.AddHostedService<AppointmentRequestExpiryHostedService>();
builder.Services.AddHostedService<PlatformNotificationHostedService>();
builder.Services.AddHostedService<PlatformOperationsReconciliationHostedService>();
BookingSecurityOptions bookingSecurityOptions =
    builder.Configuration.GetSection(BookingSecurityOptions.SectionName).Get<BookingSecurityOptions>()
    ?? new BookingSecurityOptions();
CustomerAbuseRateLimitOptions customerAbuseRateLimitOptions =
    builder.Configuration.GetSection(CustomerAbuseRateLimitOptions.SectionName)
        .Get<CustomerAbuseRateLimitOptions>()
    ?? new CustomerAbuseRateLimitOptions();
SessionRateLimitOptions sessionRateLimitOptions =
    builder.Configuration.GetSection(SessionRateLimitOptions.SectionName)
        .Get<SessionRateLimitOptions>()
    ?? new SessionRateLimitOptions();

// CORS: browser clients (Next.js on http://localhost:3000) call the API directly
// cross-origin to http://localhost:5252. The allowed origins reuse the same
// Security:UnsafeRequestOrigins:AllowedOrigins config that backs the server-side
// UnsafeRequestOriginMiddleware, keeping a single source of truth. Because the
// browser client uses credentials:"include" (cookie auth per AGENTS.md), the
// policy must echo the specific origin and call AllowCredentials(); a wildcard
// "*" is not valid with credentials. When no origins are configured (production
// default), the policy allows nothing — fail-closed.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        UnsafeRequestOriginOptions originOptions =
            builder.Configuration.GetSection(UnsafeRequestOriginOptions.SectionName)
                .Get<UnsafeRequestOriginOptions>()
            ?? new UnsafeRequestOriginOptions();

        policy.SetPreflightMaxAge(TimeSpan.FromMinutes(5));

        if (originOptions.AllowedOrigins.Length > 0)
        {
            policy.WithOrigins(originOptions.AllowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    PublicBusinessDirectoryOptions publicBusinessDirectoryOptions =
        builder.Configuration.GetSection(PublicBusinessDirectoryOptions.SectionName)
            .Get<PublicBusinessDirectoryOptions>()
        ?? new PublicBusinessDirectoryOptions();
    options.AddPolicy(OrganizationRateLimitPolicyNames.PublicDiscovery, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = publicBusinessDirectoryOptions.PermitLimit,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(publicBusinessDirectoryOptions.WindowMinutes),
            }));
    options.AddPolicy(BookingRateLimitPolicyNames.AppointmentRequests, httpContext =>
    {
        string remoteIpAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        string tenantOrPublicBusiness = httpContext.Request.Headers[TenantContextHeaders.TenantId].FirstOrDefault()
            ?? httpContext.Request.RouteValues["slug"]?.ToString()
            ?? "tenant-missing";
        string userId = httpContext.User.FindFirst("sub")?.Value
            ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            $"{remoteIpAddress}:{tenantOrPublicBusiness}:{userId}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = bookingSecurityOptions.AppointmentRequestPermitLimit,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(bookingSecurityOptions.AppointmentRequestWindowMinutes),
            });
    });
    options.AddPolicy(BookingRateLimitPolicyNames.BusinessDecisions, httpContext =>
    {
        string remoteIpAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        string tenantId = httpContext.Request.Headers[TenantContextHeaders.TenantId].FirstOrDefault()
            ?? "tenant-missing";
        string userId = httpContext.User.FindFirst("sub")?.Value
            ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            $"{remoteIpAddress}:{tenantId}:{userId}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = bookingSecurityOptions.BusinessDecisionPermitLimit,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(bookingSecurityOptions.BusinessDecisionWindowMinutes),
            });
    });
    options.AddPolicy(AdminControlPlaneRateLimitPolicyNames.Bootstrap, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(15),
            }));
    options.AddPolicy(AdminControlPlaneRateLimitPolicyNames.Operations, httpContext =>
    {
        string remoteIpAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        string userId = httpContext.User.FindFirst("sub")?.Value
            ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            $"{remoteIpAddress}:{userId}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1),
            });
    });
    options.AddPolicy(SessionRateLimitPolicyNames.Bootstrap, httpContext =>
    {
        string remoteIpAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        string userId = httpContext.User.FindFirst("sub")?.Value
            ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            $"{remoteIpAddress}:{userId}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = sessionRateLimitOptions.PermitLimit,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(sessionRateLimitOptions.WindowMinutes),
            });
    });
    options.AddPolicy(CustomerAbuseRateLimitPolicyNames.Actions, httpContext =>
    {
        string remoteIpAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        string userId = httpContext.User.FindFirst("sub")?.Value
            ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            $"{remoteIpAddress}:{userId}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = customerAbuseRateLimitOptions.PermitLimit,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(customerAbuseRateLimitOptions.WindowMinutes),
            });
    });
});

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "RezSaaS API";
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "RezSaaS API v1");
    });
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    // In production, configure KnownProxies/KnownNetworks from config
    // to prevent XFF spoofing. For single-proxy deployment:
    // options.KnownProxies.Add(IPAddress.Parse("your-proxy-ip"));
});
// CORS must run before authentication/authorization so that preflight OPTIONS
// requests are answered with the appropriate Access-Control-* headers and
// short-circuited, instead of being rejected by auth middleware (401). This is
// what fixes the "blocked by CORS policy: No 'Access-Control-Allow-Origin'
// header" preflight failure observed from the browser client.
app.UseCors();
app.UseMiddleware<TenantContextMiddleware>();
app.UseMiddleware<UnsafeRequestOriginMiddleware>();
app.UseAuthentication();
app.UseRateLimiter();
app.UseMiddleware<ActiveUserAccountMiddleware>();
app.UseAuthorization();
app.MapHealthChecks(
    "/health",
    new HealthCheckOptions
    {
        Predicate = registration => !registration.Tags.Contains("operations"),
    });
app.MapHealthChecks(
    "/health/operations",
    new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("operations"),
    })
    .RequireRateLimiting(AdminControlPlaneRateLimitPolicyNames.Operations);
app.MapPublicBusinessProfileEndpoints();
app.MapPublicReviewEndpoints();
app.MapPublicBusinessSlotEndpoints();
app.MapPublicAppointmentRequestEndpoints();
app.MapSessionEndpoints();
app.MapBusinessContextEndpoints();
app.MapBusinessAppointmentRequestEndpoints();
app.MapBusinessAppointmentEndpoints();
app.MapBusinessResourceEndpoints();
app.MapBusinessResourceTypeEndpoints();
app.MapBusinessBranchEndpoints();
app.MapBusinessStaffEndpoints();
app.MapBusinessSkillEndpoints();
app.MapBusinessServiceEndpoints();
app.MapBusinessVariantEndpoints();
app.MapBusinessWorkingHoursEndpoints();
app.MapBusinessStaffUnavailableEndpoints();
app.MapBusinessSettingsEndpoints();
app.MapBusinessReviewEndpoints();
app.MapCustomerAppointmentHistoryEndpoints();
app.MapCustomerAbuseEndpoints();
app.MapCustomerReviewEndpoints();
app.MapAdminControlPlaneEndpoints();
app.MapAdminAbuseControlPlaneEndpoints();
app.MapAdminAbuseWorkflowEndpoints();
app.MapAdminOperationsEndpoints();
// Integrations and Payments endpoints disabled until Phase 4/5
// app.MapAdminIntegrationOperationsEndpoints();
// app.MapAdminPaymentOperationsEndpoints();
app.MapModuleEndpoints(modules);

app.Run();

public partial class Program;