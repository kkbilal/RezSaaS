namespace RezSaaS.Modules.Organization.Domain;

public sealed class Business
{
    private Business()
    {
    }

    private Business(
        Guid id,
        Guid tenantId,
        string slug,
        string displayName,
        string categoryKey,
        DateTimeOffset createdAtUtc)
    {
        RequireNonEmpty(tenantId, nameof(tenantId));

        Id = id;
        TenantId = tenantId;
        Slug = NormalizeRequiredText(slug, nameof(slug));
        NormalizedSlug = Slug.ToUpperInvariant();
        DisplayName = NormalizeRequiredText(displayName, nameof(displayName));
        CategoryKey = NormalizeRequiredText(categoryKey, nameof(categoryKey));
        CreatedAtUtc = createdAtUtc;
    }

    public string CategoryKey { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public Guid Id { get; private set; }

    public string NormalizedSlug { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public BusinessStatus Status { get; private set; } = BusinessStatus.Active;

    public Guid TenantId { get; private set; }

    public static Business Create(
        Guid tenantId,
        string slug,
        string displayName,
        string categoryKey,
        DateTimeOffset createdAtUtc)
    {
        return new Business(
            Guid.CreateVersion7(),
            tenantId,
            slug,
            displayName,
            categoryKey,
            createdAtUtc);
    }

    public void Rename(string displayName)
    {
        DisplayName = NormalizeRequiredText(displayName, nameof(displayName));
    }

    public void Suspend()
    {
        Status = BusinessStatus.Suspended;
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
