using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Auditing;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Booking.Domain;
using RezSaaS.Modules.Booking.Infrastructure.Persistence;

namespace RezSaaS.Modules.Booking.Application;

public sealed class CancelAppointmentRequestService
{
    private const string AlreadyClosed = "APPOINTMENT_REQUEST_ALREADY_CLOSED";
    private const string CancelOperation = "public.appointment-request.cancel";
    private const string IdempotencyKeyReused = "IDEMPOTENCY_KEY_REUSED";
    private const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    private const string NotFound = "APPOINTMENT_REQUEST_NOT_FOUND";

    private readonly IAuditLogRecorder auditLogRecorder;
    private readonly BookingDbContext dbContext;
    private readonly ITenantContextAccessor tenantContextAccessor;
    private readonly TimeProvider timeProvider;

    public CancelAppointmentRequestService(
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

    public async Task<AppointmentRequestDecisionResult> CancelAsync(
        Guid appointmentRequestId,
        Guid customerUserAccountId,
        BookingIdempotencyContext? idempotency = null,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return AppointmentRequestDecisionResult.Failure(MissingTenantContext);
        }

        AppointmentRequestDecisionResult? idempotentResult =
            await TryReplayCancelAsync(
                tenantId,
                customerUserAccountId,
                idempotency,
                cancellationToken);

        if (idempotentResult is not null)
        {
            return idempotentResult;
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        AppointmentRequest? request = await dbContext.AppointmentRequests
            .SingleOrDefaultAsync(
                entity => entity.Id == appointmentRequestId
                    && entity.CustomerUserAccountId == customerUserAccountId,
                cancellationToken);

        if (request is null)
        {
            return AppointmentRequestDecisionResult.Failure(NotFound);
        }

        if (request.Status == AppointmentRequestStatus.CancelledByCustomer)
        {
            await AddIdempotencyRecordAsync(
                tenantId,
                customerUserAccountId,
                request.Id,
                idempotency,
                now,
                cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            return AppointmentRequestDecisionResult.Success(request.Id);
        }

        if (request.Status != AppointmentRequestStatus.PendingApproval)
        {
            return AppointmentRequestDecisionResult.Failure(AlreadyClosed);
        }

        request.CancelByCustomer();
        await AddIdempotencyRecordAsync(
            tenantId,
            customerUserAccountId,
            request.Id,
            idempotency,
            now,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLogRecorder.RecordAsync(
            new AuditLogRecord(
                tenantId,
                customerUserAccountId,
                "booking.request.cancelled_by_customer",
                $$"""{"tenantId":"{{tenantId}}","appointmentRequestId":"{{request.Id}}"}""",
                now),
            cancellationToken);

        return AppointmentRequestDecisionResult.Success(request.Id);
    }

    private async Task<AppointmentRequestDecisionResult?> TryReplayCancelAsync(
        Guid tenantId,
        Guid customerUserAccountId,
        BookingIdempotencyContext? idempotency,
        CancellationToken cancellationToken)
    {
        if (idempotency is null)
        {
            return null;
        }

        BookingIdempotencyRecord? existingRecord = await dbContext.IdempotencyRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.TenantId == tenantId
                    && entity.ActorUserAccountId == customerUserAccountId
                    && entity.Operation == CancelOperation
                    && entity.KeyHash == idempotency.KeyHash,
                cancellationToken);

        if (existingRecord is null)
        {
            return null;
        }

        if (existingRecord.RequestHash != idempotency.RequestHash
            || existingRecord.ResponseResourceId is null)
        {
            return AppointmentRequestDecisionResult.Failure(IdempotencyKeyReused);
        }

        return AppointmentRequestDecisionResult.Success(existingRecord.ResponseResourceId);
    }

    private Task AddIdempotencyRecordAsync(
        Guid tenantId,
        Guid customerUserAccountId,
        Guid appointmentRequestId,
        BookingIdempotencyContext? idempotency,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (idempotency is null)
        {
            return Task.CompletedTask;
        }

        dbContext.IdempotencyRecords.Add(
            BookingIdempotencyRecord.Create(
                tenantId,
                customerUserAccountId,
                CancelOperation,
                idempotency.KeyHash,
                idempotency.RequestHash,
                appointmentRequestId,
                appointmentRequestId,
                AppointmentRequestStatus.CancelledByCustomer.ToString(),
                affectedRequests: 0,
                responseExpiresAtUtc: null,
                createdAtUtc: now));

        return Task.CompletedTask;
    }
}
