using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Payments.Domain;

namespace RezSaaS.Modules.Payments.Infrastructure.Persistence;

public sealed class PaymentsDbContext : DbContext
{
    private readonly ITenantContextAccessor? tenantContextAccessor;

    public const string ConnectionStringName = "PaymentsDatabase";

    public PaymentsDbContext(
        DbContextOptions<PaymentsDbContext> options,
        ITenantContextAccessor? tenantContextAccessor = null)
        : base(options)
    {
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public DbSet<PaymentAuditLogEntry> AuditLogEntries => Set<PaymentAuditLogEntry>();

    public DbSet<PaymentIntent> PaymentIntents => Set<PaymentIntent>();

    public DbSet<PaymentPolicy> PaymentPolicies => Set<PaymentPolicy>();

    public DbSet<PaymentWebhookEvent> WebhookEvents => Set<PaymentWebhookEvent>();

    private Guid? CurrentTenantId => tenantContextAccessor?.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("payments");

        modelBuilder.Entity<PaymentPolicy>(policy =>
        {
            policy.ToTable(
                "PaymentPolicies",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_PaymentPolicies_FixedAmount",
                        "\"FixedAmount\" IS NULL OR \"FixedAmount\" >= 0");
                    table.HasCheckConstraint(
                        "CK_PaymentPolicies_Percentage",
                        "\"Percentage\" IS NULL OR (\"Percentage\" > 0 AND \"Percentage\" <= 100)");
                    table.HasCheckConstraint(
                        "CK_PaymentPolicies_CurrencyCode",
                        "length(\"CurrencyCode\") = 3");
                    table.HasCheckConstraint(
                        "CK_PaymentPolicies_HostedCheckoutShape",
                        "\"HostedCheckoutEnabled\" = FALSE OR length(\"ProviderKey\") > 0");
                });
            policy.HasKey(entity => entity.Id);
            policy.Property(entity => entity.Mode)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            policy.Property(entity => entity.CurrencyCode)
                .HasMaxLength(3)
                .IsRequired();
            policy.Property(entity => entity.ProviderKey)
                .HasMaxLength(80)
                .IsRequired();
            policy.Property(entity => entity.FixedAmount)
                .HasPrecision(12, 2);
            policy.Property(entity => entity.Percentage)
                .HasPrecision(5, 2);
            policy.HasIndex(entity => entity.TenantId)
                .HasDatabaseName("IX_PaymentPolicies_TenantId_TenantWide")
                .IsUnique()
                .HasFilter("\"BranchId\" IS NULL");
            policy.HasIndex(entity => new { entity.TenantId, entity.BranchId })
                .HasDatabaseName("IX_PaymentPolicies_TenantId_BranchId_BranchScoped")
                .IsUnique()
                .HasFilter("\"BranchId\" IS NOT NULL");
            policy.HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
        });

        modelBuilder.Entity<PaymentIntent>(intent =>
        {
            intent.ToTable(
                "PaymentIntents",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_PaymentIntents_Amount",
                        "\"Amount\" > 0");
                    table.HasCheckConstraint(
                        "CK_PaymentIntents_CurrencyCode",
                        "length(\"CurrencyCode\") = 3");
                    table.HasCheckConstraint(
                        "CK_PaymentIntents_Target",
                        "\"AppointmentRequestId\" IS NOT NULL OR \"AppointmentId\" IS NOT NULL");
                });
            intent.HasKey(entity => entity.Id);
            intent.Property(entity => entity.Purpose)
                .HasConversion<string>()
                .HasMaxLength(48)
                .IsRequired();
            intent.Property(entity => entity.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            intent.Property(entity => entity.Amount)
                .HasPrecision(12, 2);
            intent.Property(entity => entity.CurrencyCode)
                .HasMaxLength(3)
                .IsRequired();
            intent.Property(entity => entity.ProviderKey)
                .HasMaxLength(80)
                .IsRequired();
            intent.Property(entity => entity.ProviderReference)
                .HasMaxLength(180)
                .IsRequired();
            intent.Property(entity => entity.ProviderCheckoutUrl)
                .HasMaxLength(1_000)
                .IsRequired();
            intent.HasIndex(entity => new { entity.TenantId, entity.CustomerUserAccountId, entity.Status });
            intent.HasIndex(entity => new { entity.TenantId, entity.AppointmentRequestId });
            intent.HasIndex(entity => new { entity.ProviderKey, entity.ProviderReference });
            intent.HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
        });

        modelBuilder.Entity<PaymentWebhookEvent>(webhookEvent =>
        {
            webhookEvent.ToTable("PaymentWebhookEvents");
            webhookEvent.HasKey(entity => entity.Id);
            webhookEvent.Property(entity => entity.ProviderKey)
                .HasMaxLength(80)
                .IsRequired();
            webhookEvent.Property(entity => entity.ProviderEventId)
                .HasMaxLength(180)
                .IsRequired();
            webhookEvent.Property(entity => entity.EventType)
                .HasMaxLength(120)
                .IsRequired();
            webhookEvent.Property(entity => entity.PayloadSha256)
                .HasMaxLength(64)
                .IsRequired();
            webhookEvent.Property(entity => entity.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            webhookEvent.Property(entity => entity.LastErrorCode)
                .HasMaxLength(120)
                .IsRequired();
            webhookEvent.HasIndex(entity => new { entity.ProviderKey, entity.ProviderEventId })
                .IsUnique();
            webhookEvent.HasIndex(entity => new { entity.Status, entity.ReceivedAtUtc });
        });

        modelBuilder.Entity<PaymentAuditLogEntry>(audit =>
        {
            audit.ToTable("PaymentAuditLogEntries");
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
