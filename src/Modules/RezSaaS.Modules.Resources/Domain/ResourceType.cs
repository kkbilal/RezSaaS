namespace RezSaaS.Modules.Resources.Domain;

public sealed class ResourceType
{
    private ResourceType()
    {
    }

    private ResourceType(Guid id, Guid tenantId, string key, string displayName)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Value is required.", nameof(tenantId));
        }

        Id = id;
        TenantId = tenantId;
        Key = NormalizeRequiredText(key, nameof(key));
        NormalizedKey = Key.ToUpperInvariant();
        DisplayName = NormalizeRequiredText(displayName, nameof(displayName));
    }

    public string DisplayName { get; private set; } = string.Empty;

    public Guid Id { get; private set; }

    public string Key { get; private set; } = string.Empty;

    public string NormalizedKey { get; private set; } = string.Empty;

    public Guid TenantId { get; private set; }

    public static ResourceType Create(Guid tenantId, string key, string displayName)
    {
        return new ResourceType(Guid.CreateVersion7(), tenantId, key, displayName);
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
}
