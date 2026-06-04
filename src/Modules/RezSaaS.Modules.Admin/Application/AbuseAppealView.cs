using RezSaaS.Modules.Admin.Domain;

namespace RezSaaS.Modules.Admin.Application;

public sealed record AbuseAppealView(
    Guid Id,
    Guid UserAccountId,
    AbuseAppealTargetType TargetType,
    Guid TargetId,
    string Statement,
    AbuseAppealStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReviewedAtUtc,
    Guid? ReviewedByUserAccountId,
    string? ReviewReason);
