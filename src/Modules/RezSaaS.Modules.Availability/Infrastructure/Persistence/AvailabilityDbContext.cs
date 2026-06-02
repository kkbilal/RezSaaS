using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Availability.Domain;

namespace RezSaaS.Modules.Availability.Infrastructure.Persistence;

public sealed class AvailabilityDbContext : DbContext
{
    private readonly ITenantContextAccessor? tenantContextAccessor;

    public const string ConnectionStringName = "AvailabilityDatabase";

    public AvailabilityDbContext(
        DbContextOptions<AvailabilityDbContext> options,
        ITenantContextAccessor? tenantContextAccessor = null)
        : base(options)
    {
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public DbSet<BranchWorkingHours> BranchWorkingHours => Set<BranchWorkingHours>();

    public DbSet<StaffUnavailableTime> StaffUnavailableTimes => Set<StaffUnavailableTime>();

    private Guid? CurrentTenantId => tenantContextAccessor?.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("availability");

        modelBuilder.Entity<BranchWorkingHours>(workingHours =>
        {
            workingHours.ToTable("BranchWorkingHours");
            workingHours.HasKey(entity => entity.Id);
            workingHours.Property(entity => entity.DayOfWeek).HasConversion<string>().HasMaxLength(16).IsRequired();
            workingHours.HasIndex(entity => new { entity.TenantId, entity.BranchId, entity.DayOfWeek }).IsUnique();
            workingHours.HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
        });

        modelBuilder.Entity<StaffUnavailableTime>(unavailable =>
        {
            unavailable.ToTable(
                "StaffUnavailableTimes",
                table => table.HasCheckConstraint(
                    "CK_StaffUnavailableTimes_EndAfterStart",
                    "\"EndUtc\" > \"StartUtc\""));
            unavailable.HasKey(entity => entity.Id);
            unavailable.Property(entity => entity.Reason).HasMaxLength(200).IsRequired();
            unavailable.HasIndex(entity => new { entity.TenantId, entity.StaffMemberId, entity.StartUtc });
            unavailable.HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
        });
    }
}
