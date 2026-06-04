namespace RezSaaS.Api.Customer;

public sealed record CustomerAbuseAppealResponse(
    Guid AppealId,
    string TargetType,
    Guid TargetId,
    string Statement,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReviewedAtUtc);
