namespace RezSaaS.Modules.Admin.Application;

public sealed record ReviewBusinessAbuseReportResult(
    bool Succeeded,
    Guid? ReportId,
    Guid? StrikeId,
    string? ErrorCode)
{
    public static ReviewBusinessAbuseReportResult Success(
        Guid reportId,
        Guid? strikeId)
    {
        return new ReviewBusinessAbuseReportResult(
            true,
            reportId,
            strikeId,
            ErrorCode: null);
    }

    public static ReviewBusinessAbuseReportResult Failure(string errorCode)
    {
        return new ReviewBusinessAbuseReportResult(
            false,
            ReportId: null,
            StrikeId: null,
            errorCode);
    }
}
