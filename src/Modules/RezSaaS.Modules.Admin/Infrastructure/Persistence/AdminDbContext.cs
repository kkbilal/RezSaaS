using Microsoft.EntityFrameworkCore;
using RezSaaS.Modules.Admin.Domain;

namespace RezSaaS.Modules.Admin.Infrastructure.Persistence;

public sealed class AdminDbContext : DbContext
{
    public const string ConnectionStringName = "AdminDatabase";

    public AdminDbContext(DbContextOptions<AdminDbContext> options)
        : base(options)
    {
    }

    public DbSet<AbuseEvent> AbuseEvents => Set<AbuseEvent>();

    public DbSet<AbuseAppeal> AbuseAppeals => Set<AbuseAppeal>();

    public DbSet<AccountClosureCase> AccountClosureCases => Set<AccountClosureCase>();

    public DbSet<AdminAuditLogEntry> AdminAuditLogEntries => Set<AdminAuditLogEntry>();

    public DbSet<BusinessAbuseReport> BusinessAbuseReports => Set<BusinessAbuseReport>();

    public DbSet<UserSanction> UserSanctions => Set<UserSanction>();

    public DbSet<UserStrike> UserStrikes => Set<UserStrike>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("admin");

        modelBuilder.Entity<AbuseEvent>(abuseEvent =>
        {
            abuseEvent.ToTable("AbuseEvents");
            abuseEvent.HasKey(entity => entity.Id);
            abuseEvent.Property(entity => entity.EventType).HasMaxLength(120).IsRequired();
            abuseEvent.Property(entity => entity.Severity).HasConversion<string>().HasMaxLength(32).IsRequired();
            abuseEvent.Property(entity => entity.DetailsJson).HasColumnType("jsonb").IsRequired();
            abuseEvent.HasIndex(entity => new { entity.UserAccountId, entity.OccurredAtUtc });
            abuseEvent.HasIndex(entity => new { entity.TenantId, entity.OccurredAtUtc });
        });

