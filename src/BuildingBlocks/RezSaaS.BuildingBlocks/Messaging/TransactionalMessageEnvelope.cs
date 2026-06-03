namespace RezSaaS.BuildingBlocks.Messaging;

public sealed record TransactionalMessageEnvelope(
    Guid TenantId,
    TransactionalMessageChannel Channel,
    string RecipientMasked,
    string TemplateKey,
    string PayloadJson,
    DateTimeOffset CreatedAtUtc);
