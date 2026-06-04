using RezSaaS.Modules.Messaging.Domain;

namespace RezSaaS.Modules.Messaging.Application;

public sealed record PlatformTransactionalMessageDeliveryView(
    Guid Id,
    Guid UserAccountId,
    PlatformMessagePurpose Purpose,
    Guid CorrelationId,
    string Subject,
    string Body,
    int AttemptCount,
    DateTimeOffset? SentAtUtc);