        modelBuilder.Entity<AbuseAppeal>(appeal =>
        {
            appeal.ToTable(
                "AbuseAppeals",
                table => table.HasCheckConstraint(
                    "CK_AbuseAppeals_ReviewShape",
                    """
                    ("Status" = 'PendingReview'
                        AND "ReviewedAtUtc" IS NULL
                        AND "ReviewedByUserAccountId" IS NULL
                        AND "ReviewReason" IS NULL)
                    OR
                    ("Status" IN ('Accepted', 'Rejected')
                        AND "ReviewedAtUtc" IS NOT NULL
                        AND "ReviewedAtUtc" >= "CreatedAtUtc"
                        AND "ReviewedByUserAccountId" IS NOT NULL
                        AND "ReviewReason" IS NOT NULL)
                    """));
            appeal.HasKey(entity => entity.Id);
            appeal.Property(entity => entity.TargetType)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            appeal.Property(entity => entity.Statement).HasMaxLength(1000).IsRequired();
            appeal.Property(entity => entity.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            appeal.Property(entity => entity.ReviewReason).HasMaxLength(500);
            appeal.HasIndex(entity => new { entity.UserAccountId, entity.TargetType, entity.TargetId })
                .IsUnique();
            appeal.HasIndex(entity => new { entity.UserAccountId, entity.Status, entity.CreatedAtUtc });
        });

        modelBuilder.Entity<AccountClosureCase>(closureCase =>
        {
            closureCase.ToTable(
                "AccountClosureCases",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_AccountClosureCases_NoSelfProposal",
                        "\"UserAccountId\" <> \"ProposedByUserAccountId\"");
                    table.HasCheckConstraint(
                        "CK_AccountClosureCases_NoticeDeliveryShape",
                        """
                        ("CustomerNoticeDeliveredAtUtc" IS NULL
                            AND "EligibleForExecutionAtUtc" IS NULL)
                        OR
                        ("CustomerNoticeDeliveredAtUtc" IS NOT NULL
                            AND "CustomerNoticeDeliveredAtUtc" >= "ProposedAtUtc"
                            AND "EligibleForExecutionAtUtc" > "CustomerNoticeDeliveredAtUtc")
                        """);
                    table.HasCheckConstraint(
                        "CK_AccountClosureCases_DecisionAfterProposal",
                        "\"DecidedAtUtc\" IS NULL OR \"DecidedAtUtc\" >= \"ProposedAtUtc\"");
                    table.HasCheckConstraint(
                        "CK_AccountClosureCases_ExecutionAfterEligibility",
                        """
                        "ExecutionStartedAtUtc" IS NULL
                        OR
                        ("EligibleForExecutionAtUtc" IS NOT NULL
                            AND "ExecutionStartedAtUtc" >= "EligibleForExecutionAtUtc")
                        """);
                    table.HasCheckConstraint(
                        "CK_AccountClosureCases_CompletionAfterExecutionStart",
                        "\"ExecutedAtUtc\" IS NULL OR \"ExecutedAtUtc\" >= \"ExecutionStartedAtUtc\"");
                    table.HasCheckConstraint(
                        "CK_AccountClosureCases_DecisionShape",
                        """
                        ("Status" = 'PendingApproval'
                            AND "ReviewedByUserAccountId" IS NULL
                            AND "DecisionReason" IS NULL
                            AND "DecidedAtUtc" IS NULL)
                        OR
                        ("Status" IN ('Approved', 'Rejected', 'CancelledByAppeal', 'Executing', 'Executed')
                            AND "ReviewedByUserAccountId" IS NOT NULL
                            AND "DecisionReason" IS NOT NULL
                            AND "DecidedAtUtc" IS NOT NULL)
                        """);
                    table.HasCheckConstraint(
                        "CK_AccountClosureCases_ExecutionShape",
                        """
                        ("Status" NOT IN ('Executing', 'Executed')
                            AND "ExecutionStartedByUserAccountId" IS NULL
                            AND "ExecutionStartedAtUtc" IS NULL
                            AND "ExecutedByUserAccountId" IS NULL
                            AND "ExecutedAtUtc" IS NULL)
                        OR
                        ("Status" = 'Executing'
                            AND "ExecutionStartedByUserAccountId" IS NOT NULL
                            AND "ExecutionStartedAtUtc" IS NOT NULL
                            AND "ExecutedByUserAccountId" IS NULL
                            AND "ExecutedAtUtc" IS NULL)
                        OR
                        ("Status" = 'Executed'
                            AND "ExecutionStartedByUserAccountId" IS NOT NULL
                            AND "ExecutionStartedAtUtc" IS NOT NULL
                            AND "ExecutedByUserAccountId" IS NOT NULL
                            AND "ExecutedAtUtc" IS NOT NULL)
                        """);
                });
            closureCase.HasKey(entity => entity.Id);
            closureCase.Property(entity => entity.InternalReason).HasMaxLength(500).IsRequired();
            closureCase.Property(entity => entity.CustomerNotice).HasMaxLength(500).IsRequired();
            closureCase.Property(entity => entity.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            closureCase.Property(entity => entity.DecisionReason).HasMaxLength(500);
            closureCase.HasIndex(entity => entity.UserAccountId)
                .IsUnique()
                .HasFilter("\"Status\" IN ('PendingApproval', 'Approved', 'Executing')");
            closureCase.HasIndex(entity => new { entity.UserAccountId, entity.Status, entity.ProposedAtUtc });
            closureCase.HasIndex(entity => new { entity.Status, entity.EligibleForExecutionAtUtc });
        });

