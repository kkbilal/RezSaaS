namespace RezSaaS.Modules.Resources.Domain;

public sealed class Resource
{
    private Resource()
    {
    }

    private Resource(Guid id, Guid tenantId, Guid branchId, Guid resourceTypeId, string displayName)
    {
        RequireNonEmpty(tenantId, nameof(tenantId));
        RequireNonEmpty(branchId, nameof(branchId));
        RequireNonEmpty(resourceTypeId, nameof(resourceTypeId));

        Id = id;
        TenantId = tenantId;
        BranchId = branchId;
        ResourceTypeId = resourceTypeId;
        DisplayName = NormalizeRequiredText(displayName, nameof(displayName));
    }

    public Guid BranchId { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public Guid Id { get; private set; }

    public ResourceStatus Status { get; private set; } = ResourceStatus.Active;

    public Guid ResourceTypeId { get; private set; }

    public Guid TenantId { get; private set; }

    public static Resource Create(Guid tenantId, Guid branchId, Guid resourceTypeId, string displayName)
    {
        return new Resource(Guid.CreateVersion7(), tenantId, branchId, resourceTypeId, displayName);
    }

    public void MarkOutOfService()
    {
        Status = ResourceStatus.OutOfService;
    }

    public void Restore()
    {
        Status = ResourceStatus.Active;
    }

    public void Rename(string displayName)
    {
        DisplayName = NormalizeRequiredText(displayName, nameof(displayName));
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
