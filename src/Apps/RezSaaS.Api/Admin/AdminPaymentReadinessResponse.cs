namespace RezSaaS.Api.Admin;

public sealed record AdminPaymentReadinessResponse(
    DateTimeOffset EvaluatedAtUtc,
    string Status,
    bool OnlineCollectionEnabled,
    bool ProviderConfigured,
    bool HostedCheckoutOnly,
    bool StoresRawCardData,
    bool WebhookIdempotencyStorageReady,
    int PolicyCount,
    int OpenIntentCount,
    int UnprocessedWebhookEventCount);
