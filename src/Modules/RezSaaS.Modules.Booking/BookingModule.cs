using RezSaaS.BuildingBlocks.Modularity;
using RezSaaS.Modules.Booking.Application;
using RezSaaS.Modules.Booking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RezSaaS.Modules.Booking;

public sealed class BookingModule : ModuleBase
{
    public override string Name => "Booking";

    public override void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(BookingDbContext.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{BookingDbContext.ConnectionStringName}' is required.");

        services.AddDbContext<BookingDbContext>(
            options => options.UseNpgsql(connectionString));
        services.AddOptions<BookingSecurityOptions>()
            .Bind(configuration.GetSection(BookingSecurityOptions.SectionName))
            .Validate(
                options => options.DefaultResponseBuffer > TimeSpan.Zero
                    && options.AppointmentRequestPermitLimit > 0
                    && options.AppointmentRequestWindowMinutes > 0
                    && options.MaxConcurrentPendingRequestsPerUser > 0
                    && options.MaxRequestsPerUserPerDay > 0,
                "Booking security options must use positive values.")
            .ValidateOnStart();
        services.AddScoped<CreateAppointmentRequestService>();
        services.AddScoped<ConfirmedAppointmentQueryService>();
        services.AddScoped<ApproveAppointmentRequestService>();
        services.AddScoped<DeclineAppointmentRequestService>();
        services.AddScoped<ExpireAppointmentRequestsService>();
    }
}
