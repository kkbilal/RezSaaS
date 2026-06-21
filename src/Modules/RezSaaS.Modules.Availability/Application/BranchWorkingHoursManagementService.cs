using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Auditing;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Availability.Domain;
using RezSaaS.Modules.Availability.Infrastructure.Persistence;

namespace RezSaaS.Modules.Availability.Application;

public sealed class BranchWorkingHoursManagementService
{
    public const string InvalidRequest = "WORKING_HOURS_INVALID_REQUEST";
    public const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    public const string BranchNotFound = "BRANCH_NOT_FOUND";
    public const string HoursNotFound = "WORKING_HOURS_NOT_FOUND";

    private readonly IAuditLogRecorder auditLogRecorder;
    private readonly AvailabilityDbContext dbContext;
    private readonly ITenantContextAccessor tenantContextAccessor;
    private readonly TimeProvider timeProvider;

    public BranchWorkingHoursManagementService(
        AvailabilityDbContext dbContext,
        ITenantContextAccessor tenantContextAccessor,
        IAuditLogRecorder auditLogRecorder,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.tenantContextAccessor = tenantContextAccessor;
        this.auditLogRecorder = auditLogRecorder;
        this.timeProvider = timeProvider;
    }

    public async Task<BranchWorkingHoursManagementResult> GetForBranchAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null)
            return BranchWorkingHoursManagementResult.Failure(MissingTenantContext);

        List<BranchWorkingHoursView> hours = await dbContext.BranchWorkingHours
            .AsNoTracking()
            .Where(entity => entity.BranchId == branchId)
            .OrderBy(entity => entity.DayOfWeek)
            .Select(entity => ToView(entity))
            .ToListAsync(cancellationToken);

        return BranchWorkingHoursManagementResult.SuccessList(hours);
    }

    public async Task<BranchWorkingHoursManagementResult> UpsertAsync(
        Guid actorUserAccountId, Guid branchId, DayOfWeek dayOfWeek,
        TimeOnly opensAt, TimeOnly closesAt, bool isClosed,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
            return BranchWorkingHoursManagementResult.Failure(MissingTenantContext);

        if (!isClosed && closesAt <= opensAt)
            return BranchWorkingHoursManagementResult.Failure(InvalidRequest);

        BranchWorkingHours? existing = await dbContext.BranchWorkingHours
            .FirstOrDefaultAsync(entity => entity.BranchId == branchId && entity.DayOfWeek == dayOfWeek,
                cancellationToken);

        DateTimeOffset now = timeProvider.GetUtcNow();

        if (existing is not null)
        {
            existing.SetHours(opensAt, closesAt, isClosed);
            await dbContext.SaveChangesAsync(cancellationToken);

            await auditLogRecorder.RecordAsync(new AuditLogRecord(
                tenantId, actorUserAccountId, "availability.working-hours.updated",
                $$"""{"tenantId":"{{tenantId}}","branchId":"{{branchId}}","dayOfWeek":"{{dayOfWeek}}"}""",
                now), cancellationToken);

            return BranchWorkingHoursManagementResult.Success(ToView(existing));
        }

        BranchWorkingHours hours = BranchWorkingHours.Create(
            tenantId, branchId, dayOfWeek, opensAt, closesAt, isClosed);

        dbContext.BranchWorkingHours.Add(hours);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogRecorder.RecordAsync(new AuditLogRecord(
            tenantId, actorUserAccountId, "availability.working-hours.created",
            $$"""{"tenantId":"{{tenantId}}","branchId":"{{branchId}}","dayOfWeek":"{{dayOfWeek}}"}""",
            now), cancellationToken);

        return BranchWorkingHoursManagementResult.Success(ToView(hours));
    }

    public async Task<BranchWorkingHoursManagementResult> ClearBranchAsync(
        Guid actorUserAccountId, Guid branchId,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
            return BranchWorkingHoursManagementResult.Failure(MissingTenantContext);

        List<BranchWorkingHours> existing = await dbContext.BranchWorkingHours
            .Where(entity => entity.BranchId == branchId)
            .ToListAsync(cancellationToken);

        if (existing.Count == 0)
            return BranchWorkingHoursManagementResult.Failure(HoursNotFound);

        dbContext.BranchWorkingHours.RemoveRange(existing);
        await dbContext.SaveChangesAsync(cancellationToken);

        DateTimeOffset now = timeProvider.GetUtcNow();
        await auditLogRecorder.RecordAsync(new AuditLogRecord(
            tenantId, actorUserAccountId, "availability.working-hours.cleared",
            $$"""{"tenantId":"{{tenantId}}","branchId":"{{branchId}}"}""",
            now), cancellationToken);

        return BranchWorkingHoursManagementResult.SuccessList(existing.Select(ToView).ToList());
    }

    private static BranchWorkingHoursView ToView(BranchWorkingHours hours)
        => new(hours.Id, hours.DayOfWeek, hours.OpensAt, hours.ClosesAt, hours.IsClosed);
}
