namespace RezSaaS.Modules.Resources.Domain;

public sealed class ResourceBlock
{
    private ResourceBlock()
    {
    }

    private ResourceBlock(
        Guid id,
        Guid tenantId,
        Guid resourceId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        string reason)
    {
        RequireNonEmpty(tenantId, nameof(tenantId));
        RequireNonEmpty(resourceId, nameof(resourceId));

        if (endUtc <= startUtc)
        {
            throw new ArgumentException("End must be later than start.", nameof(endUtc));
        }

        Id = id;
        TenantId = tenantId;
        ResourceId = resourceId;
        StartUtc = startUtc;
        EndUtc = endUtc;
        Reason = NormalizeRequiredText(reason, nameof(reason));
    }

    public DateTimeOffset EndUtc { get; private set; }

    public Guid Id { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public Guid ResourceId { get; private set; }

    public DateTimeOffset StartUtc { get; private set; }

    public Guid TenantId { get; private set; }

    public static ResourceBlock Create(
        Guid tenantId,
        Guid resourceId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        string reason)
    {
        return new ResourceBlock(Guid.CreateVersion7(), tenantId, resourceId, startUtc, endUtc, reason);
    }

    private static string NormalizeRequiredText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }

    private static void RequireNonEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value is required.", parameterName);
        }
    }
}
