namespace RezSaaS.Modules.Payments.Application;

public sealed class PaymentReadinessOptions
{
    public const string SectionName = "Payments";

    public bool OnlineCollectionEnabled { get; init; }

    public string? ProviderKey { get; init; }

    public int WebhookMaxPayloadBytes { get; init; } = 128_000;
}
