namespace RezSaaS.Modules.Admin.Application;

public sealed record BusinessAbuseReportCommandResult(
    bool Succeeded,
    Guid? ReportId,
    bool Created,
    string? ErrorCode)
{
    public static BusinessAbuseReportCommandResult Success(Guid reportId, bool created)
    {
        return new BusinessAbuseReportCommandResult(
            true,
            reportId,
            created,
            ErrorCode: null);
    }

    public static BusinessAbuseReportCommandResult Failure(string errorCode)
    {
        return new BusinessAbuseReportCommandResult(
            false,
            ReportId: null,
            Created: false,
            errorCode);
    }
}
