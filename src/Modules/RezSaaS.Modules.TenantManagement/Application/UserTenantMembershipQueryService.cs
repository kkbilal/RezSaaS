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
}
