namespace RezSaaS.Modules.Payments.Domain;

public sealed class PaymentWebhookEvent
{
    private PaymentWebhookEvent()
    {
    }

    private PaymentWebhookEvent(
        Guid id,
        string providerKey,
        string providerEventId,
        string eventType,
        string payloadSha256,
        DateTimeOffset receivedAtUtc)
    {
        Id = id;
        ProviderKey = NormalizeRequiredText(providerKey, nameof(providerKey), maxLength: 80);
        ProviderEventId = NormalizeRequiredText(providerEventId, nameof(providerEventId), maxLength: 180);
        EventType = NormalizeRequiredText(eventType, nameof(eventType), maxLength: 120);
        PayloadSha256 = NormalizeSha256(payloadSha256);
        ReceivedAtUtc = receivedAtUtc;
        Status = PaymentWebhookEventStatus.Received;
    }

    public string EventType { get; private set; } = string.Empty;

    public Guid Id { get; private set; }

    public string LastErrorCode { get; private set; } = string.Empty;

    public string PayloadSha256 { get; private set; } = string.Empty;

    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    public string ProviderEventId { get; private set; } = string.Empty;

    public string ProviderKey { get; private set; } = string.Empty;

    public DateTimeOffset ReceivedAtUtc { get; private set; }

    public PaymentWebhookEventStatus Status { get; private set; }

    public static PaymentWebhookEvent RecordReceived(
        string providerKey,
        string providerEventId,
        string eventType,
        string payloadSha256,
        DateTimeOffset receivedAtUtc)
    {
        return new PaymentWebhookEvent(
            Guid.CreateVersion7(),
            providerKey,
            providerEventId,
            eventType,
            payloadSha256,
            receivedAtUtc);
    }

    public void BeginProcessing()
    {
        if (Status != PaymentWebhookEventStatus.Received)
        {
            throw new InvalidOperationException("Only received webhook events can be processed.");
        }

        Status = PaymentWebhookEventStatus.Processing;
    }

    public void MarkProcessed(DateTimeOffset processedAtUtc)
    {
        if (Status != PaymentWebhookEventStatus.Processing)
        {
            throw new InvalidOperationException("Only processing webhook events can be marked processed.");
        }

        Status = PaymentWebhookEventStatus.Processed;
        ProcessedAtUtc = processedAtUtc;
        LastErrorCode = string.Empty;
    }

    public void MarkFailed(string errorCode)
    {
        Status = PaymentWebhookEventStatus.Failed;
        LastErrorCode = NormalizeRequiredText(errorCode, nameof(errorCode), maxLength: 120);
    }

    private static string NormalizeSha256(string value)
    {
        string normalized = NormalizeRequiredText(value, nameof(value), maxLength: 64).ToUpperInvariant();

        if (normalized.Length != 64 || normalized.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException("Payload hash must be a SHA-256 hex string.", nameof(value));
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
}
