using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Integrations.Domain;

namespace RezSaaS.Modules.Integrations.Infrastructure.Persistence;

public sealed class IntegrationsDbContext : DbContext
{
    private readonly ITenantContextAccessor? tenantContextAccessor;

    public const string ConnectionStringName = "IntegrationsDatabase";

    public IntegrationsDbContext(
        DbContextOptions<IntegrationsDbContext> options,
        ITenantContextAccessor? tenantContextAccessor = null)
        : base(options)
    {
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public DbSet<IntegrationApiClient> ApiClients => Set<IntegrationApiClient>();

    public DbSet<IntegrationAuditLogEntry> AuditLogEntries => Set<IntegrationAuditLogEntry>();

    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();

    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();

    private Guid? CurrentTenantId => tenantContextAccessor?.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("integrations");

        modelBuilder.Entity<IntegrationApiClient>(client =>
        {
            client.ToTable(
                "IntegrationApiClients",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_IntegrationApiClients_KeyHashSha256",
                        "length(\"KeyHashSha256\") = 64");
                    table.HasCheckConstraint(
                        "CK_IntegrationApiClients_RevocationShape",
                        """
                        ("Status" = 'Active'
                            AND "RevokedByUserAccountId" IS NULL
                            AND "RevokedAtUtc" IS NULL
                            AND length("RevocationReason") = 0)
                        OR
                        ("Status" = 'Revoked'
                            AND "RevokedByUserAccountId" IS NOT NULL
                            AND "RevokedAtUtc" IS NOT NULL
                            AND length("RevocationReason") > 0)
                        """);
                });
            client.HasKey(entity => entity.Id);
            client.Property(entity => entity.DisplayName)
                .HasMaxLength(120)
                .IsRequired();
            client.Property(entity => entity.KeyPrefix)
                .HasMaxLength(32)
                .IsRequired();
            client.Property(entity => entity.KeyHashSha256)
                .HasMaxLength(64)
                .IsRequired();
            client.Property(entity => entity.ScopeSet)
                .HasMaxLength(500)
                .IsRequired();
            client.Property(entity => entity.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            client.Property(entity => entity.RevocationReason)
                .HasMaxLength(500)
                .IsRequired();
            client.HasIndex(entity => entity.KeyHashSha256)
                .IsUnique();
            client.HasIndex(entity => new { entity.TenantId, entity.KeyPrefix })
                .IsUnique();
            client.HasIndex(entity => new { entity.TenantId, entity.Status, entity.CreatedAtUtc });
            client.HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
        });

        modelBuilder.Entity<WebhookSubscription>(subscription =>
        {
            subscription.ToTable(
                "WebhookSubscriptions",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_WebhookSubscriptions_SigningSecretHashSha256",
                        "length(\"SigningSecretHashSha256\") = 64");
                    table.HasCheckConstraint(
                        "CK_WebhookSubscriptions_TargetHttps",
                        "\"TargetUrl\" LIKE 'https://%'");
                    table.HasCheckConstraint(
                        "CK_WebhookSubscriptions_RevocationShape",
                        """
                        ("Status" IN ('Active', 'Paused')
                            AND "RevokedByUserAccountId" IS NULL
                            AND "RevokedAtUtc" IS NULL
                            AND length("RevocationReason") = 0)
                        OR
                        ("Status" = 'Revoked'
                            AND "RevokedByUserAccountId" IS NOT NULL
                            AND "RevokedAtUtc" IS NOT NULL
                            AND length("RevocationReason") > 0)
                        """);
                });
            subscription.HasKey(entity => entity.Id);
            subscription.Property(entity => entity.DisplayName)
                .HasMaxLength(120)
                .IsRequired();
            subscription.Property(entity => entity.TargetUrl)
                .HasMaxLength(1_000)
                .IsRequired();
            subscription.Property(entity => entity.EventTypes)
                .HasMaxLength(500)
                .IsRequired();
            subscription.Property(entity => entity.SigningSecretHashSha256)
                .HasMaxLength(64)
                .IsRequired();
            subscription.Property(entity => entity.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            subscription.Property(entity => entity.RevocationReason)
                .HasMaxLength(500)
                .IsRequired();
            subscription.HasIndex(entity => new { entity.TenantId, entity.Status, entity.CreatedAtUtc });
            subscription.HasIndex(entity => new { entity.TenantId, entity.TargetUrl });
            subscription.HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
        });

        modelBuilder.Entity<WebhookDelivery>(delivery =>
        {
            delivery.ToTable(
                "WebhookDeliveries",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_WebhookDeliveries_AttemptCount",
                        "\"AttemptCount\" >= 0");
                    table.HasCheckConstraint(
                        "CK_WebhookDeliveries_PayloadSha256",
                        "length(\"PayloadSha256\") = 64");
                    table.HasCheckConstraint(
                        "CK_WebhookDeliveries_DeliveredAfterCreation",
                        "\"DeliveredAtUtc\" IS NULL OR \"DeliveredAtUtc\" >= \"CreatedAtUtc\"");
                    table.HasCheckConstraint(
                        "CK_WebhookDeliveries_LockShape",
                        "\"LockedUntilUtc\" IS NULL OR (\"LastAttemptAtUtc\" IS NOT NULL "
                            + "AND \"LockedUntilUtc\" > \"LastAttemptAtUtc\")");
                });
            delivery.HasKey(entity => entity.Id);
            delivery.Property(entity => entity.EventType)
                .HasMaxLength(120)
                .IsRequired();
            delivery.Property(entity => entity.PayloadSha256)
                .HasMaxLength(64)
                .IsRequired();
            delivery.Property(entity => entity.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            delivery.Property(entity => entity.LastErrorCode)
                .HasMaxLength(120)
                .IsRequired();
            delivery.HasIndex(entity => new { entity.TenantId, entity.SubscriptionId, entity.Status });
            delivery.HasIndex(entity => new { entity.Status, entity.CreatedAtUtc });
            delivery.HasIndex(entity => new { entity.TenantId, entity.CorrelationId, entity.EventType });
            delivery.HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
        });

        modelBuilder.Entity<IntegrationAuditLogEntry>(audit =>
        {
            audit.ToTable("IntegrationAuditLogEntries");
            audit.HasKey(entity => entity.Id);
            audit.Property(entity => entity.Action)
                .HasMaxLength(128)
                .IsRequired();
            audit.Property(entity => entity.DetailsJson)
                .HasColumnType("jsonb")
                .IsRequired();
            audit.HasIndex(entity => new { entity.TenantId, entity.OccurredAtUtc });
            audit.HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
        });
    }
}
