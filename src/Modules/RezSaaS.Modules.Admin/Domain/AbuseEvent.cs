namespace RezSaaS.Modules.Admin.Domain;

public sealed class AbuseEvent
{
    private AbuseEvent()
    {
    }

    private AbuseEvent(
        Guid id,
        Guid? tenantId,
        Guid userAccountId,
        string eventType,
        AbuseEventSeverity severity,
        string detailsJson,
        DateTimeOffset occurredAtUtc)
    {
        if (userAccountId == Guid.Empty)
        {
            throw new ArgumentException("Value is required.", nameof(userAccountId));
        }

        Id = id;
        TenantId = tenantId;
        UserAccountId = userAccountId;
        EventType = NormalizeRequiredText(eventType, nameof(eventType));
        Severity = severity;
        DetailsJson = string.IsNullOrWhiteSpace(detailsJson) ? "{}" : detailsJson.Trim();
        OccurredAtUtc = occurredAtUtc;
    }

    public string DetailsJson { get; private set; } = "{}";

    public string EventType { get; private set; } = string.Empty;

    public Guid Id { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public AbuseEventSeverity Severity { get; private set; }

    public Guid? TenantId { get; private set; }

    public Guid UserAccountId { get; private set; }

    public static AbuseEvent Create(
        Guid? tenantId,
        Guid userAccountId,
        string eventType,
        AbuseEventSeverity severity,
        string detailsJson,
        DateTimeOffset occurredAtUtc)
    {
        return new AbuseEvent(
            Guid.CreateVersion7(),
            tenantId,
            userAccountId,
            eventType,
            severity,
            detailsJson,
            occurredAtUtc);
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
