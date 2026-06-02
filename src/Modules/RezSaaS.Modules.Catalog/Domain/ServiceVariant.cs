namespace RezSaaS.Modules.Catalog.Domain;

public sealed class ServiceVariant
{
    private ServiceVariant()
    {
    }

    private ServiceVariant(
        Guid id,
        Guid tenantId,
        Guid serviceId,
        string name,
        int durationMinutes,
        decimal priceAmount,
        string currencyCode,
        DateTimeOffset createdAtUtc,
        Guid? requiredResourceTypeId)
    {
        RequireNonEmpty(tenantId, nameof(tenantId));
        RequireNonEmpty(serviceId, nameof(serviceId));

        if (durationMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationMinutes), "Duration must be greater than zero.");
        }

        if (priceAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(priceAmount), "Price cannot be negative.");
        }

        Id = id;
        TenantId = tenantId;
        ServiceId = serviceId;
        Name = NormalizeRequiredText(name, nameof(name));
        NormalizedName = Name.ToUpperInvariant();
        DurationMinutes = durationMinutes;
        PriceAmount = priceAmount;
        CurrencyCode = NormalizeRequiredText(currencyCode, nameof(currencyCode)).ToUpperInvariant();
        CreatedAtUtc = createdAtUtc;
        RequiredResourceTypeId = requiredResourceTypeId;
    }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public int DurationMinutes { get; private set; }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public decimal PriceAmount { get; private set; }

    public Guid? RequiredResourceTypeId { get; private set; }

    public Service? Service { get; private set; }

    public Guid ServiceId { get; private set; }

    public Guid TenantId { get; private set; }

    public static ServiceVariant Create(
        Guid tenantId,
        Guid serviceId,
        string name,
        int durationMinutes,
        decimal priceAmount,
        string currencyCode,
        DateTimeOffset createdAtUtc,
        Guid? requiredResourceTypeId = null)
    {
        return new ServiceVariant(
            Guid.CreateVersion7(),
            tenantId,
            serviceId,
            name,
            durationMinutes,
            priceAmount,
            currencyCode,
            createdAtUtc,
            requiredResourceTypeId);
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
