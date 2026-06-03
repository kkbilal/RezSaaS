using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Auditing;
using RezSaaS.BuildingBlocks.Messaging;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Booking.Domain;
using RezSaaS.Modules.Booking.Infrastructure.Persistence;

namespace RezSaaS.Modules.Booking.Application;

public sealed class ApproveAppointmentRequestService
{
    private const string AlreadyClosed = "APPOINTMENT_REQUEST_ALREADY_CLOSED";
    private const string ApproveOperation = "business.appointment-request.approve";
    private const string Conflict = "APPOINTMENT_CONFLICT";
    private const string Expired = "APPOINTMENT_REQUEST_EXPIRED";
    private const string IdempotencyKeyReused = "IDEMPOTENCY_KEY_REUSED";
    private const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    private const string NotFound = "APPOINTMENT_REQUEST_NOT_FOUND";

    private readonly BookingDbContext dbContext;
    private readonly IAuditLogRecorder auditLogRecorder;
    private readonly ITransactionalMessageOutbox messageOutbox;
    private readonly ITenantContextAccessor tenantContextAccessor;
    private readonly TimeProvider timeProvider;

    public ApproveAppointmentRequestService(
        BookingDbContext dbContext,
        IAuditLogRecorder auditLogRecorder,
        ITransactionalMessageOutbox messageOutbox,
        ITenantContextAccessor tenantContextAccessor,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.auditLogRecorder = auditLogRecorder;
        this.messageOutbox = messageOutbox;
        this.tenantContextAccessor = tenantContextAccessor;
        this.timeProvider = timeProvider;
    }

    public async Task<AppointmentRequestDecisionResult> ApproveAsync(
        Guid appointmentRequestId,
        Guid approverUserAccountId,
        CancellationToken cancellationToken = default)
    {
        return await ApproveAsync(
            appointmentRequestId,
            approverUserAccountId,
            idempotency: null,
            cancellationToken);
    }

    public async Task<AppointmentRequestDecisionResult> ApproveAsync(
        Guid appointmentRequestId,
        Guid approverUserAccountId,
        BookingIdempotencyContext? idempotency,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return AppointmentRequestDecisionResult.Failure(MissingTenantContext);
        }

        AppointmentRequestDecisionResult? idempotentResult =
            await TryReplayDecisionAsync(
                tenantId,
                approverUserAccountId,
                idempotency,
                cancellationToken);

        if (idempotentResult is not null)
        {
            return idempotentResult;
        }

        DateTimeOffset now = timeProvider.GetUtcNow();

        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        AppointmentRequest? request = await LockAppointmentRequestAsync(
            tenantId,
            appointmentRequestId,
            includeLines: true,
            cancellationToken);

        if (request is null)
        {
            return AppointmentRequestDecisionResult.Failure(NotFound);
        }

        if (request.Status == AppointmentRequestStatus.Approved)
        {
            Guid? existingAppointmentId = await dbContext.Appointments
                .Where(entity => entity.AppointmentRequestId == request.Id)
                .Select(entity => (Guid?)entity.Id)
                .SingleOrDefaultAsync(cancellationToken);

            AddIdempotencyRecord(
                tenantId,
                approverUserAccountId,
                idempotency,
                existingAppointmentId,
                request.Id,
                "Approved",
                affectedRequests: 0,
                now);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException) when (idempotency is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
                DetachChangedEntities();

                AppointmentRequestDecisionResult? replayedResult =
                    await TryReplayDecisionAsync(
                        tenantId,
                        approverUserAccountId,
                        idempotency,
                        cancellationToken);

                if (replayedResult is not null)
                {
                    return replayedResult;
                }

                throw;
            }

