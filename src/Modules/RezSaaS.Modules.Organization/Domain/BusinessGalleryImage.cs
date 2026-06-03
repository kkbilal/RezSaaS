namespace RezSaaS.Modules.Organization.Domain;

public sealed class BusinessGalleryImage
{
    private BusinessGalleryImage()
    {
    }

    private BusinessGalleryImage(
        Guid id,
        Guid tenantId,
        Guid businessId,
        string imageUrl,
        string altText,
        int sortOrder,
        DateTimeOffset createdAtUtc)
    {
        RequireNonEmpty(tenantId, nameof(tenantId));
        RequireNonEmpty(businessId, nameof(businessId));

        Id = id;
        TenantId = tenantId;
        BusinessId = businessId;
        ImageUrl = NormalizeRequiredText(imageUrl, nameof(imageUrl));
        AltText = NormalizeOptionalText(altText);
        SortOrder = sortOrder;
        CreatedAtUtc = createdAtUtc;
    }

    public string AltText { get; private set; } = string.Empty;

    public Business? Business { get; private set; }

    public Guid BusinessId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid Id { get; private set; }

    public string ImageUrl { get; private set; } = string.Empty;

    public bool IsPublished { get; private set; } = true;

    public int SortOrder { get; private set; }

    public Guid TenantId { get; private set; }

    public static BusinessGalleryImage Create(
        Guid tenantId,
        Guid businessId,
        string imageUrl,
        DateTimeOffset createdAtUtc,
        string altText = "",
        int sortOrder = 0)
    {
        return new BusinessGalleryImage(
            Guid.CreateVersion7(),
            tenantId,
            businessId,
            imageUrl,
            altText,
            sortOrder,
            createdAtUtc);
    }

    public void Unpublish()
    {
        IsPublished = false;
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
