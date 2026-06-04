using System.Security.Claims;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Admin.Application;
using RezSaaS.Modules.Admin.Domain;
using RezSaaS.Modules.Booking.Application;
using RezSaaS.Modules.TenantManagement.Application;

namespace RezSaaS.Api.Business;

public sealed class BusinessAbuseReportComposer
{
    private const string Forbidden = "BUSINESS_ABUSE_REPORT_FORBIDDEN";
    private const string InvalidReasonCode = "BUSINESS_ABUSE_REPORT_INVALID_REASON_CODE";
    private const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    private const string NotFound = "BUSINESS_ABUSE_REPORT_APPOINTMENT_REQUEST_NOT_FOUND";
    private const string Unauthorized = "BUSINESS_ABUSE_REPORT_UNAUTHORIZED";

    private readonly TenantBookingAuthorizationService authorizationService;
    private readonly CreateBusinessAbuseReportService createService;
    private readonly BusinessAppointmentRequestQueryService bookingQueryService;
    private readonly AbuseReportQueryService reportQueryService;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public BusinessAbuseReportComposer(
        TenantBookingAuthorizationService authorizationService,
        BusinessAppointmentRequestQueryService bookingQueryService,
        CreateBusinessAbuseReportService createService,
        AbuseReportQueryService reportQueryService,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.authorizationService = authorizationService;
        this.bookingQueryService = bookingQueryService;
        this.createService = createService;
        this.reportQueryService = reportQueryService;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<BusinessAbuseReportAccessResult> CreateAsync(
        Guid appointmentRequestId,
        BusinessAbuseReportRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid actorUserAccountId))
        {
            return BusinessAbuseReportAccessResult.Failure(
                BusinessAbuseReportOutcome.Unauthorized,
                Unauthorized);
        }

        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return BusinessAbuseReportAccessResult.Failure(
                BusinessAbuseReportOutcome.BadRequest,
                MissingTenantContext);
        }

        if (!TryParseReasonCode(request.ReasonCode, out AbuseReportReasonCode reasonCode))
        {
            return BusinessAbuseReportAccessResult.Failure(
                BusinessAbuseReportOutcome.BadRequest,
                InvalidReasonCode);
        }

        BusinessAppointmentRequestAuthorizationContext? authorizationContext =
            await bookingQueryService.GetAuthorizationContextAsync(
                appointmentRequestId,
                cancellationToken);

        if (authorizationContext is null)
        {
            return BusinessAbuseReportAccessResult.Failure(
                BusinessAbuseReportOutcome.NotFound,
                NotFound);
        }

        if (!await authorizationService.CanManageAppointmentRequestsAsync(
            tenantId,
            actorUserAccountId,
            authorizationContext.BranchId,
            cancellationToken))
        {
            return BusinessAbuseReportAccessResult.Failure(
                BusinessAbuseReportOutcome.Forbidden,
                Forbidden);
        }

        BusinessAbuseReportCommandResult result =
            await createService.CreateAsync(
                new CreateBusinessAbuseReportCommand(
                    tenantId,
                    authorizationContext.BranchId,
                    appointmentRequestId,
                    authorizationContext.CustomerUserAccountId,
                    actorUserAccountId,
                    reasonCode,
                    request.Note),
                cancellationToken);

        if (!result.Succeeded)
        {
            return MapFailure(result.ErrorCode!);
        }

        BusinessAbuseReportView? report =
            await reportQueryService.GetByIdAsync(result.ReportId!.Value, cancellationToken);

        return report is null
            ? BusinessAbuseReportAccessResult.Failure(
                BusinessAbuseReportOutcome.Unprocessable,
                "BUSINESS_ABUSE_REPORT_NOT_FOUND")
            : BusinessAbuseReportAccessResult.Success(
                new BusinessAbuseReportResponse(
                    report.Id,
                    report.Status.ToString(),
                    report.CreatedAtUtc),
                result.Created);
    }

    private static BusinessAbuseReportAccessResult MapFailure(string errorCode)
    {
        BusinessAbuseReportOutcome outcome = errorCode switch
        {
            "BUSINESS_ABUSE_REPORT_INVALID" => BusinessAbuseReportOutcome.BadRequest,
            "BUSINESS_ABUSE_REPORT_DAILY_LIMIT_EXCEEDED" => BusinessAbuseReportOutcome.TooManyRequests,
            _ => BusinessAbuseReportOutcome.Unprocessable,
        };

        return BusinessAbuseReportAccessResult.Failure(outcome, errorCode);
    }

    private static bool TryGetUserAccountId(
        ClaimsPrincipal user,
        out Guid userAccountId)
    {
        string? rawUserId = user.FindFirstValue("sub")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(rawUserId, out userAccountId);
    }

    private static bool TryParseReasonCode(
        string reasonCode,
        out AbuseReportReasonCode parsedReasonCode)
    {
        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            parsedReasonCode = default;
            return false;
        }

        return Enum.TryParse(reasonCode, ignoreCase: true, out parsedReasonCode)
            && Enum.IsDefined(parsedReasonCode);
    }
}
