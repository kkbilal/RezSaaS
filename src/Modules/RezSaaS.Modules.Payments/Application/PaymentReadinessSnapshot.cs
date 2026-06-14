namespace RezSaaS.Modules.Payments.Application;

public sealed record PaymentReadinessSnapshot(
    DateTimeOffset EvaluatedAtUtc,
    bool OnlineCollectionEnabled,
    bool ProviderConfigured,
    bool HostedCheckoutOnly,
    bool StoresRawCardData,
    bool WebhookIdempotencyStorageReady,
    int PolicyCount,
    int OpenIntentCount,
    int UnprocessedWebhookEventCount);
