namespace RezSaaS.Modules.Integrations.Application;

public sealed record IntegrationReadinessSnapshot(
    DateTimeOffset EvaluatedAtUtc,
    bool ExternalApiEnabled,
    bool WebhookDeliveryEnabled,
    bool ApiKeyHashStorageReady,
    bool WebhookSigningSecretHashReady,
    bool StoresRawSecrets,
    bool StoresRawWebhookPayloads,
    int ActiveApiClientCount,
    int ActiveWebhookSubscriptionCount,
    int PendingWebhookDeliveryCount,
    int FailedWebhookDeliveryCount);
