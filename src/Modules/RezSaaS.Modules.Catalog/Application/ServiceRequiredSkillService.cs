using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Catalog.Domain;
using RezSaaS.Modules.Catalog.Infrastructure.Persistence;

namespace RezSaaS.Modules.Catalog.Application;

public sealed class ServiceRequiredSkillService
{
    public const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    public const string VariantNotFound = "VARIANT_NOT_FOUND";
    public const string SkillNotFound = "SKILL_NOT_FOUND";
    public const string AlreadyAssigned = "SKILL_ALREADY_ASSIGNED";
    public const string NotAssigned = "SKILL_NOT_ASSIGNED";

    private readonly CatalogDbContext dbContext;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public ServiceRequiredSkillService(
        CatalogDbContext dbContext,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.dbContext = dbContext;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<IReadOnlyCollection<Guid>> GetSkillIdsForVariantAsync(
        Guid variantId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ServiceRequiredSkills
            .AsNoTracking()
            .Where(entity => entity.ServiceVariantId == variantId)
            .Select(entity => entity.SkillId)
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceRequiredSkillActionResult> AssignAsync(
        Guid actorUserAccountId, Guid variantId, Guid skillId,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
            return ServiceRequiredSkillActionResult.Failure(MissingTenantContext);

        bool variantExists = await dbContext.ServiceVariants
            .AnyAsync(entity => entity.Id == variantId, cancellationToken);

        if (!variantExists)
            return ServiceRequiredSkillActionResult.Failure(VariantNotFound);

        bool alreadyAssigned = await dbContext.ServiceRequiredSkills
            .AnyAsync(entity => entity.ServiceVariantId == variantId
                && entity.SkillId == skillId, cancellationToken);

        if (alreadyAssigned)
            return ServiceRequiredSkillActionResult.Failure(AlreadyAssigned);

        dbContext.ServiceRequiredSkills.Add(
            ServiceRequiredSkill.Create(tenantId, variantId, skillId));

        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceRequiredSkillActionResult.Success();
    }

    public async Task<ServiceRequiredSkillActionResult> RemoveAsync(
        Guid variantId, Guid skillId, CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null)
            return ServiceRequiredSkillActionResult.Failure(MissingTenantContext);

        ServiceRequiredSkill? requiredSkill = await dbContext.ServiceRequiredSkills
            .FirstOrDefaultAsync(entity => entity.ServiceVariantId == variantId
                && entity.SkillId == skillId, cancellationToken);

        if (requiredSkill is null)
            return ServiceRequiredSkillActionResult.Failure(NotAssigned);

        dbContext.ServiceRequiredSkills.Remove(requiredSkill);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceRequiredSkillActionResult.Success();
    }
}
