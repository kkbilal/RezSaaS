namespace RezSaaS.Api.Admin;

public sealed record AdminUserAbuseOverviewResponse(
    Guid UserAccountId,
    IReadOnlyCollection<AdminAbuseEventResponse> Events,
    IReadOnlyCollection<AdminUserSanctionResponse> Sanctions);
