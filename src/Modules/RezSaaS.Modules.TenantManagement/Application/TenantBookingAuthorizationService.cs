using Microsoft.EntityFrameworkCore;
using RezSaaS.Modules.TenantManagement.Domain;
using RezSaaS.Modules.TenantManagement.Infrastructure.Persistence;

namespace RezSaaS.Modules.TenantManagement.Application;

public sealed class TenantBookingAuthorizationService
{
    private readonly TenantManagementDbContext dbContext;

    public TenantBookingAuthorizationService(TenantManagementDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<bool> CanManageAppointmentRequestsAsync(
        Guid tenantId,
        Guid userAccountId,
        Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty || userAccountId == Guid.Empty)
        {
            return false;
        }

        List<TenantMembershipScopeView> memberships = await dbContext.Memberships
            .AsNoTracking()
            .Where(entity => entity.TenantId == tenantId
                && entity.UserAccountId == userAccountId
                && entity.Status == TenantMembershipStatus.Active
                && (entity.Role == TenantMembershipRole.BusinessOwner
                    || entity.Role == TenantMembershipRole.BranchManager))
            .Select(entity => new TenantMembershipScopeView(entity.Role, entity.BranchId))
            .ToListAsync(cancellationToken);

        return memberships.Any(membership =>
            membership.Role == TenantMembershipRole.BusinessOwner
            || membership.Role == TenantMembershipRole.BranchManager
                && (membership.BranchId is null
                    || branchId is not null && membership.BranchId == branchId));
    }

    public async Task<bool> HasAppointmentRequestManagementMembershipAsync(
        Guid tenantId,
        Guid userAccountId,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty || userAccountId == Guid.Empty)
        {
            return false;
        }

        return await dbContext.Memberships
            .AsNoTracking()
            .AnyAsync(entity => entity.TenantId == tenantId
                && entity.UserAccountId == userAccountId
                && entity.Status == TenantMembershipStatus.Active
                && (entity.Role == TenantMembershipRole.BusinessOwner
                    || entity.Role == TenantMembershipRole.BranchManager),
                cancellationToken);
    }
}
