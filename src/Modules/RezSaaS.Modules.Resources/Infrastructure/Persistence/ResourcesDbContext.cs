using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Resources.Domain;

namespace RezSaaS.Modules.Resources.Infrastructure.Persistence;

public sealed class ResourcesDbContext : DbContext
{
    private readonly ITenantContextAccessor? tenantContextAccessor;

    public const string ConnectionStringName = "ResourcesDatabase";

    public ResourcesDbContext(
        DbContextOptions<ResourcesDbContext> options,
        ITenantContextAccessor? tenantContextAccessor = null)
        : base(options)
    {
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public DbSet<ResourceBlock> ResourceBlocks => Set<ResourceBlock>();

    public DbSet<Resource> Resources => Set<Resource>();

    public DbSet<ResourceType> ResourceTypes => Set<ResourceType>();

    private Guid? CurrentTenantId => tenantContextAccessor?.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("resources");

        modelBuilder.Entity<ResourceType>(resourceType =>
        {
            resourceType.ToTable("ResourceTypes");
            resourceType.HasKey(entity => entity.Id);
            resourceType.Property(entity => entity.Key).HasMaxLength(80).IsRequired();
            resourceType.Property(entity => entity.NormalizedKey).HasMaxLength(80).IsRequired();
            resourceType.Property(entity => entity.DisplayName).HasMaxLength(160).IsRequired();
            resourceType.HasIndex(entity => new { entity.TenantId, entity.NormalizedKey }).IsUnique();
            resourceType.HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
        });

        modelBuilder.Entity<Resource>(resource =>
        {
            resource.ToTable("Resources");
            resource.HasKey(entity => entity.Id);
            resource.Property(entity => entity.DisplayName).HasMaxLength(160).IsRequired();
            resource.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            resource.HasIndex(entity => new { entity.TenantId, entity.BranchId });
            resource.HasIndex(entity => new { entity.TenantId, entity.ResourceTypeId });
            resource.HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
        });

        modelBuilder.Entity<ResourceBlock>(block =>
        {
            block.ToTable(
                "ResourceBlocks",
                table => table.HasCheckConstraint(
                    "CK_ResourceBlocks_EndAfterStart",
                    "\"EndUtc\" > \"StartUtc\""));
            block.HasKey(entity => entity.Id);
            block.Property(entity => entity.Reason).HasMaxLength(200).IsRequired();
            block.HasIndex(entity => new { entity.TenantId, entity.ResourceId, entity.StartUtc });
            block.HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
        });
    }
}
