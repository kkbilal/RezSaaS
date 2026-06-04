namespace RezSaaS.Api.Admin;

public sealed record AdminUserRiskResponse(
    int ActiveStrikeCount,
    string Level);
