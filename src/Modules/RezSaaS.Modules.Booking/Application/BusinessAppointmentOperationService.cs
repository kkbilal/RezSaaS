using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Auditing;
using RezSaaS.BuildingBlocks.Messaging;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Booking.Domain;
using RezSaaS.Modules.Booking.Infrastructure.Persistence;

namespace RezSaaS.Modules.Booking.Application;

public sealed class BusinessAppointmentOperationService
{
    private const string AlreadyClosed = "APPOINTMENT_ALREADY_CLOSED";
    private const string CompleteTooEarly = "APPOINTMENT_COMPLETE_TOO_EARLY";
    private const string Conflict = "APPOINTMENT_CONFLICT";
    private const string IdempotencyKeyReused = "IDEMPOTENCY_KEY_REUSED";
    private const string InvalidTimeRange = "APPOINTMENT_INVALID_TIME_RANGE";
    private const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    private const string NotFound = "APPOINTMENT_NOT_FOUND";
    private const string NoShowTooEarly = "APPOINTMENT_NO_SHOW_TOO_EARLY";

    private readonly IAuditLogRecorder auditLogRecorder;
    private readonly BookingDbContext dbContext;
    private readonly ITransactionalMessageOutbox messageOutbox;
    private readonly ITenantContextAccessor tenantContextAccessor;
    private readonly TimeProvider timeProvider;

    public BusinessAppointmentOperationService(
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

    public async Task<AppointmentOperationResult> CancelAsync(
        Guid appointmentId,
        Guid actorUserAccountId,
        string reason,
        BookingIdempotencyContext? idempotency = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            appointmentId,
            actorUserAccountId,
            operation: "business.appointment.cancel",
            idempotency,
            (appointment, _, now, _) =>
            {
                if (appointment.Status == AppointmentStatus.Cancelled)
                {
                    return Task.FromResult(AppointmentOperationResult.Success(
                        appointment.Id,
                        AppointmentStatus.Cancelled.ToString()));
                }

                if (appointment.Status != AppointmentStatus.Confirmed)
                {
                    return Task.FromResult(AppointmentOperationResult.Failure(AlreadyClosed));
                }

                appointment.Cancel(actorUserAccountId, reason, now);

                return Task.FromResult(AppointmentOperationResult.Success(
                    appointment.Id,
                    appointment.Status.ToString()));
            },
            cancellationToken);
    }

