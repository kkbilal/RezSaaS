using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using RezSaaS.Api.Admin;
using RezSaaS.Api.Business;
using RezSaaS.Api.Configuration;
using RezSaaS.Api.Customer;
using RezSaaS.Api.PublicApi;
using RezSaaS.BuildingBlocks.Modularity;
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
builder.Services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddModules(modules, builder.Configuration);
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
builder.Services.AddScoped<PublicSlotSearchComposer>();
builder.Services.AddScoped<PublicAppointmentRequestComposer>();
builder.Services.AddScoped<BusinessAppointmentRequestComposer>();
builder.Services.AddScoped<BusinessAbuseReportComposer>();
builder.Services.AddScoped<AdminTenantProvisioningComposer>();
builder.Services.AddScoped<AdminAbuseControlPlaneComposer>();
builder.Services.AddScoped<AdminAbuseReportComposer>();
builder.Services.AddScoped<AdminAbuseWorkflowComposer>();
builder.Services.AddScoped<CustomerAbuseComposer>();
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
app.MapPublicBusinessSlotEndpoints();
app.MapPublicAppointmentRequestEndpoints();
app.MapBusinessAppointmentRequestEndpoints();
app.MapCustomerAbuseEndpoints();
app.MapAdminControlPlaneEndpoints();
app.MapAdminAbuseControlPlaneEndpoints();
app.MapAdminAbuseWorkflowEndpoints();
app.MapAdminOperationsEndpoints();
app.MapModuleEndpoints(modules);

app.Run();

public partial class Program;
