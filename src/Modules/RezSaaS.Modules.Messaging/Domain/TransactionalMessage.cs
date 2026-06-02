namespace RezSaaS.Modules.Messaging.Domain;

public sealed class TransactionalMessage
{
    private TransactionalMessage()
    {
    }

    private TransactionalMessage(
        Guid id,
        Guid tenantId,
        MessageChannel channel,
        string recipientMasked,
        string templateKey,
        string payloadJson,
        DateTimeOffset createdAtUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Value is required.", nameof(tenantId));
        }

        Id = id;
        TenantId = tenantId;
        Channel = channel;
        RecipientMasked = NormalizeRequiredText(recipientMasked, nameof(recipientMasked));
        TemplateKey = NormalizeRequiredText(templateKey, nameof(templateKey));
        PayloadJson = string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson.Trim();
        CreatedAtUtc = createdAtUtc;
    }

    public MessageChannel Channel { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid Id { get; private set; }

    public string PayloadJson { get; private set; } = "{}";

    public string? ProviderMessageId { get; private set; }

    public string RecipientMasked { get; private set; } = string.Empty;

    public DateTimeOffset? SentAtUtc { get; private set; }

    public TransactionalMessageStatus Status { get; private set; } = TransactionalMessageStatus.Pending;

    public Guid TenantId { get; private set; }

    public string TemplateKey { get; private set; } = string.Empty;

    public static TransactionalMessage Create(
        Guid tenantId,
        MessageChannel channel,
        string recipientMasked,
        string templateKey,
        string payloadJson,
        DateTimeOffset createdAtUtc)
    {
        return new TransactionalMessage(
            Guid.CreateVersion7(),
            tenantId,
            channel,
            recipientMasked,
            templateKey,
            payloadJson,
            createdAtUtc);
    }

    public void MarkSent(string providerMessageId, DateTimeOffset sentAtUtc)
    {
        ProviderMessageId = NormalizeRequiredText(providerMessageId, nameof(providerMessageId));
        SentAtUtc = sentAtUtc;
        Status = TransactionalMessageStatus.Sent;
    }

    private static string NormalizeRequiredText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }
}