            return AppointmentRequestDecisionResult.Success(existingAppointmentId);
        }

        if (request.Status != AppointmentRequestStatus.PendingApproval)
        {
            return AppointmentRequestDecisionResult.Failure(AlreadyClosed);
        }

        if (request.ExpiresAtUtc <= now)
        {
            request.Expire();
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            await RecordAuditAsync(
                tenantId,
                approverUserAccountId,
                "booking.request.expired_on_approval",
                $$"""{"tenantId":"{{tenantId}}","appointmentRequestId":"{{request.Id}}"}""",
                now,
                cancellationToken);

            return AppointmentRequestDecisionResult.Failure(Expired);
        }

        Appointment appointment = Appointment.CreateConfirmed(
            request.TenantId,
            request.Id,
            request.CustomerUserAccountId,
            request.BranchId,
            request.StaffMemberId,
            request.ResourceId,
            request.RequestedStartUtc,
            request.RequestedEndUtc,
            now);

        foreach (AppointmentRequestLine line in request.Lines)
        {
            appointment.AddLine(
                line.ServiceVariantId,
                line.ServiceNameSnapshot,
                line.DurationMinutes,
                line.PriceAmount,
                line.CurrencyCode);
        }

        dbContext.Appointments.Add(appointment);
        request.Approve();

        List<AppointmentRequest> supersededRequests = await dbContext.AppointmentRequests
            .Where(entity => entity.Id != request.Id)
            .Where(entity => entity.Status == AppointmentRequestStatus.PendingApproval)
            .Where(entity => entity.RequestedStartUtc < request.RequestedEndUtc
                && entity.RequestedEndUtc > request.RequestedStartUtc)
            .Where(entity => entity.StaffMemberId == request.StaffMemberId
                || entity.ResourceId == request.ResourceId)
            .ToListAsync(cancellationToken);

        foreach (AppointmentRequest supersededRequest in supersededRequests)
        {
            supersededRequest.Supersede();
        }

        AddIdempotencyRecord(
            tenantId,
            approverUserAccountId,
            idempotency,
            appointment.Id,
            request.Id,
            "Approved",
            supersededRequests.Count,
            now);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            DetachChangedEntities();

            AppointmentRequestDecisionResult? replayedResult =
                await TryReplayDecisionAsync(
                    tenantId,
                    approverUserAccountId,
                    idempotency,
                    cancellationToken);

            if (replayedResult is not null)
            {
                return replayedResult;
            }

            return AppointmentRequestDecisionResult.Failure(Conflict);
        }

        await RecordAuditAsync(
            tenantId,
            approverUserAccountId,
            "booking.request.approved",
            $$"""{"tenantId":"{{tenantId}}","appointmentRequestId":"{{request.Id}}","appointmentId":"{{appointment.Id}}","supersededRequests":{{supersededRequests.Count}}}""",
            now,
            cancellationToken);

        await messageOutbox.EnqueueAsync(
            new TransactionalMessageEnvelope(
                tenantId,
                TransactionalMessageChannel.Email,
                $"user:{request.CustomerUserAccountId}",
                "booking.approved",
                $$"""{"appointmentId":"{{appointment.Id}}","appointmentRequestId":"{{request.Id}}"}""",
                now),
            cancellationToken);

        return AppointmentRequestDecisionResult.Success(
            appointment.Id,
            supersededRequests.Count);
    }

    private async Task<AppointmentRequest?> LockAppointmentRequestAsync(
        Guid tenantId,
        Guid appointmentRequestId,
        bool includeLines,
        CancellationToken cancellationToken)
    {
        AppointmentRequest? request = await dbContext.AppointmentRequests
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM booking."AppointmentRequests"
                WHERE "TenantId" = {tenantId}
                    AND "Id" = {appointmentRequestId}
                FOR UPDATE
                """)
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(cancellationToken);

        if (request is not null && includeLines)
        {
            await dbContext.Entry(request)
                .Collection(entity => entity.Lines)
                .LoadAsync(cancellationToken);
        }

        return request;
    }

    private async Task<AppointmentRequestDecisionResult?> TryReplayDecisionAsync(
        Guid tenantId,
        Guid actorUserAccountId,
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
                    && entity.ActorUserAccountId == actorUserAccountId
                    && entity.Operation == ApproveOperation
                    && entity.KeyHash == idempotency.KeyHash,
                cancellationToken);

        if (existingRecord is null)
        {
            return null;
        }

        if (existingRecord.RequestHash != idempotency.RequestHash)
        {
            return AppointmentRequestDecisionResult.Failure(IdempotencyKeyReused);
        }

        return AppointmentRequestDecisionResult.Success(
            existingRecord.ResponseResourceId,
            existingRecord.AffectedRequests);
    }

    private void AddIdempotencyRecord(
        Guid tenantId,
        Guid actorUserAccountId,
        BookingIdempotencyContext? idempotency,
        Guid? appointmentId,
        Guid appointmentRequestId,
        string status,
        int affectedRequests,
        DateTimeOffset now)
    {
        if (idempotency is null)
        {
            return;
        }

        dbContext.IdempotencyRecords.Add(
            BookingIdempotencyRecord.Create(
                tenantId,
                actorUserAccountId,
                ApproveOperation,
                idempotency.KeyHash,
                idempotency.RequestHash,
                appointmentId,
                appointmentRequestId,
                status,
                affectedRequests,
                responseExpiresAtUtc: null,
                createdAtUtc: now));
    }

    private void DetachChangedEntities()
    {
        foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry in dbContext.ChangeTracker
            .Entries()
            .Where(entity => entity.State is EntityState.Added or EntityState.Modified))
        {
            entry.State = EntityState.Detached;
        }
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
