namespace RezSaaS.Modules.Organization.Domain;

public sealed class Branch
{
    private Branch()
    {
    }

    private Branch(
        Guid id,
        Guid tenantId,
        Guid businessId,
        string slug,
        string displayName,
        string timeZoneId,
        DateTimeOffset createdAtUtc)
    {
        RequireNonEmpty(tenantId, nameof(tenantId));
        RequireNonEmpty(businessId, nameof(businessId));

        Id = id;
        TenantId = tenantId;
        BusinessId = businessId;
        Slug = NormalizeRequiredText(slug, nameof(slug));
        NormalizedSlug = Slug.ToUpperInvariant();
        DisplayName = NormalizeRequiredText(displayName, nameof(displayName));
        TimeZoneId = NormalizeRequiredText(timeZoneId, nameof(timeZoneId));
        CreatedAtUtc = createdAtUtc;
    }

    public Business? Business { get; private set; }

    public Guid BusinessId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public Guid Id { get; private set; }

    public string NormalizedSlug { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public Guid TenantId { get; private set; }

    public string TimeZoneId { get; private set; } = string.Empty;

    public static Branch Create(
        Guid tenantId,
        Guid businessId,
        string slug,
        string displayName,
        string timeZoneId,
        DateTimeOffset createdAtUtc)
    {
        return new Branch(
            Guid.CreateVersion7(),
            tenantId,
            businessId,
            slug,
            displayName,
            timeZoneId,
            createdAtUtc);
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
