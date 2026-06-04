using RezSaaS.Modules.Admin.Domain;

namespace RezSaaS.Modules.Admin.Application;

public sealed record CustomerAccountClosureCaseView(
    Guid Id,
    string CustomerNotice,
    DateTimeOffset ProposedAtUtc,
    DateTimeOffset? CustomerNoticeDeliveredAtUtc,
    DateTimeOffset? EligibleForExecutionAtUtc,
    AccountClosureCaseStatus Status,
    DateTimeOffset? DecidedAtUtc,
    DateTimeOffset? ExecutedAtUtc);
