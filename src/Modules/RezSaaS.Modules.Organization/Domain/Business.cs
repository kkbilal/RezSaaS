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
        DateTimeOffset createdAtUtc,
        string description)
    {
        RequireNonEmpty(tenantId, nameof(tenantId));

        Id = id;
        TenantId = tenantId;
        Slug = NormalizeRequiredText(slug, nameof(slug));
        NormalizedSlug = Slug.ToUpperInvariant();
        DisplayName = NormalizeRequiredText(displayName, nameof(displayName));
        CategoryKey = NormalizeRequiredText(categoryKey, nameof(categoryKey));
        CreatedAtUtc = createdAtUtc;
        Description = NormalizeOptionalText(description);
        SeoTitle = DisplayName;
    }

    public string CategoryKey { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public Guid Id { get; private set; }

    public string NormalizedSlug { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public BusinessStatus Status { get; private set; } = BusinessStatus.Active;

    public Guid TenantId { get; private set; }

    public string PublicRules { get; private set; } = string.Empty;

    public PublicStaffDisplayPolicy PublicStaffDisplayPolicy { get; private set; } =
        PublicStaffDisplayPolicy.ShowNames;

    public decimal RatingAverage { get; private set; }

    public int ReviewCount { get; private set; }

    public string SeoDescription { get; private set; } = string.Empty;

    public string SeoTitle { get; private set; } = string.Empty;

    public static Business Create(
        Guid tenantId,
        string slug,
        string displayName,
        string categoryKey,
        DateTimeOffset createdAtUtc,
        string description = "")
    {
        return new Business(
            Guid.CreateVersion7(),
            tenantId,
            slug,
            displayName,
            categoryKey,
            createdAtUtc,
            description);
    }

    public void Rename(string displayName)
    {
        DisplayName = NormalizeRequiredText(displayName, nameof(displayName));
    }

    public void UpdateDescription(string description)
    {
        Description = NormalizeOptionalText(description);
    }

    public void UpdatePublicProfile(
        string publicRules,
        string seoTitle,
        string seoDescription,
        PublicStaffDisplayPolicy staffDisplayPolicy)
    {
        PublicRules = NormalizeOptionalText(publicRules);
        SeoTitle = NormalizeOptionalText(seoTitle);
        SeoDescription = NormalizeOptionalText(seoDescription);
        PublicStaffDisplayPolicy = staffDisplayPolicy;
    }

    public void UpdateRatingSummary(decimal ratingAverage, int reviewCount)
    {
        if (ratingAverage is < 0 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(ratingAverage), "Rating must be between 0 and 5.");
        }

        if (reviewCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reviewCount), "Review count cannot be negative.");
        }

        RatingAverage = ratingAverage;
        ReviewCount = reviewCount;
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

    private static string NormalizeOptionalText(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static void RequireNonEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value is required.", parameterName);
        }
    }
}
