using RezSaaS.Modules.Admin.Domain;

namespace RezSaaS.Modules.Admin.Application;

public sealed record AccountClosureCaseView(
    Guid Id,
    Guid UserAccountId,
    Guid ProposedByUserAccountId,
    string InternalReason,
    string CustomerNotice,
    DateTimeOffset ProposedAtUtc,
    DateTimeOffset EligibleForExecutionAtUtc,
    AccountClosureCaseStatus Status,
    Guid? ReviewedByUserAccountId,
    string? DecisionReason,
    DateTimeOffset? DecidedAtUtc,
    Guid? ExecutionStartedByUserAccountId,
    DateTimeOffset? ExecutionStartedAtUtc,
    Guid? ExecutedByUserAccountId,
    DateTimeOffset? ExecutedAtUtc);
