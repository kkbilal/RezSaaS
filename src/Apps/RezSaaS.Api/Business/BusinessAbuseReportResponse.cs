namespace RezSaaS.Api.Business;

public sealed record BusinessAbuseReportResponse(
    Guid ReportId,
    string Status,
    DateTimeOffset CreatedAtUtc);
