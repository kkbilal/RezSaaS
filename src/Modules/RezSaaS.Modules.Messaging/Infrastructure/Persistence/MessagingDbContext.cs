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

    public DbSet<PlatformTransactionalMessage> PlatformTransactionalMessages =>
        Set<PlatformTransactionalMessage>();

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

        modelBuilder.Entity<PlatformTransactionalMessage>(message =>
        {
            message.ToTable(
                "PlatformTransactionalMessages",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_PlatformTransactionalMessages_AttemptCount",
                        "\"AttemptCount\" >= 0");
                    table.HasCheckConstraint(
                        "CK_PlatformTransactionalMessages_DeliveryAfterCreation",
                        "\"SentAtUtc\" IS NULL OR \"SentAtUtc\" >= \"CreatedAtUtc\"");
                    table.HasCheckConstraint(
                        "CK_PlatformTransactionalMessages_CompletionAfterCreation",
                        "\"CompletedAtUtc\" IS NULL OR \"CompletedAtUtc\" >= \"CreatedAtUtc\"");
                    table.HasCheckConstraint(
                        "CK_PlatformTransactionalMessages_AttemptAfterCreation",
                        "\"LastAttemptAtUtc\" IS NULL OR \"LastAttemptAtUtc\" >= \"CreatedAtUtc\"");
                    table.HasCheckConstraint(
                        "CK_PlatformTransactionalMessages_LockAfterAttempt",
                        "\"LockedUntilUtc\" IS NULL OR (\"LastAttemptAtUtc\" IS NOT NULL "
                            + "AND \"LockedUntilUtc\" > \"LastAttemptAtUtc\")");
                    table.HasCheckConstraint(
                        "CK_PlatformTransactionalMessages_NextAttemptAfterCreation",
                        "\"NextAttemptAtUtc\" IS NULL OR \"NextAttemptAtUtc\" >= \"CreatedAtUtc\"");
                    table.HasCheckConstraint(
                        "CK_PlatformTransactionalMessages_SentShape",
                        "\"Status\" <> 'Sent' OR \"SentAtUtc\" IS NOT NULL");
                    table.HasCheckConstraint(
                        "CK_PlatformTransactionalMessages_StateShape",
                        """
                        ("Status" = 'Pending'
                            AND "NextAttemptAtUtc" IS NOT NULL
                            AND "LockedUntilUtc" IS NULL
                            AND "CompletedAtUtc" IS NULL)
                        OR
                        ("Status" = 'Processing'
                            AND "NextAttemptAtUtc" IS NULL
                            AND "LockedUntilUtc" IS NOT NULL
                            AND "CompletedAtUtc" IS NULL)
                        OR
                        ("Status" IN ('Sent', 'Failed', 'Cancelled')
                            AND "NextAttemptAtUtc" IS NULL
                            AND "LockedUntilUtc" IS NULL
                            AND "CompletedAtUtc" IS NOT NULL)
                        """);
                });
            message.HasKey(entity => entity.Id);
            message.Property(entity => entity.Purpose)
                .HasConversion<string>()
                .HasMaxLength(48)
                .IsRequired();
            message.Property(entity => entity.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            message.Property(entity => entity.DeliveryKey).HasMaxLength(180).IsRequired();
            message.Property(entity => entity.Subject).HasMaxLength(200).IsRequired();
            message.Property(entity => entity.Body).HasMaxLength(4000).IsRequired();
            message.Property(entity => entity.LastErrorCode).HasMaxLength(120);
            message.HasIndex(entity => entity.DeliveryKey).IsUnique();
            message.HasIndex(entity => new { entity.Status, entity.NextAttemptAtUtc, entity.CreatedAtUtc });
            message.HasIndex(entity => new { entity.Purpose, entity.CorrelationId });
        });
    }
}
