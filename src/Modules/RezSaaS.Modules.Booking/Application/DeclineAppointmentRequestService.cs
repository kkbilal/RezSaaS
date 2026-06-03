using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Auditing;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Booking.Domain;
using RezSaaS.Modules.Booking.Infrastructure.Persistence;

namespace RezSaaS.Modules.Booking.Application;

public sealed class DeclineAppointmentRequestService
{
    private const string AlreadyClosed = "APPOINTMENT_REQUEST_ALREADY_CLOSED";
    private const string DeclineOperation = "business.appointment-request.decline";
    private const string Expired = "APPOINTMENT_REQUEST_EXPIRED";
    private const string IdempotencyKeyReused = "IDEMPOTENCY_KEY_REUSED";
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
        return await DeclineAsync(
            appointmentRequestId,
            actorUserAccountId,
            idempotency: null,
            cancellationToken);
    }

    public async Task<AppointmentRequestDecisionResult> DeclineAsync(
        Guid appointmentRequestId,
        Guid actorUserAccountId,
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
                actorUserAccountId,
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
            cancellationToken);

        if (request is null)
        {
            return AppointmentRequestDecisionResult.Failure(NotFound);
        }

        if (request.Status == AppointmentRequestStatus.Declined)
        {
            AddIdempotencyRecord(
                tenantId,
                actorUserAccountId,
                idempotency,
                request.Id,
                "Declined",
                now);
            AppointmentRequestDecisionResult replayResult = AppointmentRequestDecisionResult.Success(appointmentId: null);
            AppointmentRequestDecisionResult? duplicateReplayResult =
                await SaveDecisionAsync(
                    transaction,
                    tenantId,
                    actorUserAccountId,
                    idempotency,
                    cancellationToken);

            return duplicateReplayResult ?? replayResult;
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
                actorUserAccountId,
                "booking.request.expired_on_decline",
                $$"""{"tenantId":"{{tenantId}}","appointmentRequestId":"{{request.Id}}"}""",
                now,
                cancellationToken);

            return AppointmentRequestDecisionResult.Failure(Expired);
        }

        request.Decline();
        AddIdempotencyRecord(
            tenantId,
            actorUserAccountId,
            idempotency,
            request.Id,
            "Declined",
            now);
        AppointmentRequestDecisionResult result = AppointmentRequestDecisionResult.Success(appointmentId: null);
        AppointmentRequestDecisionResult? duplicateReplay =
            await SaveDecisionAsync(
                transaction,
                tenantId,
                actorUserAccountId,
                idempotency,
                cancellationToken);

        if (duplicateReplay is not null)
        {
            return duplicateReplay;
        }

        await RecordAuditAsync(
            tenantId,
            actorUserAccountId,
            "booking.request.declined",
            $$"""{"tenantId":"{{tenantId}}","appointmentRequestId":"{{request.Id}}"}""",
            now,
            cancellationToken);

        return result;
    }

    private async Task<AppointmentRequestDecisionResult?> SaveDecisionAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        Guid tenantId,
        Guid actorUserAccountId,
        BookingIdempotencyContext? idempotency,
        CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return null;
        }
        catch (DbUpdateException) when (idempotency is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
            DetachChangedEntities();

            AppointmentRequestDecisionResult? replayedResult =
                await TryReplayDecisionAsync(
                    tenantId,
                    actorUserAccountId,
                    idempotency,
                    cancellationToken);

            if (replayedResult is not null)
            {
                return replayedResult;
            }

            throw;
        }
    }

    private async Task<AppointmentRequest?> LockAppointmentRequestAsync(
        Guid tenantId,
        Guid appointmentRequestId,
        CancellationToken cancellationToken)
    {
        return await dbContext.AppointmentRequests
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
                    && entity.Operation == DeclineOperation
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
            appointmentId: null,
            existingRecord.AffectedRequests);
    }

    private void AddIdempotencyRecord(
        Guid tenantId,
        Guid actorUserAccountId,
        BookingIdempotencyContext? idempotency,
        Guid appointmentRequestId,
        string status,
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
                DeclineOperation,
                idempotency.KeyHash,
                idempotency.RequestHash,
                responseResourceId: null,
                relatedResourceId: appointmentRequestId,
                responseStatus: status,
                affectedRequests: 0,
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
