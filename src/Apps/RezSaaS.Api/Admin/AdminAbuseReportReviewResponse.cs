namespace RezSaaS.Api.Admin;

public sealed record AdminAbuseReportReviewResponse(
    AdminBusinessAbuseReportResponse Report,
    AdminUserStrikeResponse? Strike);
