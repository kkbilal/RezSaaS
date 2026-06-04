namespace RezSaaS.Api.Admin;

public sealed record AdminAbuseAppealResponse(
    Guid AppealId,
    Guid UserAccountId,
    string TargetType,
    Guid TargetId,
    string Statement,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReviewedAtUtc,
    Guid? ReviewedByUserAccountId,
    string? ReviewReason);
