namespace RezSaaS.Modules.Integrations.Application;

public sealed class IntegrationReadinessOptions
{
    public const string SectionName = "Integrations";

    public bool ExternalApiEnabled { get; init; }

    public bool WebhookDeliveryEnabled { get; init; }

    public int WebhookMaxPayloadBytes { get; init; } = 128_000;
}
