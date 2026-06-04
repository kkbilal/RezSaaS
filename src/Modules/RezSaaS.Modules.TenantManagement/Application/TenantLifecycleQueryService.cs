using Microsoft.EntityFrameworkCore;
using RezSaaS.Modules.TenantManagement.Domain;
using RezSaaS.Modules.TenantManagement.Infrastructure.Persistence;

namespace RezSaaS.Modules.TenantManagement.Application;

public sealed class TenantLifecycleQueryService
{
    private readonly TenantManagementDbContext dbContext;

    public TenantLifecycleQueryService(TenantManagementDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<Guid>> GetActiveTenantIdsAsync(
        int take = 500,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Tenants
            .AsNoTracking()
            .Where(entity => entity.Status == TenantStatus.Active)
            .OrderBy(entity => entity.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 5000))
            .Select(entity => entity.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsActiveAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            return false;
        }

        return await dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(
                entity => entity.Id == tenantId
                    && entity.Status == TenantStatus.Active,
                cancellationToken);
    }
}
