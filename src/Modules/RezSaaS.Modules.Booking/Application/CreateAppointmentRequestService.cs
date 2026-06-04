using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RezSaaS.BuildingBlocks.Abuse;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Booking.Domain;
using RezSaaS.Modules.Booking.Infrastructure.Persistence;

namespace RezSaaS.Modules.Booking.Application;

public sealed class CreateAppointmentRequestService
{
    private const string DailyLimitExceeded = "BOOKING_DAILY_LIMIT_EXCEEDED";
    private const string IdempotencyKeyReused = "IDEMPOTENCY_KEY_REUSED";
    private const string InvalidTimeRange = "INVALID_TIME_RANGE";
    private const string LinesRequired = "APPOINTMENT_REQUEST_LINES_REQUIRED";
    private const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    private const string PendingLimitExceeded = "BOOKING_PENDING_LIMIT_EXCEEDED";
    private const string RequestTooSoon = "APPOINTMENT_REQUEST_TOO_SOON";
    private const string UserSanctioned = "BOOKING_USER_SANCTIONED";
    private const string CreateOperation = "public.appointment-request.create";

    private readonly IAbuseEventRecorder abuseEventRecorder;
    private readonly BookingDbContext dbContext;
    private readonly IOptions<BookingSecurityOptions> options;
    private readonly ITenantContextAccessor tenantContextAccessor;
    private readonly TimeProvider timeProvider;
    private readonly IUserBookingRestrictionEvaluator userBookingRestrictionEvaluator;

    public CreateAppointmentRequestService(
        BookingDbContext dbContext,
        IAbuseEventRecorder abuseEventRecorder,
        IUserBookingRestrictionEvaluator userBookingRestrictionEvaluator,
        IOptions<BookingSecurityOptions> options,
        ITenantContextAccessor tenantContextAccessor,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.abuseEventRecorder = abuseEventRecorder;
        this.userBookingRestrictionEvaluator = userBookingRestrictionEvaluator;
        this.options = options;
        this.tenantContextAccessor = tenantContextAccessor;
        this.timeProvider = timeProvider;
    }

    public async Task<CreateAppointmentRequestResult> CreateAsync(
        CreateAppointmentRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return CreateAppointmentRequestResult.Failure(MissingTenantContext);
        }

        BookingSecurityOptions securityOptions = options.Value;
        DateTimeOffset now = timeProvider.GetUtcNow();
        TimeSpan responseBuffer = command.ResponseBuffer ?? securityOptions.DefaultResponseBuffer;
        CreateAppointmentRequestResult? idempotentResult =
            await TryReplayCreateAsync(
                tenantId,
                command.CustomerUserAccountId,
                command.Idempotency,
                cancellationToken);

        if (idempotentResult is not null)
        {
            return idempotentResult;
        }

        UserBookingRestriction restriction =
            await userBookingRestrictionEvaluator.EvaluateAsync(
                command.CustomerUserAccountId,
                now,
                cancellationToken);

        if (restriction.IsRestricted)
        {
            return CreateAppointmentRequestResult.Failure(UserSanctioned);
        }

        if (command.RequestedEndUtc <= command.RequestedStartUtc)
        {
            return CreateAppointmentRequestResult.Failure(InvalidTimeRange);
        }

        if (command.RequestedStartUtc <= now.Add(responseBuffer))
        {
            return CreateAppointmentRequestResult.Failure(RequestTooSoon);
        }

        if (command.Lines.Count == 0)
        {
            return CreateAppointmentRequestResult.Failure(LinesRequired);
        }

        int pendingRequestCount = await dbContext.AppointmentRequests.CountAsync(
            entity => entity.CustomerUserAccountId == command.CustomerUserAccountId
                && entity.Status == AppointmentRequestStatus.PendingApproval,
            cancellationToken);

        if (pendingRequestCount >= securityOptions.MaxConcurrentPendingRequestsPerUser)
        {
            await RecordLimitAbuseAsync(
                tenantId,
                command.CustomerUserAccountId,
                "booking.pending_limit_exceeded",
                $$"""{"tenantId":"{{tenantId}}","current":{{pendingRequestCount}},"max":{{securityOptions.MaxConcurrentPendingRequestsPerUser}}}""",
                now,
                cancellationToken);

            return CreateAppointmentRequestResult.Failure(PendingLimitExceeded);
        }

