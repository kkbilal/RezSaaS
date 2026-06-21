using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Auditing;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Organization.Domain;
using RezSaaS.Modules.Organization.Infrastructure.Persistence;

namespace RezSaaS.Modules.Organization.Application;

public sealed class SkillManagementService
{
    public const string InvalidRequest = "SKILL_INVALID_REQUEST";
    public const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    public const string SkillNotFound = "SKILL_NOT_FOUND";
    public const string NameConflict = "SKILL_NAME_CONFLICT";
    public const string SkillInUse = "SKILL_IN_USE";

    private readonly IAuditLogRecorder auditLogRecorder;
    private readonly OrganizationDbContext dbContext;
    private readonly ITenantContextAccessor tenantContextAccessor;
    private readonly TimeProvider timeProvider;

    public SkillManagementService(
        OrganizationDbContext dbContext,
        ITenantContextAccessor tenantContextAccessor,
        IAuditLogRecorder auditLogRecorder,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.tenantContextAccessor = tenantContextAccessor;
        this.auditLogRecorder = auditLogRecorder;
        this.timeProvider = timeProvider;
    }

    public async Task<SkillManagementResult> ListAsync(
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null)
        {
            return SkillManagementResult.Failure(MissingTenantContext);
        }

        List<SkillView> skills = await dbContext.Skills
            .AsNoTracking()
            .OrderBy(entity => entity.Name)
            .Select(entity => ToView(entity))
            .ToListAsync(cancellationToken);

        return SkillManagementResult.SuccessList(skills);
    }

    public async Task<SkillManagementResult> CreateAsync(
        CreateSkillCommand command,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return SkillManagementResult.Failure(MissingTenantContext);
        }

        string trimmedName = command.Name?.Trim() ?? string.Empty;

        if (trimmedName.Length < 2 || trimmedName.Length > 120)
        {
            return SkillManagementResult.Failure(InvalidRequest);
        }

        string upperName = trimmedName.ToUpperInvariant();
        bool nameExists = await dbContext.Skills
            .AnyAsync(entity => entity.NormalizedName == upperName,
                cancellationToken);

        if (nameExists)
        {
            return SkillManagementResult.Failure(NameConflict);
        }

        Skill skill = Skill.Create(tenantId, trimmedName);
        dbContext.Skills.Add(skill);
        await dbContext.SaveChangesAsync(cancellationToken);

        DateTimeOffset now = timeProvider.GetUtcNow();
        await auditLogRecorder.RecordAsync(
            new AuditLogRecord(
                tenantId,
                command.ActorUserAccountId,
                "organization.skill.created",
                $$"""{"tenantId":"{{tenantId}}","skillId":"{{skill.Id}}","name":"{{trimmedName}}"}""",
                now),
            cancellationToken);

        return SkillManagementResult.Success(ToView(skill));
    }

    public async Task<SkillManagementResult> DeleteAsync(
        Guid actorUserAccountId,
        Guid skillId,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return SkillManagementResult.Failure(MissingTenantContext);
        }

        Skill? skill = await dbContext.Skills
            .FirstOrDefaultAsync(entity => entity.Id == skillId, cancellationToken);

        if (skill is null)
        {
            return SkillManagementResult.Failure(SkillNotFound);
        }

        bool inUse = await dbContext.StaffSkills
            .AnyAsync(entity => entity.SkillId == skillId, cancellationToken);

        if (inUse)
        {
            return SkillManagementResult.Failure(SkillInUse);
        }

        dbContext.Skills.Remove(skill);
        await dbContext.SaveChangesAsync(cancellationToken);

        DateTimeOffset now = timeProvider.GetUtcNow();
        await auditLogRecorder.RecordAsync(
            new AuditLogRecord(
                tenantId,
                actorUserAccountId,
                "organization.skill.deleted",
                $$"""{"tenantId":"{{tenantId}}","skillId":"{{skillId}}"}""",
                now),
            cancellationToken);

        return SkillManagementResult.Success(ToView(skill));
    }

    private static SkillView ToView(Skill skill)
    {
        return new SkillView(skill.Id, skill.Name);
    }
}
