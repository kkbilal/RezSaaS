namespace RezSaaS.Modules.TenantManagement.Domain;

public sealed class TenantAuditLogEntry
{
    private TenantAuditLogEntry()
    {
    }

    private TenantAuditLogEntry(
        Guid id,
        Guid tenantId,
        Guid? actorUserAccountId,
        string action,
        string detailsJson,
        DateTimeOffset occurredAtUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Action is required.", nameof(action));
        }

        Id = id;
        Action = action.Trim();
        ActorUserAccountId = actorUserAccountId;
        DetailsJson = string.IsNullOrWhiteSpace(detailsJson) ? "{}" : detailsJson.Trim();
        OccurredAtUtc = occurredAtUtc;
        TenantId = tenantId;
    }

    public string Action { get; private set; } = string.Empty;

    public Guid? ActorUserAccountId { get; private set; }

    public string DetailsJson { get; private set; } = "{}";

    public Guid Id { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public Guid TenantId { get; private set; }

    public static TenantAuditLogEntry Create(
        Guid tenantId,
        Guid? actorUserAccountId,
        string action,
        string detailsJson,
        DateTimeOffset occurredAtUtc)
    {
        return new TenantAuditLogEntry(
            Guid.CreateVersion7(),
            tenantId,
            actorUserAccountId,
            action,
            detailsJson,
            occurredAtUtc);
    }
}
