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

    public void Rename(string name)
    {
        Name = NormalizeRequiredText(name, nameof(name));
        NormalizedName = Name.ToUpperInvariant();
    }

    /// <summary>
    /// Katalogda desteklenen para birimleri.
    /// </summary>
    /// <remarks>
    /// NEDEN WHITELIST VAR:
    /// CurrencyCode serbest 3-karakter string'di ve HICBIR dogrulamasi yoktu. Yani ayni
    /// isletmenin katalogu "Sac Kesimi 400 TRY" + "Boya 800 USD" gibi KARISIK PARA BIRIMINE
    /// dusebiliyordu -- fiyat karsilastirmasi, toplam, raporlama hepsi anlamsizlasirdi.
    /// (Uctan uca duman testi bunu kanitladi: USD ile varyant olusturulabiliyordu.)
    ///
    /// "UI'da para birimi secici koymayiz" bir KURAL KONTROLU DEGILDIR -- API dogrudan
    /// cagrilabilir. Kisit DOMAIN'de olmali.
    ///
    /// Su an tek para birimi TRY: urun Turkiye-first ve Payments modulu de TRY'yi sabit
    /// kodluyor. Uluslararasi acilimda buraya eklenir.
    /// </remarks>
    public static readonly IReadOnlySet<string> SupportedCurrencyCodes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "TRY" };

    public static bool IsSupportedCurrency(string? currencyCode)
    {
        return !string.IsNullOrWhiteSpace(currencyCode)
            && SupportedCurrencyCodes.Contains(currencyCode.Trim());
    }

    /// <summary>
    /// Varyantin fiyatini ve para birimini gunceller.
    /// </summary>
    /// <remarks>
    /// BUG FIX: bu metot ONCEDEN yalnizca priceAmount aliyordu. ServiceVariantManagementService
    /// ise CurrencyCode'u DOGRULUYOR (bos olamaz) ama UYGULAYACAK BIR METOT OLMADIGI ICIN
    /// ASLA YAZMIYORDU. Yani istek 200 OK donuyor, para birimi degismiyordu -- sozlesme
    /// "bu alan zorunlu" diyordu ama alan ETKISIZDI.
    ///
    /// Bu, kod tabanindaki UCUNCU ayni tur SESSIZ NO-OP idi:
    ///   1) StaffMember: Rename metodu YOKTU -> isim degismiyordu
    ///   2) Service: Archive() vardi ama cagrilmiyordu -> KALICI SILINIYORDU
    ///   3) ServiceVariant: para birimi dogrulaniyor ama uygulanmiyordu
    /// Ucunde de servis "basarili" diyor, audit "guncellendi" yaziyordu.
    /// </remarks>
    public void UpdatePricing(decimal priceAmount, string currencyCode)
    {
        if (priceAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(priceAmount), "Price cannot be negative.");
        }

        if (!IsSupportedCurrency(currencyCode))
        {
            throw new ArgumentException(
                $"Unsupported currency code '{currencyCode}'. Supported: {string.Join(", ", SupportedCurrencyCodes)}.",
                nameof(currencyCode));
        }

        PriceAmount = priceAmount;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
    }

    public void UpdateDuration(int durationMinutes)
    {
        if (durationMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationMinutes), "Duration must be greater than zero.");
        }

        DurationMinutes = durationMinutes;
    }

    public void UpdateResourceType(Guid? requiredResourceTypeId)
    {
        RequiredResourceTypeId = requiredResourceTypeId;
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
