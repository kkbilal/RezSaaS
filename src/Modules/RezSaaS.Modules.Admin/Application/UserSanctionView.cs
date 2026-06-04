using RezSaaS.Modules.Admin.Domain;

namespace RezSaaS.Modules.Admin.Application;

public sealed record UserSanctionView(
    Guid Id,
    Guid UserAccountId,
    UserSanctionType Type,
    string Reason,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? EndsAtUtc,
    DateTimeOffset? RevokedAtUtc,
    Guid? RevokedByUserAccountId,
    string? RevocationReason,
    bool IsActive);
