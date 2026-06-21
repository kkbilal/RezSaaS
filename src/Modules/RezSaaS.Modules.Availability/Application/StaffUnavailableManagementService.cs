using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Auditing;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Availability.Domain;
using RezSaaS.Modules.Availability.Infrastructure.Persistence;

namespace RezSaaS.Modules.Availability.Application;

public sealed class StaffUnavailableManagementService
{
    public const string InvalidRequest = "STAFF_UNAVAILABLE_INVALID_REQUEST";
    public const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    public const string NotFound = "STAFF_UNAVAILABLE_NOT_FOUND";
    public const string OverlapConflict = "STAFF_UNAVAILABLE_OVERLAP";

    private readonly IAuditLogRecorder auditLogRecorder;
    private readonly AvailabilityDbContext dbContext;
    private readonly ITenantContextAccessor tenantContextAccessor;
    private readonly TimeProvider timeProvider;

    public StaffUnavailableManagementService(
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

    public async Task<StaffUnavailableManagementResult> ListForStaffAsync(
        Guid staffMemberId, CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null)
            return StaffUnavailableManagementResult.Failure(MissingTenantContext);

        List<StaffUnavailableTimeView> items = await dbContext.StaffUnavailableTimes
            .AsNoTracking()
            .Where(entity => entity.StaffMemberId == staffMemberId)
            .OrderBy(entity => entity.StartUtc)
            .Select(entity => ToView(entity))
            .ToListAsync(cancellationToken);

        return StaffUnavailableManagementResult.SuccessList(items);
    }

    public async Task<StaffUnavailableManagementResult> CreateAsync(
        Guid actorUserAccountId, Guid staffMemberId,
        DateTimeOffset startUtc, DateTimeOffset endUtc, string reason,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
            return StaffUnavailableManagementResult.Failure(MissingTenantContext);

        if (endUtc <= startUtc)
            return StaffUnavailableManagementResult.Failure(InvalidRequest);

        string trimmedReason = reason?.Trim() ?? string.Empty;
        if (trimmedReason.Length > 200)
            return StaffUnavailableManagementResult.Failure(InvalidRequest);

        bool hasOverlap = await dbContext.StaffUnavailableTimes
            .AnyAsync(entity => entity.StaffMemberId == staffMemberId
                && entity.StartUtc < endUtc
                && entity.EndUtc > startUtc,
                cancellationToken);

        if (hasOverlap)
            return StaffUnavailableManagementResult.Failure(OverlapConflict);

        DateTimeOffset now = timeProvider.GetUtcNow();
        StaffUnavailableTime unavailable = StaffUnavailableTime.Create(
            tenantId, staffMemberId, startUtc, endUtc, trimmedReason);

        dbContext.StaffUnavailableTimes.Add(unavailable);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogRecorder.RecordAsync(new AuditLogRecord(
            tenantId, actorUserAccountId, "availability.staff-unavailable.created",
            $$"""{"tenantId":"{{tenantId}}","staffMemberId":"{{staffMemberId}}","unavailableId":"{{unavailable.Id}}"}""",
            now), cancellationToken);

        return StaffUnavailableManagementResult.Success(ToView(unavailable));
    }

    public async Task<StaffUnavailableManagementResult> DeleteAsync(
        Guid actorUserAccountId, Guid unavailableId,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
            return StaffUnavailableManagementResult.Failure(MissingTenantContext);

        StaffUnavailableTime? item = await dbContext.StaffUnavailableTimes
            .FirstOrDefaultAsync(entity => entity.Id == unavailableId, cancellationToken);

        if (item is null)
            return StaffUnavailableManagementResult.Failure(NotFound);

        dbContext.StaffUnavailableTimes.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        DateTimeOffset now = timeProvider.GetUtcNow();
        await auditLogRecorder.RecordAsync(new AuditLogRecord(
            tenantId, actorUserAccountId, "availability.staff-unavailable.deleted",
            $$"""{"tenantId":"{{tenantId}}","unavailableId":"{{unavailableId}}"}""",
            now), cancellationToken);

        return StaffUnavailableManagementResult.Success(ToView(item));
    }

    private static StaffUnavailableTimeView ToView(StaffUnavailableTime item)
        => new(item.Id, item.StaffMemberId, item.StartUtc, item.EndUtc, item.Reason);
}
