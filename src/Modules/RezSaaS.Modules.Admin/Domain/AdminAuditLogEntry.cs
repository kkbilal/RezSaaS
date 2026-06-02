namespace RezSaaS.Modules.Admin.Domain;

public sealed class AdminAuditLogEntry
{
    private AdminAuditLogEntry()
    {
    }

    private AdminAuditLogEntry(
        Guid id,
        Guid actorUserAccountId,
        string action,
        string detailsJson,
        DateTimeOffset occurredAtUtc)
    {
        if (actorUserAccountId == Guid.Empty)
        {
            throw new ArgumentException("Value is required.", nameof(actorUserAccountId));
        }

        Id = id;
        ActorUserAccountId = actorUserAccountId;
        Action = NormalizeRequiredText(action, nameof(action));
        DetailsJson = string.IsNullOrWhiteSpace(detailsJson) ? "{}" : detailsJson.Trim();
        OccurredAtUtc = occurredAtUtc;
    }

    public string Action { get; private set; } = string.Empty;

    public Guid ActorUserAccountId { get; private set; }

    public string DetailsJson { get; private set; } = "{}";

    public Guid Id { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static AdminAuditLogEntry Create(
        Guid actorUserAccountId,
        string action,
        string detailsJson,
        DateTimeOffset occurredAtUtc)
    {
        return new AdminAuditLogEntry(
            Guid.CreateVersion7(),
            actorUserAccountId,
            action,
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
