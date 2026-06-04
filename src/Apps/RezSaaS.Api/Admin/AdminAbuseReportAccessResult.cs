namespace RezSaaS.Api.Admin;

public sealed record AdminAbuseReportAccessResult(
    AdminAbuseOutcome Outcome,
    IReadOnlyCollection<AdminBusinessAbuseReportResponse> Reports,
    AdminBusinessAbuseReportResponse? Report,
    AdminUserStrikeResponse? Strike,
    string? ErrorCode)
{
    public static AdminAbuseReportAccessResult Success(
        IReadOnlyCollection<AdminBusinessAbuseReportResponse> reports)
    {
        return new AdminAbuseReportAccessResult(
            AdminAbuseOutcome.Success,
            reports,
            Report: null,
            Strike: null,
            ErrorCode: null);
    }

    public static AdminAbuseReportAccessResult Success(
        AdminBusinessAbuseReportResponse report,
        AdminUserStrikeResponse? strike)
    {
        return new AdminAbuseReportAccessResult(
            AdminAbuseOutcome.Success,
            Reports: [],
            report,
            strike,
            ErrorCode: null);
    }

    public static AdminAbuseReportAccessResult Success(AdminUserStrikeResponse strike)
    {
        return new AdminAbuseReportAccessResult(
            AdminAbuseOutcome.Success,
            Reports: [],
            Report: null,
            strike,
            ErrorCode: null);
    }

    public static AdminAbuseReportAccessResult Failure(
        AdminAbuseOutcome outcome,
        string errorCode)
    {
        return new AdminAbuseReportAccessResult(
            outcome,
            Reports: [],
            Report: null,
            Strike: null,
            errorCode);
    }
}
