namespace RezSaaS.Api.Admin;

public sealed record AdminAbuseReportListResponse(
    IReadOnlyCollection<AdminBusinessAbuseReportResponse> Reports);
