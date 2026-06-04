namespace RezSaaS.Api.Admin;

public sealed record AdminApplyUserSanctionRequest(
    string Type,
    string Reason,
    DateTimeOffset? EndsAtUtc);
