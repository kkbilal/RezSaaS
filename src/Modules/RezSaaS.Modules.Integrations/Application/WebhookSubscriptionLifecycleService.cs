using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Integrations.Domain;
using RezSaaS.Modules.Integrations.Infrastructure.Persistence;

namespace RezSaaS.Modules.Integrations.Application;

public sealed class WebhookSubscriptionLifecycleService
{
    public const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    public const string NotFound = "WEBHOOK_SUBSCRIPTION_NOT_FOUND";

    private readonly IntegrationsDbContext dbContext;
    private readonly IOptions<IntegrationReadinessOptions> options;
    private readonly ITenantContextAccessor tenantContextAccessor;
    private readonly TimeProvider timeProvider;

    public WebhookSubscriptionLifecycleService(
        IntegrationsDbContext dbContext,
        IOptions<IntegrationReadinessOptions> options,
        ITenantContextAccessor tenantContextAccessor,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.options = options;
        this.tenantContextAccessor = tenantContextAccessor;
        this.timeProvider = timeProvider;
    }

    public async Task<CreateWebhookSubscriptionResult> CreateAsync(
        CreateWebhookSubscriptionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!options.Value.WebhookDeliveryEnabled)
        {
            throw new InvalidOperationException("Integration webhook delivery is disabled.");
        }

        Guid tenantId = RequireTenantId();
        string eventTypes = NormalizeEventTypes(command.EventTypes);
        IntegrationSecretMaterial secret = IntegrationSecretFactory.CreateWebhookSigningSecret();
        DateTimeOffset now = timeProvider.GetUtcNow();
        WebhookSubscription subscription = WebhookSubscription.Create(
            tenantId,
            command.DisplayName,
            command.TargetUrl,
            eventTypes,
            secret.Sha256Hash,
            command.ActorUserAccountId,
            now);

        dbContext.WebhookSubscriptions.Add(subscription);
        dbContext.AuditLogEntries.Add(
            IntegrationAuditLogEntry.Create(
                tenantId,
                command.ActorUserAccountId,
                "integrations.webhook_subscription.created",
                $$"""{"tenantId":"{{tenantId}}","webhookSubscriptionId":"{{subscription.Id}}"}""",
                now));

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateWebhookSubscriptionResult(
            subscription.Id,
            secret.Plaintext,
            now);
    }

    public async Task<IntegrationLifecycleResult> PauseAsync(
        Guid subscriptionId,
        Guid actorUserAccountId,
        CancellationToken cancellationToken = default)
    {
        return await MutateAsync(
            subscriptionId,
            actorUserAccountId,
            "integrations.webhook_subscription.paused",
            subscription => subscription.Pause(actorUserAccountId, timeProvider.GetUtcNow()),
            cancellationToken);
    }

    public async Task<IntegrationLifecycleResult> ReactivateAsync(
        Guid subscriptionId,
        Guid actorUserAccountId,
        CancellationToken cancellationToken = default)
    {
        return await MutateAsync(
            subscriptionId,
            actorUserAccountId,
            "integrations.webhook_subscription.reactivated",
            subscription => subscription.Reactivate(actorUserAccountId, timeProvider.GetUtcNow()),
            cancellationToken);
    }

    public async Task<IntegrationLifecycleResult> RevokeAsync(
        Guid subscriptionId,
        Guid actorUserAccountId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        return await MutateAsync(
            subscriptionId,
            actorUserAccountId,
            "integrations.webhook_subscription.revoked",
            subscription => subscription.Revoke(actorUserAccountId, reason, timeProvider.GetUtcNow()),
            cancellationToken);
    }

    private async Task<IntegrationLifecycleResult> MutateAsync(
        Guid subscriptionId,
        Guid actorUserAccountId,
        string auditAction,
        Action<WebhookSubscription> mutation,
        CancellationToken cancellationToken)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return IntegrationLifecycleResult.Failure(MissingTenantContext);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        WebhookSubscription? subscription = await LockWebhookSubscriptionAsync(
            tenantId,
            subscriptionId,
            cancellationToken);

        if (subscription is null)
        {
            return IntegrationLifecycleResult.Failure(NotFound);
        }

        mutation(subscription);
        dbContext.AuditLogEntries.Add(
            IntegrationAuditLogEntry.Create(
                tenantId,
                actorUserAccountId,
                auditAction,
                $$"""{"tenantId":"{{tenantId}}","webhookSubscriptionId":"{{subscription.Id}}"}""",
                now));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return IntegrationLifecycleResult.Success(subscription.Id);
    }

    private async Task<WebhookSubscription?> LockWebhookSubscriptionAsync(
        Guid tenantId,
        Guid subscriptionId,
        CancellationToken cancellationToken)
    {
        return await dbContext.WebhookSubscriptions
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM integrations."WebhookSubscriptions"
                WHERE "TenantId" = {tenantId}
                    AND "Id" = {subscriptionId}
                FOR UPDATE
                """)
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(cancellationToken);
    }

    private Guid RequireTenantId()
    {
        return tenantContextAccessor.TenantId
            ?? throw new InvalidOperationException("Tenant context is required.");
    }

    private static string NormalizeEventTypes(IReadOnlyCollection<string> eventTypes)
    {
        string[] normalizedEventTypes = eventTypes
            .Select(eventType => eventType.Trim())
            .Where(eventType => eventType.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        if (normalizedEventTypes.Length == 0)
        {
            throw new ArgumentException("At least one webhook event type is required.", nameof(eventTypes));
        }

        return string.Join(',', normalizedEventTypes);
    }
}
