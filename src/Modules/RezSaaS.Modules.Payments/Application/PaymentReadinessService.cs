using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RezSaaS.Modules.Payments.Domain;
using RezSaaS.Modules.Payments.Infrastructure.Persistence;

namespace RezSaaS.Modules.Payments.Application;

public sealed class PaymentReadinessService
{
    private readonly PaymentsDbContext dbContext;
    private readonly IOptions<PaymentReadinessOptions> options;
    private readonly TimeProvider timeProvider;

    public PaymentReadinessService(
        PaymentsDbContext dbContext,
        IOptions<PaymentReadinessOptions> options,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.options = options;
        this.timeProvider = timeProvider;
    }

    public async Task<PaymentReadinessSnapshot> InspectAsync(
        CancellationToken cancellationToken = default)
    {
        PaymentReadinessOptions value = options.Value;
        int policyCount = await dbContext.PaymentPolicies
            .IgnoreQueryFilters()
            .CountAsync(cancellationToken);
        int openIntentCount = await dbContext.PaymentIntents
            .IgnoreQueryFilters()
            .CountAsync(
                entity => entity.Status == PaymentIntentStatus.PendingCheckout
                    || entity.Status == PaymentIntentStatus.CheckoutCreated,
                cancellationToken);
        int unprocessedWebhookEventCount = await dbContext.WebhookEvents
            .CountAsync(
                entity => entity.Status == PaymentWebhookEventStatus.Received
                    || entity.Status == PaymentWebhookEventStatus.Processing
                    || entity.Status == PaymentWebhookEventStatus.Failed,
                cancellationToken);

        return new PaymentReadinessSnapshot(
            timeProvider.GetUtcNow(),
            value.OnlineCollectionEnabled,
            !string.IsNullOrWhiteSpace(value.ProviderKey),
            HostedCheckoutOnly: true,
            StoresRawCardData: false,
            WebhookIdempotencyStorageReady: true,
            policyCount,
            openIntentCount,
            unprocessedWebhookEventCount);
    }
}
