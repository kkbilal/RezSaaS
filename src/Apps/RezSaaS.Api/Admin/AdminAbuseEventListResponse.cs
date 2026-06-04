namespace RezSaaS.Api.Admin;

public sealed record AdminAbuseEventListResponse(
    IReadOnlyCollection<AdminAbuseEventResponse> Events);
