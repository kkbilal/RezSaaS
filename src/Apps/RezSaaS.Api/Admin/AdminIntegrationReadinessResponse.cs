namespace RezSaaS.Api.Admin;

public sealed record AdminIntegrationReadinessResponse(
    DateTimeOffset EvaluatedAtUtc,
    string Status,
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
