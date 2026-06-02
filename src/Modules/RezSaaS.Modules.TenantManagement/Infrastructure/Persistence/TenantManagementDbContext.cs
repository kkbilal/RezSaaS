using Microsoft.EntityFrameworkCore;
using RezSaaS.Modules.TenantManagement.Domain;

namespace RezSaaS.Modules.TenantManagement.Infrastructure.Persistence;

public sealed class TenantManagementDbContext : DbContext
{
    public const string ConnectionStringName = "TenantManagementDatabase";

    public TenantManagementDbContext(DbContextOptions<TenantManagementDbContext> options)
        : base(options)
    {
    }

    public DbSet<TenantAuditLogEntry> AuditLogEntries => Set<TenantAuditLogEntry>();

    public DbSet<TenantMembership> Memberships => Set<TenantMembership>();

    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("tenant_management");

        modelBuilder.Entity<Tenant>(tenant =>
        {
            tenant.ToTable("Tenants");
            tenant.HasKey(entity => entity.Id);
            tenant.Property(entity => entity.Slug)
                .HasMaxLength(64)
                .IsRequired();
            tenant.Property(entity => entity.NormalizedSlug)
                .HasMaxLength(64)
                .IsRequired();
            tenant.Property(entity => entity.DisplayName)
                .HasMaxLength(200)
                .IsRequired();
            tenant.Property(entity => entity.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            tenant.HasIndex(entity => entity.NormalizedSlug)
                .IsUnique();
            tenant.HasMany(entity => entity.Memberships)
                .WithOne(entity => entity.Tenant)
                .HasForeignKey(entity => entity.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TenantMembership>(membership =>
        {
            membership.ToTable(
                "TenantMemberships",
                table => table.HasCheckConstraint(
                    "CK_TenantMemberships_BusinessOwner_NotBranchScoped",
                    "\"Role\" <> 'BusinessOwner' OR \"BranchId\" IS NULL"));
            membership.HasKey(entity => entity.Id);
            membership.Property(entity => entity.Role)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            membership.Property(entity => entity.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            membership.HasIndex(entity => new { entity.TenantId, entity.UserAccountId })
                .IsUnique();
            membership.HasIndex(entity => new { entity.TenantId, entity.Role });
        });

        modelBuilder.Entity<TenantAuditLogEntry>(audit =>
        {
            audit.ToTable("TenantAuditLogEntries");
            audit.HasKey(entity => entity.Id);
            audit.Property(entity => entity.Action)
                .HasMaxLength(128)
                .IsRequired();
            audit.Property(entity => entity.DetailsJson)
                .HasColumnType("jsonb")
                .IsRequired();
            audit.HasIndex(entity => new { entity.TenantId, entity.OccurredAtUtc });
        });
    }
}
