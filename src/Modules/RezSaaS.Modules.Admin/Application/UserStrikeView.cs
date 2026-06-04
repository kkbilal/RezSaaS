using RezSaaS.Modules.Admin.Domain;

namespace RezSaaS.Modules.Admin.Application;

public sealed record UserStrikeView(
    Guid Id,
    Guid UserAccountId,
    Guid TenantId,
    Guid SourceAbuseReportId,
    AbuseReportReasonCode ReasonCode,
    Guid IssuedByUserAccountId,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? RevokedAtUtc,
    Guid? RevokedByUserAccountId,
    string? RevocationReason,
    bool IsActive);
