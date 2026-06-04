namespace RezSaaS.Api.Customer;

public sealed record CustomerAccountClosureCaseResponse(
    Guid ClosureCaseId,
    string CustomerNotice,
    DateTimeOffset ProposedAtUtc,
    DateTimeOffset? CustomerNoticeDeliveredAtUtc,
    DateTimeOffset? EligibleForExecutionAtUtc,
    string Status,
    DateTimeOffset? DecidedAtUtc,
    DateTimeOffset? ExecutedAtUtc);
