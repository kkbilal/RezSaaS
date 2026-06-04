namespace RezSaaS.Api.Admin;

public sealed record AdminUserStrikeResponse(
    Guid StrikeId,
    Guid UserAccountId,
    Guid TenantId,
    Guid SourceAbuseReportId,
    string ReasonCode,
    Guid IssuedByUserAccountId,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? RevokedAtUtc,
    Guid? RevokedByUserAccountId,
    string? RevocationReason,
    bool IsActive);
