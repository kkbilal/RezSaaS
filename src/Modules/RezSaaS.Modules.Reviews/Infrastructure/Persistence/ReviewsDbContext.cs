using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Reviews.Domain;

namespace RezSaaS.Modules.Reviews.Infrastructure.Persistence;

public sealed class ReviewsDbContext : DbContext
{
    private readonly ITenantContextAccessor? tenantContextAccessor;

    public const string ConnectionStringName = "ReviewsDatabase";

    public ReviewsDbContext(
        DbContextOptions<ReviewsDbContext> options,
        ITenantContextAccessor? tenantContextAccessor = null)
        : base(options)
    {
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public DbSet<Review> Reviews => Set<Review>();

    private Guid? CurrentTenantId => tenantContextAccessor?.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("reviews");

        modelBuilder.Entity<Review>(review =>
        {
            review.ToTable(
                "Reviews",
                table => table.HasCheckConstraint(
                    "CK_Reviews_RatingRange",
                    "\"Rating\" BETWEEN 1 AND 5"));

            review.HasKey(entity => entity.Id);

            review.Property(entity => entity.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            review.Property(entity => entity.Comment).HasMaxLength(1_000).IsRequired();
            review.Property(entity => entity.ModerationNote).HasMaxLength(500);

            // Unique constraint: one review per appointment per tenant.
            review.HasIndex(entity => new { entity.TenantId, entity.AppointmentId })
                .IsUnique();

            // Lookup indexes.
            review.HasIndex(entity => new { entity.TenantId, entity.BusinessId, entity.Status });
            review.HasIndex(entity => new { entity.TenantId, entity.CustomerUserAccountId });

            review.HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
        });
    }
}