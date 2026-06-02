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

    public DbSet<AdminAuditLogEntry> AdminAuditLogEntries => Set<AdminAuditLogEntry>();

    public DbSet<UserSanction> UserSanctions => Set<UserSanction>();

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

        modelBuilder.Entity<UserSanction>(sanction =>
        {
            sanction.ToTable("UserSanctions");
            sanction.HasKey(entity => entity.Id);
            sanction.Property(entity => entity.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
            sanction.Property(entity => entity.Reason).HasMaxLength(300).IsRequired();
            sanction.HasIndex(entity => new { entity.UserAccountId, entity.StartsAtUtc });
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
