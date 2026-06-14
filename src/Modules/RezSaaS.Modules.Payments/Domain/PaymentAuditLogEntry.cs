namespace RezSaaS.Modules.Payments.Domain;

public sealed class PaymentAuditLogEntry
{
    private PaymentAuditLogEntry()
    {
    }

    private PaymentAuditLogEntry(
        Guid id,
        Guid tenantId,
        Guid actorUserAccountId,
        string action,
        string detailsJson,
        DateTimeOffset occurredAtUtc)
    {
        RequireNonEmpty(tenantId, nameof(tenantId));
        RequireNonEmpty(actorUserAccountId, nameof(actorUserAccountId));

        Id = id;
        TenantId = tenantId;
        ActorUserAccountId = actorUserAccountId;
        Action = NormalizeRequiredText(action, nameof(action), maxLength: 128);
        DetailsJson = string.IsNullOrWhiteSpace(detailsJson) ? "{}" : detailsJson.Trim();
        OccurredAtUtc = occurredAtUtc;
    }

    public string Action { get; private set; } = string.Empty;

    public Guid ActorUserAccountId { get; private set; }

    public string DetailsJson { get; private set; } = "{}";

    public Guid Id { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public Guid TenantId { get; private set; }

    public static PaymentAuditLogEntry Create(
        Guid tenantId,
        Guid actorUserAccountId,
        string action,
        string detailsJson,
        DateTimeOffset occurredAtUtc)
    {
        return new PaymentAuditLogEntry(
            Guid.CreateVersion7(),
            tenantId,
            actorUserAccountId,
            action,
            detailsJson,
            occurredAtUtc);
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
