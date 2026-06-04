namespace RezSaaS.Api.Business;

public sealed record BusinessAbuseReportRequest(
    string ReasonCode,
    string? Note);
