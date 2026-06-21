namespace RezSaaS.Modules.Catalog.Domain;

public sealed class Service
{
    private Service()
    {
    }

    private Service(Guid id, Guid tenantId, string name, string categoryKey, DateTimeOffset createdAtUtc)
    {
        RequireNonEmpty(tenantId, nameof(tenantId));

        Id = id;
        TenantId = tenantId;
        Name = NormalizeRequiredText(name, nameof(name));
        NormalizedName = Name.ToUpperInvariant();
        CategoryKey = NormalizeRequiredText(categoryKey, nameof(categoryKey));
        CreatedAtUtc = createdAtUtc;
    }

    public string CategoryKey { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public ServiceStatus Status { get; private set; } = ServiceStatus.Active;

    public Guid TenantId { get; private set; }

    public static Service Create(Guid tenantId, string name, string categoryKey, DateTimeOffset createdAtUtc)
    {
        return new Service(Guid.CreateVersion7(), tenantId, name, categoryKey, createdAtUtc);
    }

    public void Rename(string name)
    {
        Name = NormalizeRequiredText(name, nameof(name));
        NormalizedName = Name.ToUpperInvariant();
    }

    public void UpdateCategory(string categoryKey)
    {
        CategoryKey = NormalizeRequiredText(categoryKey, nameof(categoryKey));
    }

    public void Archive()
    {
        Status = ServiceStatus.Archived;
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
