using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RezSaaS.Modules.Integrations.Domain;
using RezSaaS.Modules.Integrations.Infrastructure.Persistence;

namespace RezSaaS.Modules.Integrations.Application;

public sealed class IntegrationReadinessService
{
    private readonly IntegrationsDbContext dbContext;
    private readonly IOptions<IntegrationReadinessOptions> options;
    private readonly TimeProvider timeProvider;

    public IntegrationReadinessService(
        IntegrationsDbContext dbContext,
        IOptions<IntegrationReadinessOptions> options,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.options = options;
        this.timeProvider = timeProvider;
    }

    public async Task<IntegrationReadinessSnapshot> InspectAsync(
        CancellationToken cancellationToken = default)
    {
        IntegrationReadinessOptions value = options.Value;
        int activeApiClientCount = await dbContext.ApiClients
            .IgnoreQueryFilters()
            .CountAsync(
                entity => entity.Status == IntegrationApiClientStatus.Active,
                cancellationToken);
        int activeWebhookSubscriptionCount = await dbContext.WebhookSubscriptions
            .IgnoreQueryFilters()
            .CountAsync(
                entity => entity.Status == WebhookSubscriptionStatus.Active,
                cancellationToken);
        int pendingWebhookDeliveryCount = await dbContext.WebhookDeliveries
            .IgnoreQueryFilters()
            .CountAsync(
                entity => entity.Status == WebhookDeliveryStatus.Pending
                    || entity.Status == WebhookDeliveryStatus.Processing,
                cancellationToken);
        int failedWebhookDeliveryCount = await dbContext.WebhookDeliveries
            .IgnoreQueryFilters()
            .CountAsync(
                entity => entity.Status == WebhookDeliveryStatus.Failed,
                cancellationToken);

        return new IntegrationReadinessSnapshot(
            timeProvider.GetUtcNow(),
            value.ExternalApiEnabled,
            value.WebhookDeliveryEnabled,
            ApiKeyHashStorageReady: true,
            WebhookSigningSecretHashReady: true,
            StoresRawSecrets: false,
            StoresRawWebhookPayloads: false,
            activeApiClientCount,
            activeWebhookSubscriptionCount,
            pendingWebhookDeliveryCount,
            failedWebhookDeliveryCount);
    }
}