        modelBuilder.Entity<UserSanction>(sanction =>
        {
            sanction.ToTable("UserSanctions");
            sanction.HasKey(entity => entity.Id);
            sanction.Property(entity => entity.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
            sanction.Property(entity => entity.Reason).HasMaxLength(300).IsRequired();
            sanction.Property(entity => entity.RevocationReason).HasMaxLength(300);
            sanction.HasIndex(entity => new { entity.UserAccountId, entity.StartsAtUtc });
        });

        modelBuilder.Entity<BusinessAbuseReport>(report =>
        {
            report.ToTable(
                "BusinessAbuseReports",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_BusinessAbuseReports_ReviewShape",
                        """
                        ("Status" = 'PendingReview'
                            AND "ReviewedAtUtc" IS NULL
                            AND "ReviewedByUserAccountId" IS NULL
                            AND "ReviewReason" IS NULL)
                        OR
                        ("Status" IN ('Confirmed', 'Dismissed')
                            AND "ReviewedAtUtc" IS NOT NULL
                            AND "ReviewedAtUtc" >= "CreatedAtUtc"
                            AND "ReviewedByUserAccountId" IS NOT NULL
                            AND "ReviewReason" IS NOT NULL)
                        """);
                    table.HasCheckConstraint(
                        "CK_BusinessAbuseReports_NoSelfReport",
                        "\"ReportedUserAccountId\" <> \"ReportedByUserAccountId\"");
                });
            report.HasKey(entity => entity.Id);
            report.Property(entity => entity.ReasonCode).HasConversion<string>().HasMaxLength(48).IsRequired();
            report.Property(entity => entity.Note).HasMaxLength(300);
            report.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            report.Property(entity => entity.ReviewReason).HasMaxLength(300);
            report.HasIndex(entity => new { entity.TenantId, entity.AppointmentRequestId }).IsUnique();
            report.HasIndex(entity => new { entity.ReportedUserAccountId, entity.CreatedAtUtc });
            report.HasIndex(entity => new { entity.TenantId, entity.Status, entity.CreatedAtUtc });
            report.HasIndex(entity => new { entity.TenantId, entity.ReportedByUserAccountId, entity.CreatedAtUtc });
        });

        modelBuilder.Entity<UserStrike>(strike =>
        {
            strike.ToTable(
                "UserStrikes",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_UserStrikes_ExpiryAfterIssue",
                        "\"ExpiresAtUtc\" > \"IssuedAtUtc\"");
                    table.HasCheckConstraint(
                        "CK_UserStrikes_RevocationShape",
                        """
                        ("RevokedAtUtc" IS NULL
                            AND "RevokedByUserAccountId" IS NULL
                            AND "RevocationReason" IS NULL)
                        OR
                        ("RevokedAtUtc" IS NOT NULL
                            AND "RevokedByUserAccountId" IS NOT NULL
                            AND "RevocationReason" IS NOT NULL)
                        """);
                    table.HasCheckConstraint(
                        "CK_UserStrikes_RevocationAfterIssue",
                        "\"RevokedAtUtc\" IS NULL OR \"RevokedAtUtc\" >= \"IssuedAtUtc\"");
                });
            strike.HasKey(entity => entity.Id);
            strike.Property(entity => entity.ReasonCode).HasConversion<string>().HasMaxLength(48).IsRequired();
            strike.Property(entity => entity.RevocationReason).HasMaxLength(300);
            strike.HasOne<BusinessAbuseReport>()
                .WithMany()
                .HasForeignKey(entity => entity.SourceAbuseReportId)
                .OnDelete(DeleteBehavior.Restrict);
            strike.HasIndex(entity => entity.SourceAbuseReportId).IsUnique();
            strike.HasIndex(entity => new { entity.UserAccountId, entity.IssuedAtUtc });
            strike.HasIndex(entity => new { entity.UserAccountId, entity.ExpiresAtUtc });
        });

        modelBuilder.Entity<AdminAuditLogEntry>(audit =>
        {
            audit.ToTable("AdminAuditLogEntries");
            audit.HasKey(entity => entity.Id);
            audit.Property(entity => entity.Action).HasMaxLength(128).IsRequired();
            audit.Property(entity => entity.DetailsJson).HasColumnType("jsonb").IsRequired();
            audit.HasIndex(entity => new { entity.ActorUserAccountId, entity.OccurredAtUtc });
        });
    }
}
