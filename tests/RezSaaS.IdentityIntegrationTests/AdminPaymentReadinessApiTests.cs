using System.Net;
using System.Text.Json;

namespace RezSaaS.IdentityIntegrationTests;

public sealed class AdminPaymentReadinessApiTests : IClassFixture<IdentityApiFixture>
{
    private readonly IdentityApiFixture fixture;

    public AdminPaymentReadinessApiTests(IdentityApiFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task PaymentReadinessIsStepUpProtectedAndDoesNotExposeProviderSecrets()
    {
        HttpResponseMessage anonymousResponse =
            await fixture.Client.GetAsync("/api/admin/payments/readiness");

        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        using HttpClient adminClient =
            fixture.CreatePlatformAdminStepUpClient(Guid.CreateVersion7());
        HttpResponseMessage adminResponse =
            await adminClient.GetAsync("/api/admin/payments/readiness");
        string json = await adminResponse.Content.ReadAsStringAsync();
        using JsonDocument body = JsonDocument.Parse(json);
        JsonElement root = body.RootElement;

        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
        Assert.Equal("Disabled", root.GetProperty("status").GetString());
        Assert.False(root.GetProperty("onlineCollectionEnabled").GetBoolean());
        Assert.True(root.GetProperty("hostedCheckoutOnly").GetBoolean());
        Assert.False(root.GetProperty("storesRawCardData").GetBoolean());
        Assert.True(root.GetProperty("webhookIdempotencyStorageReady").GetBoolean());
        Assert.Equal(0, root.GetProperty("policyCount").GetInt32());
        Assert.Equal(0, root.GetProperty("openIntentCount").GetInt32());
        Assert.Equal(0, root.GetProperty("unprocessedWebhookEventCount").GetInt32());
        Assert.DoesNotContain("providerKey", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", json, StringComparison.OrdinalIgnoreCase);
    }
}
