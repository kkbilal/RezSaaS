using Microsoft.EntityFrameworkCore;
using RezSaaS.Modules.TenantManagement.Domain;
using RezSaaS.Modules.TenantManagement.Infrastructure.Persistence;

namespace RezSaaS.Modules.TenantManagement.Application;

public sealed class TenantControlPlaneQueryService
{
    private readonly TenantManagementDbContext dbContext;

    public TenantControlPlaneQueryService(TenantManagementDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public static bool IsValidStatusOrEmpty(string? status)
    {
        return string.IsNullOrWhiteSpace(status)
            || TryParseStatus(status, out _);
    }

    public async Task<IReadOnlyCollection<TenantListItemView>> GetAsync(
        TenantControlPlaneQuery tenantQuery,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Tenant> query = dbContext.Tenants
            .AsNoTracking()
            .Include(entity => entity.Memberships);

        if (!string.IsNullOrWhiteSpace(tenantQuery.Search))
        {
            string normalizedSearch = tenantQuery.Search.Trim().ToUpperInvariant();
            string searchPattern = $"%{tenantQuery.Search.Trim()}%";
            string normalizedSearchPattern = $"%{normalizedSearch}%";
            query = query.Where(entity =>
                EF.Functions.Like(entity.NormalizedSlug, normalizedSearchPattern)
                || EF.Functions.ILike(entity.DisplayName, searchPattern));
        }

        if (TryParseStatus(tenantQuery.Status, out TenantStatus parsedStatus))
        {
            query = query.Where(entity => entity.Status == parsedStatus);
        }

        List<Tenant> tenants = await query
            .OrderBy(entity => entity.CreatedAtUtc)
            .Take(Math.Clamp(tenantQuery.Take, 1, 100))
            .ToListAsync(cancellationToken);

        return tenants.Select(ToListItemView).ToArray();
    }

    public async Task<TenantDetailView?> GetByIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            return null;
        }

        Tenant? tenant = await dbContext.Tenants
            .AsNoTracking()
            .Include(entity => entity.Memberships)
            .SingleOrDefaultAsync(entity => entity.Id == tenantId, cancellationToken);

        return tenant is null ? null : ToDetailView(tenant);
    }

    public async Task<IReadOnlyCollection<TenantMembershipView>> GetMembershipsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            return [];
        }

        return await dbContext.Memberships
            .AsNoTracking()
            .Where(entity => entity.TenantId == tenantId)
            .OrderBy(entity => entity.CreatedAtUtc)
            .Select(entity => new TenantMembershipView(
                entity.Id,
                entity.TenantId,
                entity.UserAccountId,
                entity.Role,
                entity.Status,
                entity.BranchId,
                entity.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    private static bool TryParseStatus(
        string? status,
        out TenantStatus parsedStatus)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            parsedStatus = default;
            return false;
        }

        return Enum.TryParse(status, ignoreCase: true, out parsedStatus)
            && Enum.IsDefined(parsedStatus);
    }

    private static TenantDetailView ToDetailView(Tenant tenant)
    {
        return new TenantDetailView(
            tenant.Id,
            tenant.Slug,
            tenant.DisplayName,
            tenant.Status,
            tenant.CreatedAtUtc,
            tenant.SuspendedAtUtc,
            tenant.ClosedAtUtc,
            tenant.Memberships
                .OrderBy(entity => entity.CreatedAtUtc)
                .Select(entity => new TenantMembershipView(
                    entity.Id,
                    entity.TenantId,
                    entity.UserAccountId,
                    entity.Role,
                    entity.Status,
                    entity.BranchId,
                    entity.CreatedAtUtc))
                .ToArray());
    }

    private static TenantListItemView ToListItemView(Tenant tenant)
    {
        return new TenantListItemView(
            tenant.Id,
            tenant.Slug,
            tenant.DisplayName,
            tenant.Status,
            tenant.CreatedAtUtc,
            tenant.SuspendedAtUtc,
            tenant.ClosedAtUtc,
            tenant.Memberships.Count(entity => entity.Status == TenantMembershipStatus.Active));
    }
}
