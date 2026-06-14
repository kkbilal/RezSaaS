namespace RezSaaS.Modules.Integrations.Domain;

public sealed class WebhookDelivery
{
    private WebhookDelivery()
    {
    }

    private WebhookDelivery(
        Guid id,
        Guid tenantId,
        Guid subscriptionId,
        string eventType,
        Guid correlationId,
        string payloadSha256,
        DateTimeOffset createdAtUtc)
    {
        RequireNonEmpty(tenantId, nameof(tenantId));
        RequireNonEmpty(subscriptionId, nameof(subscriptionId));
        RequireNonEmpty(correlationId, nameof(correlationId));

        Id = id;
        TenantId = tenantId;
        SubscriptionId = subscriptionId;
        EventType = NormalizeRequiredText(eventType, nameof(eventType), maxLength: 120);
        CorrelationId = correlationId;
        PayloadSha256 = NormalizeSha256(payloadSha256);
        CreatedAtUtc = createdAtUtc;
        Status = WebhookDeliveryStatus.Pending;
    }

    public int AttemptCount { get; private set; }

    public Guid CorrelationId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? DeliveredAtUtc { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public Guid Id { get; private set; }

    public string LastErrorCode { get; private set; } = string.Empty;

    public DateTimeOffset? LastAttemptAtUtc { get; private set; }

    public DateTimeOffset? LockedUntilUtc { get; private set; }

    public string PayloadSha256 { get; private set; } = string.Empty;

    public WebhookDeliveryStatus Status { get; private set; }

    public Guid SubscriptionId { get; private set; }

    public Guid TenantId { get; private set; }

    public static WebhookDelivery Create(
        Guid tenantId,
        Guid subscriptionId,
        string eventType,
        Guid correlationId,
        string payloadSha256,
        DateTimeOffset createdAtUtc)
    {
        return new WebhookDelivery(
            Guid.CreateVersion7(),
            tenantId,
            subscriptionId,
            eventType,
            correlationId,
            payloadSha256,
            createdAtUtc);
    }

    public void BeginAttempt(DateTimeOffset now, TimeSpan lockDuration)
    {
        if (lockDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(lockDuration), "Lock duration must be positive.");
        }

        if (Status != WebhookDeliveryStatus.Pending && Status != WebhookDeliveryStatus.Failed)
        {
            throw new InvalidOperationException("Webhook delivery is not claimable.");
        }

        Status = WebhookDeliveryStatus.Processing;
        LastAttemptAtUtc = now;
        LockedUntilUtc = now.Add(lockDuration);
        AttemptCount += 1;
    }

    public void MarkDelivered(DateTimeOffset deliveredAtUtc)
    {
        if (Status != WebhookDeliveryStatus.Processing)
        {
            throw new InvalidOperationException("Only processing delivery can be marked delivered.");
        }

        Status = WebhookDeliveryStatus.Delivered;
        DeliveredAtUtc = deliveredAtUtc;
        LockedUntilUtc = null;
        LastErrorCode = string.Empty;
    }

    public void MarkFailed(string errorCode)
    {
        if (Status != WebhookDeliveryStatus.Processing)
        {
            throw new InvalidOperationException("Only processing delivery can be marked failed.");
        }

        Status = WebhookDeliveryStatus.Failed;
        LockedUntilUtc = null;
        LastErrorCode = NormalizeRequiredText(errorCode, nameof(errorCode), maxLength: 120);
    }

    public void Cancel()
    {
        if (Status == WebhookDeliveryStatus.Delivered)
        {
            throw new InvalidOperationException("Delivered webhook delivery cannot be cancelled.");
        }

        Status = WebhookDeliveryStatus.Cancelled;
        LockedUntilUtc = null;
    }

    private static string NormalizeSha256(string value)
    {
        string normalized = NormalizeRequiredText(value, nameof(value), maxLength: 64)
            .ToUpperInvariant();

        if (normalized.Length != 64 || normalized.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException("Value must be a SHA-256 hex string.", nameof(value));
        }

        return normalized;
    }

    private static string NormalizeRequiredText(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        string normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException("Value is too long.", parameterName);
        }

        return normalized;
    }

    private static void RequireNonEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value is required.", parameterName);
        }
    }
}
