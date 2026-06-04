namespace RezSaaS.Api.Admin;

public sealed record AdminUserSanctionResponse(
    Guid SanctionId,
    Guid UserAccountId,
    string Type,
    string Reason,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? EndsAtUtc,
    DateTimeOffset? RevokedAtUtc,
    Guid? RevokedByUserAccountId,
    string? RevocationReason,
    bool IsActive);
