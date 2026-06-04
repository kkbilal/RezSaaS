using RezSaaS.Modules.Admin.Domain;

namespace RezSaaS.Modules.Admin.Application;

public sealed record CustomerAbuseAppealView(
    Guid Id,
    AbuseAppealTargetType TargetType,
    Guid TargetId,
    string Statement,
    AbuseAppealStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReviewedAtUtc);
