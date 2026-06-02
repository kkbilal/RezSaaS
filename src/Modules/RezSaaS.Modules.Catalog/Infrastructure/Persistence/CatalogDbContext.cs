using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Catalog.Domain;

namespace RezSaaS.Modules.Catalog.Infrastructure.Persistence;

public sealed class CatalogDbContext : DbContext
{
    private readonly ITenantContextAccessor? tenantContextAccessor;

    public const string ConnectionStringName = "CatalogDatabase";

    public CatalogDbContext(
        DbContextOptions<CatalogDbContext> options,
        ITenantContextAccessor? tenantContextAccessor = null)
        : base(options)
    {
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public DbSet<ServiceRequiredSkill> ServiceRequiredSkills => Set<ServiceRequiredSkill>();

    public DbSet<Service> Services => Set<Service>();

    public DbSet<ServiceVariant> ServiceVariants => Set<ServiceVariant>();

    private Guid? CurrentTenantId => tenantContextAccessor?.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("catalog");

        modelBuilder.Entity<Service>(service =>
        {
            service.ToTable("Services");
            service.HasKey(entity => entity.Id);
            service.Property(entity => entity.Name).HasMaxLength(160).IsRequired();
            service.Property(entity => entity.NormalizedName).HasMaxLength(160).IsRequired();
            service.Property(entity => entity.CategoryKey).HasMaxLength(80).IsRequired();
            service.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            service.HasIndex(entity => new { entity.TenantId, entity.NormalizedName }).IsUnique();
            service.HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
        });

        modelBuilder.Entity<ServiceVariant>(variant =>
        {
            variant.ToTable(
                "ServiceVariants",
                table => table.HasCheckConstraint(
                    "CK_ServiceVariants_PositiveDuration",
                    "\"DurationMinutes\" > 0"));
            variant.HasKey(entity => entity.Id);
            variant.Property(entity => entity.Name).HasMaxLength(160).IsRequired();
            variant.Property(entity => entity.NormalizedName).HasMaxLength(160).IsRequired();
            variant.Property(entity => entity.CurrencyCode).HasMaxLength(3).IsRequired();
            variant.Property(entity => entity.PriceAmount).HasPrecision(12, 2);
            variant.HasIndex(entity => new { entity.TenantId, entity.ServiceId, entity.NormalizedName }).IsUnique();
            variant.HasOne(entity => entity.Service)
                .WithMany()
                .HasForeignKey(entity => entity.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
            variant.HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
        });

        modelBuilder.Entity<ServiceRequiredSkill>(requiredSkill =>
        {
            requiredSkill.ToTable("ServiceRequiredSkills");
            requiredSkill.HasKey(entity => entity.Id);
            requiredSkill.HasIndex(entity => new { entity.TenantId, entity.ServiceVariantId, entity.SkillId })
                .IsUnique();
            requiredSkill.HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
        });
    }
}
