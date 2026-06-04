namespace RezSaaS.Api.Business;

public sealed record BusinessAbuseReportAccessResult(
    BusinessAbuseReportOutcome Outcome,
    BusinessAbuseReportResponse? Report,
    string? ErrorCode)
{
    public static BusinessAbuseReportAccessResult Success(
        BusinessAbuseReportResponse report,
        bool created)
    {
        return new BusinessAbuseReportAccessResult(
            created ? BusinessAbuseReportOutcome.Created : BusinessAbuseReportOutcome.Success,
            report,
            ErrorCode: null);
    }

    public static BusinessAbuseReportAccessResult Failure(
        BusinessAbuseReportOutcome outcome,
        string errorCode)
    {
        return new BusinessAbuseReportAccessResult(
            outcome,
            Report: null,
            errorCode);
    }
}
