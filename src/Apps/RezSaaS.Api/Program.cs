using System.Security.Claims;
using System.Threading.RateLimiting;
using RezSaaS.Api.Configuration;
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
builder.Services.AddHealthChecks();
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
builder.Services.AddScoped<PublicBusinessProfileComposer>();
builder.Services.AddScoped<PublicSlotSearchComposer>();
builder.Services.AddScoped<PublicAppointmentRequestComposer>();
BookingSecurityOptions bookingSecurityOptions =
    builder.Configuration.GetSection(BookingSecurityOptions.SectionName).Get<BookingSecurityOptions>()
    ?? new BookingSecurityOptions();
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

app.UseRateLimiter();
app.UseMiddleware<TenantContextMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapPublicBusinessProfileEndpoints();
app.MapPublicBusinessSlotEndpoints();
app.MapPublicAppointmentRequestEndpoints();
app.MapModuleEndpoints(modules);

app.Run();

public partial class Program;
