using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Availability.Infrastructure.Persistence;

namespace RezSaaS.Modules.Availability.Application;

public sealed class AvailabilityQueryService
{
    private readonly AvailabilityDbContext dbContext;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public AvailabilityQueryService(
        AvailabilityDbContext dbContext,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.dbContext = dbContext;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<AvailabilitySnapshot?> GetBranchSnapshotAsync(
        Guid branchId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        IReadOnlyCollection<Guid>? staffMemberIds = null,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null)
        {
            return null;
        }

        if (toUtc <= fromUtc)
        {
            throw new ArgumentException("End must be later than start.", nameof(toUtc));
        }

        List<BranchWorkingHoursView> workingHours = await dbContext.BranchWorkingHours
            .AsNoTracking()
            .Where(entity => entity.BranchId == branchId)
            .OrderBy(entity => entity.DayOfWeek)
            .Select(entity => new BranchWorkingHoursView(
                entity.Id,
                entity.DayOfWeek,
                entity.OpensAt,
                entity.ClosesAt,
                entity.IsClosed))
            .ToListAsync(cancellationToken);

        HashSet<Guid>? staffFilter = staffMemberIds is null
            ? null
            : new HashSet<Guid>(staffMemberIds);

        IQueryable<Domain.StaffUnavailableTime> unavailableQuery = dbContext.StaffUnavailableTimes
            .AsNoTracking()
            .Where(entity => entity.StartUtc < toUtc && entity.EndUtc > fromUtc);

        if (staffFilter is { Count: > 0 })
        {
            unavailableQuery = unavailableQuery.Where(entity => staffFilter.Contains(entity.StaffMemberId));
        }

        List<StaffUnavailableTimeView> unavailableTimes = await unavailableQuery
            .OrderBy(entity => entity.StartUtc)
            .Select(entity => new StaffUnavailableTimeView(
                entity.Id,
                entity.StaffMemberId,
                entity.StartUtc,
                entity.EndUtc,
                entity.Reason))
            .ToListAsync(cancellationToken);

        return new AvailabilitySnapshot(
            branchId,
            fromUtc,
            toUtc,
            workingHours,
            unavailableTimes);
    }

    public async Task<IReadOnlyCollection<BranchWorkingHoursView>> GetBranchWorkingHoursAsync(
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null)
        {
            return [];
        }

        return await dbContext.BranchWorkingHours
            .AsNoTracking()
            .Where(entity => entity.BranchId == branchId)
            .OrderBy(entity => entity.DayOfWeek)
            .Select(entity => new BranchWorkingHoursView(
                entity.Id,
                entity.DayOfWeek,
                entity.OpensAt,
                entity.ClosesAt,
                entity.IsClosed))
            .ToListAsync(cancellationToken);
    }
}
