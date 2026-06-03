using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Resources.Domain;
using RezSaaS.Modules.Resources.Infrastructure.Persistence;

namespace RezSaaS.Modules.Resources.Application;

public sealed class PublicResourceAvailabilityQueryService
{
    private readonly ResourcesDbContext dbContext;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public PublicResourceAvailabilityQueryService(
        ResourcesDbContext dbContext,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.dbContext = dbContext;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<IReadOnlyCollection<PublicResourceCandidateView>> GetActiveResourcesAsync(
        Guid branchId,
        Guid? resourceTypeId,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null)
        {
            return [];
        }

        IQueryable<Resource> query = dbContext.Resources
            .AsNoTracking()
            .Where(entity => entity.BranchId == branchId && entity.Status == ResourceStatus.Active);

        if (resourceTypeId is not null)
        {
            query = query.Where(entity => entity.ResourceTypeId == resourceTypeId);
        }

        return await query
            .OrderBy(entity => entity.DisplayName)
            .Select(entity => new PublicResourceCandidateView(
                entity.Id,
                entity.ResourceTypeId,
                entity.DisplayName))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PublicResourceBlockView>> GetResourceBlocksAsync(
        IReadOnlyCollection<Guid> resourceIds,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null || resourceIds.Count == 0)
        {
            return [];
        }

        Guid[] distinctResourceIds = resourceIds
            .Distinct()
            .ToArray();

        return await dbContext.ResourceBlocks
            .AsNoTracking()
            .Where(entity => distinctResourceIds.Contains(entity.ResourceId)
                && entity.StartUtc < toUtc
                && entity.EndUtc > fromUtc)
            .OrderBy(entity => entity.StartUtc)
            .Select(entity => new PublicResourceBlockView(
                entity.ResourceId,
                entity.StartUtc,
                entity.EndUtc))
            .ToListAsync(cancellationToken);
    }
}
