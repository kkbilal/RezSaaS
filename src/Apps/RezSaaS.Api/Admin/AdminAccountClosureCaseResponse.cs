namespace RezSaaS.Api.Admin;

public sealed record AdminAccountClosureCaseResponse(
    Guid ClosureCaseId,
    Guid UserAccountId,
    Guid ProposedByUserAccountId,
    string InternalReason,
    string CustomerNotice,
    DateTimeOffset ProposedAtUtc,
    DateTimeOffset? CustomerNoticeDeliveredAtUtc,
    DateTimeOffset? EligibleForExecutionAtUtc,
    string Status,
    Guid? ReviewedByUserAccountId,
    string? DecisionReason,
    DateTimeOffset? DecidedAtUtc,
    Guid? ExecutionStartedByUserAccountId,
    DateTimeOffset? ExecutionStartedAtUtc,
    Guid? ExecutedByUserAccountId,
    DateTimeOffset? ExecutedAtUtc);
