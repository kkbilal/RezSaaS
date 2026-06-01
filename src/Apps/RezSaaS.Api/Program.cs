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

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddModules(modules, builder.Configuration);

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.MapHealthChecks("/health");
app.MapModuleEndpoints(modules);

app.Run();

public partial class Program;
