using Microsoft.EntityFrameworkCore;
using RezSaaS.Modules.Organization.Infrastructure.Persistence;

namespace RezSaaS.Modules.Organization.Application;

public sealed class BusinessEntityLabelQueryService
{
    private readonly OrganizationDbContext dbContext;

    public BusinessEntityLabelQueryService(OrganizationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<BusinessLabelView?> GetBusinessLabelAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Businesses
            .AsNoTracking()
            .OrderBy(entity => entity.DisplayName)
            .Select(entity => new BusinessLabelView(
                entity.TenantId,
                entity.Id,
                entity.Slug,
                entity.DisplayName))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, BranchLabelView>> GetBranchLabelsAsync(
        IReadOnlyCollection<Guid> branchIds,
        CancellationToken cancellationToken = default)
    {
        if (branchIds.Count == 0)
        {
            return new Dictionary<Guid, BranchLabelView>();
        }

        Guid[] distinctBranchIds = branchIds
            .Where(entity => entity != Guid.Empty)
            .Distinct()
            .ToArray();

        return await dbContext.Branches
            .AsNoTracking()
            .Where(entity => distinctBranchIds.Contains(entity.Id))
            .Select(entity => new BranchLabelView(
                entity.Id,
                entity.Slug,
                entity.DisplayName,
                entity.TimeZoneId))
            .ToDictionaryAsync(entity => entity.Id, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, BranchLabelView>> GetAllBranchLabelsAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Branches
            .AsNoTracking()
            .OrderBy(entity => entity.DisplayName)
            .Select(entity => new BranchLabelView(
                entity.Id,
                entity.Slug,
                entity.DisplayName,
                entity.TimeZoneId))
            .ToDictionaryAsync(entity => entity.Id, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, StaffMemberLabelView>> GetStaffMemberLabelsAsync(
        IReadOnlyCollection<Guid> staffMemberIds,
        CancellationToken cancellationToken = default)
    {
        if (staffMemberIds.Count == 0)
        {
            return new Dictionary<Guid, StaffMemberLabelView>();
        }

        Guid[] distinctStaffMemberIds = staffMemberIds
            .Where(entity => entity != Guid.Empty)
            .Distinct()
            .ToArray();

        return await dbContext.StaffMembers
            .AsNoTracking()
            .Where(entity => distinctStaffMemberIds.Contains(entity.Id))
            .Select(entity => new StaffMemberLabelView(
                entity.Id,
                entity.DisplayName))
            .ToDictionaryAsync(entity => entity.Id, cancellationToken);
    }
}
