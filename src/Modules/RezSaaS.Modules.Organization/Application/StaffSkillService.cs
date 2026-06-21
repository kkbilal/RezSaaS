using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Organization.Domain;
using RezSaaS.Modules.Organization.Infrastructure.Persistence;

namespace RezSaaS.Modules.Organization.Application;

public sealed class StaffSkillService
{
    public const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    public const string StaffNotFound = "STAFF_NOT_FOUND";
    public const string SkillNotFound = "SKILL_NOT_FOUND";
    public const string AlreadyAssigned = "SKILL_ALREADY_ASSIGNED";
    public const string NotAssigned = "SKILL_NOT_ASSIGNED";

    private readonly OrganizationDbContext dbContext;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public StaffSkillService(
        OrganizationDbContext dbContext,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.dbContext = dbContext;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<StaffSkillActionResult> AssignAsync(
        Guid actorUserAccountId,
        Guid staffMemberId,
        Guid skillId,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return StaffSkillActionResult.Failure(MissingTenantContext);
        }

        bool staffExists = await dbContext.StaffMembers
            .AnyAsync(entity => entity.Id == staffMemberId, cancellationToken);

        if (!staffExists)
        {
            return StaffSkillActionResult.Failure(StaffNotFound);
        }

        bool skillExists = await dbContext.Skills
            .AnyAsync(entity => entity.Id == skillId, cancellationToken);

        if (!skillExists)
        {
            return StaffSkillActionResult.Failure(SkillNotFound);
        }

        bool alreadyAssigned = await dbContext.StaffSkills
            .AnyAsync(entity => entity.StaffMemberId == staffMemberId
                && entity.SkillId == skillId,
                cancellationToken);

        if (alreadyAssigned)
        {
            return StaffSkillActionResult.Failure(AlreadyAssigned);
        }

        dbContext.StaffSkills.Add(
            StaffSkill.Create(tenantId, staffMemberId, skillId));

        await dbContext.SaveChangesAsync(cancellationToken);

        return StaffSkillActionResult.Success();
    }

    public async Task<StaffSkillActionResult> RemoveAsync(
        Guid staffMemberId,
        Guid skillId,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null)
        {
            return StaffSkillActionResult.Failure(MissingTenantContext);
        }

        StaffSkill? staffSkill = await dbContext.StaffSkills
            .FirstOrDefaultAsync(entity => entity.StaffMemberId == staffMemberId
                && entity.SkillId == skillId,
                cancellationToken);

        if (staffSkill is null)
        {
            return StaffSkillActionResult.Failure(NotAssigned);
        }

        dbContext.StaffSkills.Remove(staffSkill);
        await dbContext.SaveChangesAsync(cancellationToken);

        return StaffSkillActionResult.Success();
    }

    public async Task<IReadOnlyCollection<Guid>> GetSkillIdsForStaffAsync(
        Guid staffMemberId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.StaffSkills
            .AsNoTracking()
            .Where(entity => entity.StaffMemberId == staffMemberId)
            .Select(entity => entity.SkillId)
            .ToListAsync(cancellationToken);
    }
}

public sealed record StaffSkillActionResult(
    bool Succeeded,
    string? ErrorCode)
{
    public static StaffSkillActionResult Success()
    {
        return new StaffSkillActionResult(true, null);
    }

    public static StaffSkillActionResult Failure(string errorCode)
    {
        return new StaffSkillActionResult(false, errorCode);
    }
}
