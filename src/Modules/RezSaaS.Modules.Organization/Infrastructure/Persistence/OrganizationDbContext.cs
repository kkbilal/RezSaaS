using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Organization.Domain;

namespace RezSaaS.Modules.Organization.Infrastructure.Persistence;

public sealed class OrganizationDbContext : DbContext
{
    private readonly ITenantContextAccessor? tenantContextAccessor;

    public const string ConnectionStringName = "OrganizationDatabase";

    public OrganizationDbContext(
        DbContextOptions<OrganizationDbContext> options,
        ITenantContextAccessor? tenantContextAccessor = null)
        : base(options)
    {
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public DbSet<Branch> Branches => Set<Branch>();

    public DbSet<Business> Businesses => Set<Business>();

    public DbSet<Skill> Skills => Set<Skill>();

    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();

    public DbSet<StaffSkill> StaffSkills => Set<StaffSkill>();

    private Guid? CurrentTenantId => tenantContextAccessor?.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("organization");

        modelBuilder.Entity<Business>(business =>
        {
            business.ToTable("Businesses");
            business.HasKey(entity => entity.Id);
            business.Property(entity => entity.Slug).HasMaxLength(64).IsRequired();
            business.Property(entity => entity.NormalizedSlug).HasMaxLength(64).IsRequired();
            business.Property(entity => entity.DisplayName).HasMaxLength(200).IsRequired();
            business.Property(entity => entity.CategoryKey).HasMaxLength(80).IsRequired();
            business.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            business.HasIndex(entity => new { entity.TenantId, entity.NormalizedSlug }).IsUnique();
            business.HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
        });

        modelBuilder.Entity<Branch>(branch =>
        {
            branch.ToTable("Branches");
            branch.HasKey(entity => entity.Id);
            branch.Property(entity => entity.Slug).HasMaxLength(64).IsRequired();
            branch.Property(entity => entity.NormalizedSlug).HasMaxLength(64).IsRequired();
            branch.Property(entity => entity.DisplayName).HasMaxLength(200).IsRequired();
            branch.Property(entity => entity.TimeZoneId).HasMaxLength(80).IsRequired();
            branch.HasIndex(entity => new { entity.TenantId, entity.BusinessId, entity.NormalizedSlug }).IsUnique();
            branch.HasOne(entity => entity.Business)
                .WithMany()
                .HasForeignKey(entity => entity.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
            branch.HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
        });

        modelBuilder.Entity<StaffMember>(staff =>
        {
            staff.ToTable("StaffMembers");
            staff.HasKey(entity => entity.Id);
            staff.Property(entity => entity.DisplayName).HasMaxLength(200).IsRequired();
            staff.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            staff.HasIndex(entity => new { entity.TenantId, entity.BranchId });
            staff.HasOne(entity => entity.Branch)
                .WithMany()
                .HasForeignKey(entity => entity.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
            staff.HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
        });

        modelBuilder.Entity<Skill>(skill =>
        {
            skill.ToTable("Skills");
            skill.HasKey(entity => entity.Id);
            skill.Property(entity => entity.Name).HasMaxLength(120).IsRequired();
            skill.Property(entity => entity.NormalizedName).HasMaxLength(120).IsRequired();
            skill.HasIndex(entity => new { entity.TenantId, entity.NormalizedName }).IsUnique();
            skill.HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
        });

        modelBuilder.Entity<StaffSkill>(staffSkill =>
        {
            staffSkill.ToTable("StaffSkills");
            staffSkill.HasKey(entity => entity.Id);
            staffSkill.HasIndex(entity => new { entity.TenantId, entity.StaffMemberId, entity.SkillId }).IsUnique();
            staffSkill.HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
        });
    }
}
