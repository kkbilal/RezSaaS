using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Messaging.Domain;

namespace RezSaaS.Modules.Messaging.Infrastructure.Persistence;

public sealed class MessagingDbContext : DbContext
{
    private readonly ITenantContextAccessor? tenantContextAccessor;

    public const string ConnectionStringName = "MessagingDatabase";

    public MessagingDbContext(
        DbContextOptions<MessagingDbContext> options,
        ITenantContextAccessor? tenantContextAccessor = null)
        : base(options)
    {
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public DbSet<TransactionalMessage> TransactionalMessages => Set<TransactionalMessage>();

    private Guid? CurrentTenantId => tenantContextAccessor?.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("messaging");

        modelBuilder.Entity<TransactionalMessage>(message =>
        {
            message.ToTable("TransactionalMessages");
            message.HasKey(entity => entity.Id);
            message.Property(entity => entity.Channel).HasConversion<string>().HasMaxLength(32).IsRequired();
            message.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            message.Property(entity => entity.RecipientMasked).HasMaxLength(160).IsRequired();
            message.Property(entity => entity.TemplateKey).HasMaxLength(120).IsRequired();
            message.Property(entity => entity.PayloadJson).HasColumnType("jsonb").IsRequired();
            message.Property(entity => entity.ProviderMessageId).HasMaxLength(160);
            message.HasIndex(entity => new { entity.TenantId, entity.Status, entity.CreatedAtUtc });
            message.HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
        });
    }
}
