using Microsoft.EntityFrameworkCore;
using RezSaaS.Modules.Analytics.Domain.ReadModels;

namespace RezSaaS.Modules.Analytics.Infrastructure.Persistence;

public sealed class AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : DbContext(options)
{
    public DbSet<DailyBusinessMetrics> DailyBusinessMetrics { get; init; }
    public DbSet<TopServiceMetrics> TopServiceMetrics { get; init; }
    public DbSet<ResourceCapacityMetrics> ResourceCapacityMetrics { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureDailyBusinessMetrics(modelBuilder);
        ConfigureTopServiceMetrics(modelBuilder);
        ConfigureResourceCapacityMetrics(modelBuilder);
    }

    private static void ConfigureDailyBusinessMetrics(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DailyBusinessMetrics>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.DateUtc).IsRequired();
            entity.Property(e => e.GeneratedAtUtc).IsRequired();
            
            entity.Property(e => e.OccupancyRate).HasPrecision(5, 2);
            entity.Property(e => e.UtilizationRate).HasPrecision(5, 2);
            entity.Property(e => e.RequestToApprovalRate).HasPrecision(5, 2);
            entity.Property(e => e.NoShowRate).HasPrecision(5, 2);
            
            // Unique index for tenant+branch+date
            entity.HasIndex(e => new { e.TenantId, e.BranchId, e.DateUtc })
                .IsUnique()
                .HasDatabaseName("IX_Analytics_DailyBusinessMetrics_Tenant_Branch_Date");
        });
    }

    private static void ConfigureTopServiceMetrics(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TopServiceMetrics>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.ServiceId).IsRequired();
            entity.Property(e => e.ServiceName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.VariantName).HasMaxLength(200);
            entity.Property(e => e.TotalRevenue).HasPrecision(18, 2);
            entity.Property(e => e.AverageServiceDurationMinutes).HasPrecision(10, 2);
            entity.Property(e => e.Ranking).IsRequired();
            entity.Property(e => e.GeneratedAtUtc).IsRequired();
            
            // Index for tenant+branch+period queries
            entity.HasIndex(e => new { e.TenantId, e.BranchId, e.PeriodStartUtc, e.PeriodEndUtc })
                .HasDatabaseName("IX_Analytics_TopServiceMetrics_Tenant_Branch_Period");
            
            // Index for ranking queries
            entity.HasIndex(e => new { e.TenantId, e.BranchId, e.Ranking })
                .HasDatabaseName("IX_Analytics_TopServiceMetrics_Tenant_Branch_Ranking");
        });
    }

    private static void ConfigureResourceCapacityMetrics(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ResourceCapacityMetrics>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.ResourceId).IsRequired();
            entity.Property(e => e.ResourceName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ResourceType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.StaffName).HasMaxLength(200);
            entity.Property(e => e.GeneratedAtUtc).IsRequired();
            
            entity.Property(e => e.CapacityUtilizationRate).HasPrecision(5, 2);
            entity.Property(e => e.TotalBookedMinutes).HasPrecision(12, 2);
            entity.Property(e => e.TotalAvailableMinutes).HasPrecision(12, 2);
            entity.Property(e => e.TimeUtilizationRate).HasPrecision(5, 2);
            
            // Index for tenant+branch+period queries
            entity.HasIndex(e => new { e.TenantId, e.BranchId, e.PeriodStartUtc, e.PeriodEndUtc })
                .HasDatabaseName("IX_Analytics_ResourceCapacity_Tenant_Branch_Period");
            
            // Index for resource queries
            entity.HasIndex(e => new { e.TenantId, e.BranchId, e.ResourceId })
                .HasDatabaseName("IX_Analytics_ResourceCapacity_Tenant_Branch_Resource");
        });
    }
}