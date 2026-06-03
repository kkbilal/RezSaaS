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
        DateTimeOffset createdAtUtc,
        string city,
        string district,
        string addressLine)
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
        SetLocation(city, district, addressLine);
    }

    public string AddressLine { get; private set; } = string.Empty;

    public Business? Business { get; private set; }

    public Guid BusinessId { get; private set; }

    public string City { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public string District { get; private set; } = string.Empty;

    public Guid Id { get; private set; }

    public string NormalizedSlug { get; private set; } = string.Empty;

    public string NormalizedCity { get; private set; } = string.Empty;

    public string NormalizedDistrict { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public Guid TenantId { get; private set; }

    public int? MaxPublicSlots { get; private set; }

    public int? SlotIntervalMinutes { get; private set; }

    public string TimeZoneId { get; private set; } = string.Empty;

    public static Branch Create(
        Guid tenantId,
        Guid businessId,
        string slug,
        string displayName,
        string timeZoneId,
        DateTimeOffset createdAtUtc,
        string city = "",
        string district = "",
        string addressLine = "")
    {
        return new Branch(
            Guid.CreateVersion7(),
            tenantId,
            businessId,
            slug,
            displayName,
            timeZoneId,
            createdAtUtc,
            city,
            district,
            addressLine);
    }

    public void SetLocation(
        string city,
        string district,
        string addressLine)
    {
        City = NormalizeOptionalText(city);
        District = NormalizeOptionalText(district);
        AddressLine = NormalizeOptionalText(addressLine);
        NormalizedCity = City.ToUpperInvariant();
        NormalizedDistrict = District.ToUpperInvariant();
    }

    public void SetPublicSlotSettings(int? slotIntervalMinutes, int? maxPublicSlots)
    {
        if (slotIntervalMinutes is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slotIntervalMinutes),
                "Slot interval must be positive when set.");
        }

        if (maxPublicSlots is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxPublicSlots),
                "Max public slots must be positive when set.");
        }

        SlotIntervalMinutes = slotIntervalMinutes;
        MaxPublicSlots = maxPublicSlots;
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