    public async Task<AppointmentOperationResult> CompleteAsync(
        Guid appointmentId,
        Guid actorUserAccountId,
        string? note,
        BookingIdempotencyContext? idempotency = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            appointmentId,
            actorUserAccountId,
            operation: "business.appointment.complete",
            idempotency,
            (appointment, _, now, _) =>
            {
                if (appointment.Status == AppointmentStatus.Completed)
                {
                    return Task.FromResult(AppointmentOperationResult.Success(
                        appointment.Id,
                        AppointmentStatus.Completed.ToString()));
                }

                if (appointment.Status != AppointmentStatus.Confirmed)
                {
                    return Task.FromResult(AppointmentOperationResult.Failure(AlreadyClosed));
                }

                if (appointment.EndUtc > now)
                {
                    return Task.FromResult(AppointmentOperationResult.Failure(CompleteTooEarly));
                }

                appointment.Complete(actorUserAccountId, note, now);

                return Task.FromResult(AppointmentOperationResult.Success(
                    appointment.Id,
                    appointment.Status.ToString()));
            },
            cancellationToken);
    }

    public async Task<AppointmentOperationResult> MarkNoShowAsync(
        Guid appointmentId,
        Guid actorUserAccountId,
        string reason,
        BookingIdempotencyContext? idempotency = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            appointmentId,
            actorUserAccountId,
            operation: "business.appointment.no-show",
            idempotency,
            (appointment, _, now, _) =>
            {
                if (appointment.Status == AppointmentStatus.NoShow)
                {
                    return Task.FromResult(AppointmentOperationResult.Success(
                        appointment.Id,
                        AppointmentStatus.NoShow.ToString()));
                }

                if (appointment.Status != AppointmentStatus.Confirmed)
                {
                    return Task.FromResult(AppointmentOperationResult.Failure(AlreadyClosed));
                }

                if (appointment.StartUtc > now)
                {
                    return Task.FromResult(AppointmentOperationResult.Failure(NoShowTooEarly));
                }

                appointment.MarkNoShow(actorUserAccountId, reason, now);

                return Task.FromResult(AppointmentOperationResult.Success(
                    appointment.Id,
                    appointment.Status.ToString()));
            },
            cancellationToken);
    }

    public async Task<AppointmentOperationResult> UpdateNoteAsync(
        Guid appointmentId,
        Guid actorUserAccountId,
        string? note,
        BookingIdempotencyContext? idempotency = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            appointmentId,
            actorUserAccountId,
            operation: "business.appointment.note",
            idempotency,
            (appointment, _, now, _) =>
            {
                appointment.UpdateBusinessNote(actorUserAccountId, note, now);

                return Task.FromResult(AppointmentOperationResult.Success(
                    appointment.Id,
                    appointment.Status.ToString()));
            },
            cancellationToken);
    }

    public async Task<AppointmentOperationResult> RebookAsync(
        Guid appointmentId,
        Guid actorUserAccountId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        Guid? staffMemberId,
        Guid? resourceId,
        string reason,
        BookingIdempotencyContext? idempotency = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            appointmentId,
            actorUserAccountId,
            operation: "business.appointment.rebook",
            idempotency,
            async (appointment, tenantId, now, token) =>
            {
                if (appointment.Status == AppointmentStatus.Rebooked
                    && appointment.RebookedToAppointmentId is not null)
                {
                    return AppointmentOperationResult.Success(
                        appointment.Id,
                        AppointmentStatus.Rebooked.ToString(),
                        appointment.RebookedToAppointmentId);
                }

                if (appointment.Status != AppointmentStatus.Confirmed)
                {
                    return AppointmentOperationResult.Failure(AlreadyClosed);
                }

                if (endUtc <= startUtc)
                {
                    return AppointmentOperationResult.Failure(InvalidTimeRange);
                }

                Guid targetStaffMemberId = staffMemberId ?? appointment.StaffMemberId;
                Guid targetResourceId = resourceId ?? appointment.ResourceId;

                bool hasConflict = await HasConfirmedConflictAsync(
                    tenantId,
                    appointment.Id,
                    targetStaffMemberId,
                    targetResourceId,
                    startUtc,
                    endUtc,
                    token);

                if (hasConflict)
                {
                    return AppointmentOperationResult.Failure(Conflict);
                }

                Appointment rebookedAppointment = Appointment.CreateRebookedConfirmed(
                    tenantId,
                    appointment.AppointmentRequestId,
                    appointment.Id,
                    appointment.CustomerUserAccountId,
                    appointment.BranchId,
                    targetStaffMemberId,
                    targetResourceId,
                    startUtc,
                    endUtc,
                    now);

                foreach (AppointmentLine line in appointment.Lines)
                {
                    rebookedAppointment.AddLine(
                        line.ServiceVariantId,
                        line.ServiceNameSnapshot,
                        line.DurationMinutes,
                        line.PriceAmount,
                        line.CurrencyCode);
                }

                dbContext.Appointments.Add(rebookedAppointment);
                appointment.MarkRebooked(
                    rebookedAppointment.Id,
                    actorUserAccountId,
                    reason,
                    now);

                return AppointmentOperationResult.Success(
                    appointment.Id,
                    appointment.Status.ToString(),
                    rebookedAppointment.Id);
            },
            cancellationToken);
    }

    private async Task<AppointmentOperationResult> ExecuteAsync(
        Guid appointmentId,
        Guid actorUserAccountId,
        string operation,
        BookingIdempotencyContext? idempotency,
        Func<Appointment, Guid, DateTimeOffset, CancellationToken, Task<AppointmentOperationResult>> action,
        CancellationToken cancellationToken)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return AppointmentOperationResult.Failure(MissingTenantContext);
        }

        AppointmentOperationResult? replayedResult = await TryReplayAsync(
            tenantId,
            actorUserAccountId,
            operation,
            idempotency,
            cancellationToken);

        if (replayedResult is not null)
        {
            return replayedResult;
        }

        DateTimeOffset now = timeProvider.GetUtcNow();

        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        Appointment? appointment = await LockAppointmentAsync(
            tenantId,
            appointmentId,
            includeLines: operation == "business.appointment.rebook",
            cancellationToken);

        if (appointment is null)
        {
            return AppointmentOperationResult.Failure(NotFound);
        }

        AppointmentOperationResult result = await action(
            appointment,
            tenantId,
            now,
            cancellationToken);

        if (!result.Succeeded)
        {
            return result;
        }

        AddIdempotencyRecord(
            tenantId,
            actorUserAccountId,
            operation,
            idempotency,
            result.AppointmentId,
            result.RelatedAppointmentId,
            result.Status!,
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

            AppointmentOperationResult? duplicateReplay = await TryReplayAsync(
                tenantId,
                actorUserAccountId,
                operation,
                idempotency,
                cancellationToken);

            if (duplicateReplay is not null)
            {
                return duplicateReplay;
            }

            throw;
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            DetachChangedEntities();

            return AppointmentOperationResult.Failure(Conflict);
        }

        await auditLogRecorder.RecordAsync(
            new AuditLogRecord(
                tenantId,
                actorUserAccountId,
                $"booking.appointment.{operation.Split('.').Last()}",
                $$"""{"tenantId":"{{tenantId}}","appointmentId":"{{appointmentId}}","relatedAppointmentId":"{{result.RelatedAppointmentId}}","status":"{{result.Status}}"}""",
                now),
            cancellationToken);

        await EnqueueOperationMessageAsync(
            tenantId,
            appointment.CustomerUserAccountId,
            operation,
            result.AppointmentId!.Value,
            result.RelatedAppointmentId,
            now,
            cancellationToken);

        return result;
    }

    private async Task<Appointment?> LockAppointmentAsync(
        Guid tenantId,
        Guid appointmentId,
        bool includeLines,
        CancellationToken cancellationToken)
    {
        Appointment? appointment = await dbContext.Appointments
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM booking."Appointments"
                WHERE "TenantId" = {tenantId}
                    AND "Id" = {appointmentId}
                FOR UPDATE
                """)
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(cancellationToken);

        if (appointment is not null && includeLines)
        {
            await dbContext.Entry(appointment)
                .Collection(entity => entity.Lines)
                .LoadAsync(cancellationToken);
        }

        return appointment;
    }

    private async Task<bool> HasConfirmedConflictAsync(
        Guid tenantId,
        Guid ignoredAppointmentId,
        Guid staffMemberId,
        Guid resourceId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken)
    {
        return await dbContext.Appointments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(entity => entity.TenantId == tenantId
                && entity.Id != ignoredAppointmentId
                && entity.Status == AppointmentStatus.Confirmed
                && entity.StartUtc < endUtc
                && entity.EndUtc > startUtc)
            .AnyAsync(
                entity => entity.StaffMemberId == staffMemberId
                    || entity.ResourceId == resourceId,
                cancellationToken);
    }

    private async Task<AppointmentOperationResult?> TryReplayAsync(
        Guid tenantId,
        Guid actorUserAccountId,
        string operation,
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
                    && entity.Operation == operation
                    && entity.KeyHash == idempotency.KeyHash,
                cancellationToken);

        if (existingRecord is null)
        {
            return null;
        }

        if (existingRecord.RequestHash != idempotency.RequestHash
            || existingRecord.ResponseResourceId is null)
        {
            return AppointmentOperationResult.Failure(IdempotencyKeyReused);
        }

        return AppointmentOperationResult.Success(
            existingRecord.ResponseResourceId.Value,
            existingRecord.ResponseStatus,
            existingRecord.RelatedResourceId);
    }

    private void AddIdempotencyRecord(
        Guid tenantId,
        Guid actorUserAccountId,
        string operation,
        BookingIdempotencyContext? idempotency,
        Guid? appointmentId,
        Guid? relatedAppointmentId,
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
                operation,
                idempotency.KeyHash,
                idempotency.RequestHash,
                appointmentId,
                relatedAppointmentId,
                status,
                affectedRequests: 0,
                responseExpiresAtUtc: null,
                createdAtUtc: now));
    }

    private Task EnqueueOperationMessageAsync(
        Guid tenantId,
        Guid customerUserAccountId,
        string operation,
        Guid appointmentId,
        Guid? relatedAppointmentId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        string? messageType = operation switch
        {
            "business.appointment.cancel" => "booking.appointment.cancelled_by_business",
            "business.appointment.no-show" => "booking.appointment.no_show_marked",
            "business.appointment.rebook" => "booking.appointment.rebooked",
            _ => null,
        };

        if (messageType is null)
        {
            return Task.CompletedTask;
        }

        string payloadJson = relatedAppointmentId is null
            ? $$"""{"appointmentId":"{{appointmentId}}"}"""
            : $$"""{"appointmentId":"{{appointmentId}}","newAppointmentId":"{{relatedAppointmentId}}"}""";

        return messageOutbox.EnqueueAsync(
            new TransactionalMessageEnvelope(
                tenantId,
                TransactionalMessageChannel.Email,
                $"user:{customerUserAccountId}",
                messageType,
                payloadJson,
                now),
            cancellationToken);
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
}
