namespace RezSaaS.Modules.Identity.Domain;

public sealed class IdentityAuditLogEntry
{
    private IdentityAuditLogEntry()
    {
    }

    private IdentityAuditLogEntry(
        Guid id,
        Guid? actorUserAccountId,
        Guid? subjectUserAccountId,
        string action,
        string detailsJson,
        DateTimeOffset occurredAtUtc)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Action is required.", nameof(action));
        }

        Id = id;
        ActorUserAccountId = actorUserAccountId;
        SubjectUserAccountId = subjectUserAccountId;
        Action = action.Trim();
        DetailsJson = string.IsNullOrWhiteSpace(detailsJson) ? "{}" : detailsJson.Trim();
        OccurredAtUtc = occurredAtUtc;
    }

    public string Action { get; private set; } = string.Empty;

    public Guid? ActorUserAccountId { get; private set; }

    public string DetailsJson { get; private set; } = "{}";

    public Guid Id { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public Guid? SubjectUserAccountId { get; private set; }

    public static IdentityAuditLogEntry Create(
        Guid? actorUserAccountId,
        Guid? subjectUserAccountId,
        string action,
        string detailsJson,
        DateTimeOffset occurredAtUtc)
    {
        return new IdentityAuditLogEntry(
            Guid.CreateVersion7(),
            actorUserAccountId,
            subjectUserAccountId,
            action,
            detailsJson,
            occurredAtUtc);
    }
}
