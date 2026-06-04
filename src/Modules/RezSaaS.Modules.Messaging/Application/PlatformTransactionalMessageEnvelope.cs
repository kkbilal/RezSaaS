using RezSaaS.Modules.Messaging.Domain;

namespace RezSaaS.Modules.Messaging.Application;

public sealed record PlatformTransactionalMessageEnvelope(
    Guid UserAccountId,
    PlatformMessagePurpose Purpose,
    Guid CorrelationId,
    string DeliveryKey,
    string Subject,
    string Body);
