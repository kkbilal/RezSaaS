namespace RezSaaS.Modules.Integrations.Application;

public sealed record CreateWebhookSubscriptionResult(
    Guid WebhookSubscriptionId,
    string OneTimePlaintextSigningSecret,
    DateTimeOffset CreatedAtUtc);
