using RezSaaS.Modules.Admin.Domain;

namespace RezSaaS.Modules.Admin.Application;

public sealed record CustomerUserSanctionView(
    Guid Id,
    UserSanctionType Type,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? EndsAtUtc,
    DateTimeOffset? RevokedAtUtc,
    bool IsActive);
