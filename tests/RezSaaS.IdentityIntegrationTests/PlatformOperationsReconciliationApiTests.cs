using System.Net;
using System.Text.Json;

namespace RezSaaS.IdentityIntegrationTests;

public sealed class PlatformOperationsReconciliationApiTests : IClassFixture<IdentityApiFixture>
{
    private readonly IdentityApiFixture fixture;

    public PlatformOperationsReconciliationApiTests(IdentityApiFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task OperationsHealthAndAdminSnapshotExposeOnlySafeIncidentMetadata()
    {
        HttpResponseMessage initialDefaultHealth = await fixture.Client.GetAsync("/health");
        HttpResponseMessage initialOperationsHealth =
            await fixture.Client.GetAsync("/health/operations");
        HttpResponseMessage anonymousAdminResponse =
            await fixture.Client.GetAsync("/api/admin/operations/reconciliation");

        Assert.Equal(HttpStatusCode.OK, initialDefaultHealth.StatusCode);
        Assert.Equal(HttpStatusCode.OK, initialOperationsHealth.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousAdminResponse.StatusCode);

        using HttpClient adminClient =
            fixture.CreatePlatformAdminStepUpClient(Guid.CreateVersion7());
        Guid callbackPendingMessageId =
            await fixture.SeedCallbackPendingPlatformNotificationAsync();
        HttpResponseMessage degradedOperationsHealth =
            await fixture.Client.GetAsync("/health/operations");
        HttpResponseMessage degradedAdminResponse =
            await adminClient.GetAsync("/api/admin/operations/reconciliation");
        string degradedAdminJson =
            await degradedAdminResponse.Content.ReadAsStringAsync();
        using (JsonDocument degradedBody = JsonDocument.Parse(degradedAdminJson))
        {
            Assert.Equal(HttpStatusCode.OK, degradedOperationsHealth.StatusCode);
            Assert.Contains(
                "Degraded",
                await degradedOperationsHealth.Content.ReadAsStringAsync(),
                StringComparison.Ordinal);
            Assert.Equal(HttpStatusCode.OK, degradedAdminResponse.StatusCode);
            Assert.Equal(
                "Degraded",
                degradedBody.RootElement.GetProperty("status").GetString());
            Assert.Contains(
                callbackPendingMessageId,
                degradedBody.RootElement
                    .GetProperty("callbackPendingNotificationIds")
                    .EnumerateArray()
                    .Select(element => element.GetGuid()));
        }

        Guid failedMessageId = await fixture.SeedFailedPlatformNotificationAsync();
        HttpResponseMessage defaultHealth = await fixture.Client.GetAsync("/health");
        HttpResponseMessage operationsHealth =
            await fixture.Client.GetAsync("/health/operations");
        HttpResponseMessage adminResponse =
            await adminClient.GetAsync("/api/admin/operations/reconciliation");
        string adminJson = await adminResponse.Content.ReadAsStringAsync();
        using JsonDocument body = JsonDocument.Parse(adminJson);

        Assert.Equal(HttpStatusCode.OK, defaultHealth.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, operationsHealth.StatusCode);
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
        Assert.Equal("Critical", body.RootElement.GetProperty("status").GetString());
        Assert.True(body.RootElement.GetProperty("failedNotificationCount").GetInt32() >= 1);
        Assert.Contains(
            failedMessageId,
            body.RootElement
                .GetProperty("failedNotificationIds")
                .EnumerateArray()
                .Select(element => element.GetGuid()));
        Assert.DoesNotContain("body", adminJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("subject", adminJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("userAccountId", adminJson, StringComparison.OrdinalIgnoreCase);
    }
}
