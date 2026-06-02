using RezSaaS.Api.Configuration;
using RezSaaS.BuildingBlocks.Modularity;
using RezSaaS.Modules.Admin;
using RezSaaS.Modules.Availability;
using RezSaaS.Modules.Booking;
using RezSaaS.Modules.Catalog;
using RezSaaS.Modules.Identity;
using RezSaaS.Modules.Messaging;
using RezSaaS.Modules.Organization;
using RezSaaS.Modules.Resources;
using RezSaaS.Modules.Reviews;
using RezSaaS.Modules.TenantManagement;
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
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddModules(modules, builder.Configuration);

WebApplication app = builder.Build();

app.UseExceptionHandler();

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
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapModuleEndpoints(modules);

app.Run();

public partial class Program;