        DateTimeOffset dayWindowStartUtc = now.AddDays(-1);
        int dailyRequestCount = await dbContext.AppointmentRequests.CountAsync(
            entity => entity.CustomerUserAccountId == command.CustomerUserAccountId
                && entity.CreatedAtUtc >= dayWindowStartUtc,
            cancellationToken);

        if (dailyRequestCount >= securityOptions.MaxRequestsPerUserPerDay)
        {
            await RecordLimitAbuseAsync(
                tenantId,
                command.CustomerUserAccountId,
                "booking.daily_limit_exceeded",
                $$"""{"tenantId":"{{tenantId}}","current":{{dailyRequestCount}},"max":{{securityOptions.MaxRequestsPerUserPerDay}}}""",
                now,
                cancellationToken);

            return CreateAppointmentRequestResult.Failure(DailyLimitExceeded);
        }

        AppointmentRequest request = AppointmentRequest.Create(
            tenantId,
            command.CustomerUserAccountId,
            command.BranchId,
            command.StaffMemberId,
            command.ResourceId,
            command.RequestedStartUtc,
            command.RequestedEndUtc,
            now,
            responseBuffer);

        foreach (AppointmentRequestLineInput line in command.Lines)
        {
            request.AddLine(
                line.ServiceVariantId,
                line.ServiceNameSnapshot,
                line.DurationMinutes,
                line.PriceAmount,
                line.CurrencyCode);
        }

        dbContext.AppointmentRequests.Add(request);
        if (command.Idempotency is not null)
        {
            dbContext.IdempotencyRecords.Add(
                BookingIdempotencyRecord.Create(
                    tenantId,
                    command.CustomerUserAccountId,
                    CreateOperation,
                    command.Idempotency.KeyHash,
                    command.Idempotency.RequestHash,
                    request.Id,
                    relatedResourceId: null,
                    responseStatus: request.Status.ToString(),
                    affectedRequests: 0,
                    responseExpiresAtUtc: request.ExpiresAtUtc,
                    createdAtUtc: now));
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (command.Idempotency is not null)
        {
            DetachAddedEntities();

            CreateAppointmentRequestResult? replayedResult =
                await TryReplayCreateAsync(
                    tenantId,
                    command.CustomerUserAccountId,
                    command.Idempotency,
                    cancellationToken);

            if (replayedResult is not null)
            {
                return replayedResult;
            }

            throw;
        }

        return CreateAppointmentRequestResult.Success(
            request.Id,
            request.ExpiresAtUtc);
    }

    private async Task<CreateAppointmentRequestResult?> TryReplayCreateAsync(
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
                    && entity.Operation == CreateOperation
                    && entity.KeyHash == idempotency.KeyHash,
                cancellationToken);

        if (existingRecord is null)
        {
            return null;
        }

        if (existingRecord.RequestHash != idempotency.RequestHash
            || existingRecord.ResponseResourceId is null
            || existingRecord.ResponseExpiresAtUtc is null)
        {
            return CreateAppointmentRequestResult.Failure(IdempotencyKeyReused);
        }

        return CreateAppointmentRequestResult.Success(
            existingRecord.ResponseResourceId.Value,
            existingRecord.ResponseExpiresAtUtc.Value,
            existingRecord.ResponseStatus,
            isReplay: true);
    }

    private void DetachAddedEntities()
    {
        foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry in dbContext.ChangeTracker
            .Entries()
            .Where(entity => entity.State == EntityState.Added))
        {
            entry.State = EntityState.Detached;
        }
    }

    private Task RecordLimitAbuseAsync(
        Guid tenantId,
        Guid userAccountId,
        string eventType,
        string detailsJson,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        return abuseEventRecorder.RecordAsync(
            new AbuseEventRecord(
                tenantId,
                userAccountId,
                eventType,
                AbuseEventSeverityLevel.Medium,
                detailsJson,
                occurredAtUtc),
            cancellationToken);
    }
}
