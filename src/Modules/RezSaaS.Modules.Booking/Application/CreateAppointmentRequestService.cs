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
    private const string InvalidTimeRange = "INVALID_TIME_RANGE";
    private const string LinesRequired = "APPOINTMENT_REQUEST_LINES_REQUIRED";
    private const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    private const string PendingLimitExceeded = "BOOKING_PENDING_LIMIT_EXCEEDED";
    private const string RequestTooSoon = "APPOINTMENT_REQUEST_TOO_SOON";

    private readonly IAbuseEventRecorder abuseEventRecorder;
    private readonly BookingDbContext dbContext;
    private readonly IOptions<BookingSecurityOptions> options;
    private readonly ITenantContextAccessor tenantContextAccessor;
    private readonly TimeProvider timeProvider;

    public CreateAppointmentRequestService(
        BookingDbContext dbContext,
        IAbuseEventRecorder abuseEventRecorder,
        IOptions<BookingSecurityOptions> options,
        ITenantContextAccessor tenantContextAccessor,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.abuseEventRecorder = abuseEventRecorder;
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
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreateAppointmentRequestResult.Success(
            request.Id,
            request.ExpiresAtUtc);
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
