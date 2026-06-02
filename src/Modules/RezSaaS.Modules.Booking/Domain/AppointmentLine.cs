namespace RezSaaS.Modules.Booking.Domain;

public sealed class AppointmentLine
{
    private AppointmentLine()
    {
    }

    private AppointmentLine(
        Guid id,
        Guid tenantId,
        Guid appointmentId,
        Guid serviceVariantId,
        string serviceNameSnapshot,
        int durationMinutes,
        decimal priceAmount,
        string currencyCode)
    {
        RequireNonEmpty(tenantId, nameof(tenantId));
        RequireNonEmpty(appointmentId, nameof(appointmentId));
        RequireNonEmpty(serviceVariantId, nameof(serviceVariantId));

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
        AppointmentId = appointmentId;
        ServiceVariantId = serviceVariantId;
        ServiceNameSnapshot = NormalizeRequiredText(serviceNameSnapshot, nameof(serviceNameSnapshot));
        DurationMinutes = durationMinutes;
        PriceAmount = priceAmount;
        CurrencyCode = NormalizeRequiredText(currencyCode, nameof(currencyCode)).ToUpperInvariant();
    }

    public Guid AppointmentId { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public int DurationMinutes { get; private set; }

    public Guid Id { get; private set; }

    public decimal PriceAmount { get; private set; }

    public string ServiceNameSnapshot { get; private set; } = string.Empty;

    public Guid ServiceVariantId { get; private set; }

    public Guid TenantId { get; private set; }

    public static AppointmentLine Create(
        Guid tenantId,
        Guid appointmentId,
        Guid serviceVariantId,
        string serviceNameSnapshot,
        int durationMinutes,
        decimal priceAmount,
        string currencyCode)
    {
        return new AppointmentLine(
            Guid.CreateVersion7(),
            tenantId,
            appointmentId,
            serviceVariantId,
            serviceNameSnapshot,
            durationMinutes,
            priceAmount,
            currencyCode);
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
