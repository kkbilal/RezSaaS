using RezSaaS.Modules.Integrations.Domain;

namespace RezSaaS.ArchitectureTests;

public sealed class IntegrationFoundationTests
{
    private static readonly DateTimeOffset TestTime =
        new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid TenantId = Guid.Parse("019A0000-0000-7000-8000-000000000001");

    private static readonly Guid ActorUserAccountId =
        Guid.Parse("019A0000-0000-7000-8000-000000000002");

    private static string Hash => new('A', 64);

    [Fact]
    public void IntegrationApiClientRejectsPlaintextSecretAsHash()
    {
        Assert.Throws<ArgumentException>(() =>
            IntegrationApiClient.Create(
                TenantId,
                "External CRM",
                "rz_live_123",
                "plaintext-api-key",
                "appointments:read",
                ActorUserAccountId,
                TestTime));
    }

    [Fact]
    public void WebhookSubscriptionRequiresHttpsTargetWithoutQuerySecrets()
    {
        Assert.Throws<ArgumentException>(() =>
            WebhookSubscription.Create(
                TenantId,
                "CRM Webhook",
                "https://crm.example.test/hooks/rezsaas?secret=leak",
                "appointment.created",
                Hash,
                ActorUserAccountId,
                TestTime));
    }

    [Fact]
    public void WebhookDeliveryAcceptsOnlyPayloadHash()
    {
        Assert.Throws<ArgumentException>(() =>
            WebhookDelivery.Create(
                TenantId,
                Guid.CreateVersion7(),
                "appointment.created",
                Guid.CreateVersion7(),
                """{"raw":"payload"}""",
                TestTime));
    }
}
