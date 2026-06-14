using System.Net;
using System.Text.Json;

namespace RezSaaS.IdentityIntegrationTests;

public sealed class AdminIntegrationReadinessApiTests : IClassFixture<IdentityApiFixture>
{
    private readonly IdentityApiFixture fixture;

    public AdminIntegrationReadinessApiTests(IdentityApiFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task IntegrationReadinessIsStepUpProtectedAndDoesNotExposeRawSecrets()
    {
        HttpResponseMessage anonymousResponse =
            await fixture.Client.GetAsync("/api/admin/integrations/readiness");

        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        using HttpClient adminClient =
            fixture.CreatePlatformAdminStepUpClient(Guid.CreateVersion7());
        HttpResponseMessage adminResponse =
            await adminClient.GetAsync("/api/admin/integrations/readiness");
        string json = await adminResponse.Content.ReadAsStringAsync();
        using JsonDocument body = JsonDocument.Parse(json);
        JsonElement root = body.RootElement;

        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
        Assert.Equal("Disabled", root.GetProperty("status").GetString());
        Assert.False(root.GetProperty("externalApiEnabled").GetBoolean());
        Assert.False(root.GetProperty("webhookDeliveryEnabled").GetBoolean());
        Assert.True(root.GetProperty("apiKeyHashStorageReady").GetBoolean());
        Assert.True(root.GetProperty("webhookSigningSecretHashReady").GetBoolean());
        Assert.False(root.GetProperty("storesRawSecrets").GetBoolean());
        Assert.False(root.GetProperty("storesRawWebhookPayloads").GetBoolean());
        Assert.Equal(0, root.GetProperty("activeApiClientCount").GetInt32());
        Assert.Equal(0, root.GetProperty("activeWebhookSubscriptionCount").GetInt32());
        Assert.Equal(0, root.GetProperty("pendingWebhookDeliveryCount").GetInt32());
        Assert.Equal(0, root.GetProperty("failedWebhookDeliveryCount").GetInt32());
        Assert.DoesNotContain("apiKey\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("signingSecret\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rawPayload", json, StringComparison.OrdinalIgnoreCase);
    }
}
