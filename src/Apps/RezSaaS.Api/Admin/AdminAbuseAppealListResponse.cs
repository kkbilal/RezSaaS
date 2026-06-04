namespace RezSaaS.Api.Admin;

public sealed record AdminAbuseAppealListResponse(
    IReadOnlyCollection<AdminAbuseAppealResponse> Appeals);
