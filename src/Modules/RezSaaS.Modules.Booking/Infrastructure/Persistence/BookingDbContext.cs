using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Booking.Domain;

namespace RezSaaS.Modules.Booking.Infrastructure.Persistence;

public sealed class BookingDbContext : DbContext
{
    private readonly ITenantContextAccessor? tenantContextAccessor;

    public const string ConnectionStringName = "BookingDatabase";

    public BookingDbContext(
        DbContextOptions<BookingDbContext> options,
        ITenantContextAccessor? tenantContextAccessor = null)
        : base(options)
    {
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public DbSet<AppointmentLine> AppointmentLines => Set<AppointmentLine>();

    public DbSet<AppointmentRequestLine> AppointmentRequestLines => Set<AppointmentRequestLine>();

    public DbSet<AppointmentRequest> AppointmentRequests => Set<AppointmentRequest>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    private Guid? CurrentTenantId => tenantContextAccessor?.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("booking");

        modelBuilder.Entity<AppointmentRequest>(request =>
        {
            request.ToTable(
                "AppointmentRequests",
                table => table.HasCheckConstraint(
                    "CK_AppointmentRequests_EndAfterStart",
                    "\"RequestedEndUtc\" > \"RequestedStartUtc\""));
            request.HasKey(entity => entity.Id);
            request.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            request.HasMany(entity => entity.Lines)
                .WithOne()
                .HasForeignKey(entity => entity.AppointmentRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            request.HasIndex(entity => new { entity.TenantId, entity.BranchId, entity.RequestedStartUtc });
            request.HasIndex(entity => new { entity.TenantId, entity.CustomerUserAccountId, entity.Status });
            request.HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
        });

        modelBuilder.Entity<AppointmentRequestLine>(line =>
        {
            line.ToTable("AppointmentRequestLines");
            line.HasKey(entity => entity.Id);
            line.Property(entity => entity.ServiceNameSnapshot).HasMaxLength(200).IsRequired();
            line.Property(entity => entity.CurrencyCode).HasMaxLength(3).IsRequired();
            line.Property(entity => entity.PriceAmount).HasPrecision(12, 2);
            line.HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
        });

        modelBuilder.Entity<Appointment>(appointment =>
        {
            appointment.ToTable(
                "Appointments",
                table => table.HasCheckConstraint(
                    "CK_Appointments_EndAfterStart",
                    "\"EndUtc\" > \"StartUtc\""));
            appointment.HasKey(entity => entity.Id);
            appointment.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            appointment.HasMany(entity => entity.Lines)
                .WithOne()
                .HasForeignKey(entity => entity.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);
            appointment.HasIndex(entity => new { entity.TenantId, entity.BranchId, entity.StartUtc });
            appointment.HasIndex(entity => new { entity.TenantId, entity.StaffMemberId, entity.StartUtc });
            appointment.HasIndex(entity => new { entity.TenantId, entity.ResourceId, entity.StartUtc });
            appointment.HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
        });

        modelBuilder.Entity<AppointmentLine>(line =>
        {
            line.ToTable("AppointmentLines");
            line.HasKey(entity => entity.Id);
            line.Property(entity => entity.ServiceNameSnapshot).HasMaxLength(200).IsRequired();
            line.Property(entity => entity.CurrencyCode).HasMaxLength(3).IsRequired();
            line.Property(entity => entity.PriceAmount).HasPrecision(12, 2);
            line.HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
        });
    }
}
