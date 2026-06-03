using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Auditing;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Booking.Domain;
using RezSaaS.Modules.Booking.Infrastructure.Persistence;

namespace RezSaaS.Modules.Booking.Application;

public sealed class DeclineAppointmentRequestService
{
    private const string AlreadyClosed = "APPOINTMENT_REQUEST_ALREADY_CLOSED";
    private const string Expired = "APPOINTMENT_REQUEST_EXPIRED";
    private const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    private const string NotFound = "APPOINTMENT_REQUEST_NOT_FOUND";

    private readonly IAuditLogRecorder auditLogRecorder;
    private readonly BookingDbContext dbContext;
    private readonly ITenantContextAccessor tenantContextAccessor;
    private readonly TimeProvider timeProvider;

    public DeclineAppointmentRequestService(
        BookingDbContext dbContext,
        IAuditLogRecorder auditLogRecorder,
        ITenantContextAccessor tenantContextAccessor,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.auditLogRecorder = auditLogRecorder;
        this.tenantContextAccessor = tenantContextAccessor;
        this.timeProvider = timeProvider;
    }

    public async Task<AppointmentRequestDecisionResult> DeclineAsync(
        Guid appointmentRequestId,
        Guid actorUserAccountId,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return AppointmentRequestDecisionResult.Failure(MissingTenantContext);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        AppointmentRequest? request = await dbContext.AppointmentRequests
            .SingleOrDefaultAsync(
                entity => entity.Id == appointmentRequestId,
                cancellationToken);

        if (request is null)
        {
            return AppointmentRequestDecisionResult.Failure(NotFound);
        }

        if (request.Status == AppointmentRequestStatus.Declined)
        {
            return AppointmentRequestDecisionResult.Success(appointmentId: null);
        }

        if (request.Status != AppointmentRequestStatus.PendingApproval)
        {
            return AppointmentRequestDecisionResult.Failure(AlreadyClosed);
        }

        if (request.ExpiresAtUtc <= now)
        {
            request.Expire();
            await dbContext.SaveChangesAsync(cancellationToken);
            await RecordAuditAsync(
                tenantId,
                actorUserAccountId,
                "booking.request.expired_on_decline",
                $$"""{"tenantId":"{{tenantId}}","appointmentRequestId":"{{request.Id}}"}""",
                now,
                cancellationToken);

            return AppointmentRequestDecisionResult.Failure(Expired);
        }

        request.Decline();
        await dbContext.SaveChangesAsync(cancellationToken);
        await RecordAuditAsync(
            tenantId,
            actorUserAccountId,
            "booking.request.declined",
            $$"""{"tenantId":"{{tenantId}}","appointmentRequestId":"{{request.Id}}"}""",
            now,
            cancellationToken);

        return AppointmentRequestDecisionResult.Success(appointmentId: null);
    }

    private Task RecordAuditAsync(
        Guid tenantId,
        Guid actorUserAccountId,
        string action,
        string detailsJson,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        return auditLogRecorder.RecordAsync(
            new AuditLogRecord(
                tenantId,
                actorUserAccountId,
                action,
                detailsJson,
                occurredAtUtc),
            cancellationToken);
    }
}
