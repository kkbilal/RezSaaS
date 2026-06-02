using RezSaaS.BuildingBlocks.Modularity;
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
    }
}
