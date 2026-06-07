using Microsoft.EntityFrameworkCore;
using RezSaaS.Modules.TenantManagement.Domain;
using RezSaaS.Modules.TenantManagement.Infrastructure.Persistence;

namespace RezSaaS.Modules.TenantManagement.Application;

public sealed class UserTenantMembershipQueryService
{
    private readonly TenantManagementDbContext dbContext;

    public UserTenantMembershipQueryService(TenantManagementDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<bool> HasActiveMembershipsAsync(
        Guid userAccountId,
        CancellationToken cancellationToken = default)
    {
        return userAccountId != Guid.Empty
            && await dbContext.Memberships
                .AsNoTracking()
                .AnyAsync(
                    entity => entity.UserAccountId == userAccountId
                        && entity.Status == TenantMembershipStatus.Active,
                    cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserTenantMembershipView>> GetActiveMembershipsAsync(
        Guid userAccountId,
        CancellationToken cancellationToken = default)
    {
        if (userAccountId == Guid.Empty)
        {
            return [];
        }

        return await dbContext.Memberships
            .AsNoTracking()
            .Join(
                dbContext.Tenants.AsNoTracking(),
                membership => membership.TenantId,
                tenant => tenant.Id,
                (membership, tenant) => new { membership, tenant })
            .Where(entity => entity.membership.UserAccountId == userAccountId
                && entity.membership.Status == TenantMembershipStatus.Active
                && entity.tenant.Status == TenantStatus.Active)
            .OrderBy(entity => entity.tenant.DisplayName)
            .ThenBy(entity => entity.membership.CreatedAtUtc)
            .Select(entity => new UserTenantMembershipView(
                entity.membership.Id,
                entity.membership.TenantId,
                entity.tenant.Slug,
                entity.tenant.DisplayName,
                entity.tenant.Status,
                entity.membership.Role,
                entity.membership.Status,
                entity.membership.BranchId,
                entity.membership.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
