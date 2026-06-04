using System.Security.Claims;
using RezSaaS.Modules.Admin.Application;
using RezSaaS.Modules.Admin.Domain;

namespace RezSaaS.Api.Admin;

public sealed class AdminAbuseReportComposer
{
    private const string InvalidStatus = "ADMIN_ABUSE_REPORT_INVALID_STATUS";
    private const string NotFound = "ADMIN_ABUSE_REPORT_NOT_FOUND";
    private const string StrikeNotFound = "ADMIN_USER_STRIKE_NOT_FOUND";
    private const string Unauthorized = "ADMIN_ABUSE_REPORT_UNAUTHORIZED";

    private readonly AbuseReportQueryService queryService;
    private readonly ReviewBusinessAbuseReportService reviewService;
    private readonly RevokeUserStrikeService revokeStrikeService;

    public AdminAbuseReportComposer(
        AbuseReportQueryService queryService,
        ReviewBusinessAbuseReportService reviewService,
        RevokeUserStrikeService revokeStrikeService)
    {
        this.queryService = queryService;
        this.reviewService = reviewService;
        this.revokeStrikeService = revokeStrikeService;
    }

    public async Task<AdminAbuseReportAccessResult> GetReportsAsync(
        Guid? userAccountId,
        Guid? tenantId,
        string? status,
        int? take,
        CancellationToken cancellationToken = default)
    {
        if (!AbuseReportQueryService.IsValidStatusOrEmpty(status))
        {
            return AdminAbuseReportAccessResult.Failure(
                AdminAbuseOutcome.BadRequest,
                InvalidStatus);
        }

        IReadOnlyCollection<BusinessAbuseReportView> reports =
            await queryService.GetReportsAsync(
                new AbuseReportControlPlaneQuery(
                    userAccountId,
                    tenantId,
                    status,
                    take ?? 50),
                cancellationToken);

        return AdminAbuseReportAccessResult.Success(
            reports.Select(ToReportResponse).ToArray());
    }

    public async Task<AdminAbuseReportAccessResult> ReviewAsync(
        Guid reportId,
        AbuseReportStatus decision,
        AdminReviewAbuseReportRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid actorUserAccountId))
        {
            return AdminAbuseReportAccessResult.Failure(
                AdminAbuseOutcome.Unauthorized,
                Unauthorized);
        }

        ReviewBusinessAbuseReportResult result =
            await reviewService.ReviewAsync(
                new ReviewBusinessAbuseReportCommand(
                    actorUserAccountId,
                    reportId,
                    decision,
                    request.Reason),
                cancellationToken);

        if (!result.Succeeded)
        {
            return MapFailure(result.ErrorCode!);
        }

        BusinessAbuseReportView? report =
            await queryService.GetByIdAsync(result.ReportId!.Value, cancellationToken);
        UserStrikeView? strike = result.StrikeId is { } strikeId
            ? await queryService.GetStrikeByIdAsync(strikeId, cancellationToken)
            : null;

        return report is null
            ? AdminAbuseReportAccessResult.Failure(AdminAbuseOutcome.NotFound, NotFound)
            : AdminAbuseReportAccessResult.Success(
                ToReportResponse(report),
                strike is null ? null : ToStrikeResponse(strike));
    }

    public async Task<AdminAbuseReportAccessResult> RevokeStrikeAsync(
        Guid userAccountId,
        Guid strikeId,
        AdminRevokeUserStrikeRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid actorUserAccountId))
        {
            return AdminAbuseReportAccessResult.Failure(
                AdminAbuseOutcome.Unauthorized,
                Unauthorized);
        }

        UserStrikeCommandResult result =
            await revokeStrikeService.RevokeAsync(
                new RevokeUserStrikeCommand(
                    actorUserAccountId,
                    userAccountId,
                    strikeId,
                    request.Reason),
                cancellationToken);

        if (!result.Succeeded)
        {
            return MapFailure(result.ErrorCode!);
        }

        UserStrikeView? strike =
            await queryService.GetStrikeByIdAsync(result.StrikeId!.Value, cancellationToken);

        return strike is null
            ? AdminAbuseReportAccessResult.Failure(AdminAbuseOutcome.NotFound, StrikeNotFound)
            : AdminAbuseReportAccessResult.Success(ToStrikeResponse(strike));
    }

    public static AdminBusinessAbuseReportResponse ToReportResponse(BusinessAbuseReportView report)
    {
        return new AdminBusinessAbuseReportResponse(
            report.Id,
            report.TenantId,
            report.BranchId,
            report.AppointmentRequestId,
            report.ReportedUserAccountId,
            report.ReportedByUserAccountId,
            report.ReasonCode.ToString(),
            report.Note,
            report.Status.ToString(),
            report.CreatedAtUtc,
            report.ReviewedAtUtc,
            report.ReviewedByUserAccountId,
            report.ReviewReason);
    }

    public static AdminUserStrikeResponse ToStrikeResponse(UserStrikeView strike)
    {
        return new AdminUserStrikeResponse(
            strike.Id,
            strike.UserAccountId,
            strike.TenantId,
            strike.SourceAbuseReportId,
            strike.ReasonCode.ToString(),
            strike.IssuedByUserAccountId,
            strike.IssuedAtUtc,
            strike.ExpiresAtUtc,
            strike.RevokedAtUtc,
            strike.RevokedByUserAccountId,
            strike.RevocationReason,
            strike.IsActive);
    }

    private static AdminAbuseReportAccessResult MapFailure(string errorCode)
    {
        AdminAbuseOutcome outcome = errorCode switch
        {
            "BUSINESS_ABUSE_REPORT_REVIEW_INVALID" or "USER_STRIKE_REVOCATION_INVALID" =>
                AdminAbuseOutcome.BadRequest,
            "BUSINESS_ABUSE_REPORT_NOT_FOUND" or "USER_STRIKE_NOT_FOUND" =>
                AdminAbuseOutcome.NotFound,
            "BUSINESS_ABUSE_REPORT_ALREADY_REVIEWED" => AdminAbuseOutcome.Conflict,
            _ => AdminAbuseOutcome.Unprocessable,
        };

        return AdminAbuseReportAccessResult.Failure(outcome, errorCode);
    }

    private static bool TryGetUserAccountId(
        ClaimsPrincipal user,
        out Guid userAccountId)
    {
        string? rawUserId = user.FindFirstValue("sub")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(rawUserId, out userAccountId);
    }
}
