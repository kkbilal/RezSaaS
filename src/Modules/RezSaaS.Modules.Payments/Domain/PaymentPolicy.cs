namespace RezSaaS.Modules.Payments.Domain;

public sealed class PaymentPolicy
{
    private PaymentPolicy()
    {
    }

    private PaymentPolicy(
        Guid id,
        Guid tenantId,
        Guid? branchId,
        PaymentCollectionMode mode,
        decimal? fixedAmount,
        decimal? percentage,
        string currencyCode,
        string providerKey,
        bool hostedCheckoutEnabled,
        Guid updatedByUserAccountId,
        DateTimeOffset updatedAtUtc)
    {
        RequireNonEmpty(tenantId, nameof(tenantId));
        RequireNonEmpty(updatedByUserAccountId, nameof(updatedByUserAccountId));

        Id = id;
        TenantId = tenantId;
        BranchId = branchId;
        UpdatedByUserAccountId = updatedByUserAccountId;
        UpdatedAtUtc = updatedAtUtc;

        Apply(
            mode,
            fixedAmount,
            percentage,
            currencyCode,
            providerKey,
            hostedCheckoutEnabled);
    }

    public Guid? BranchId { get; private set; }

    public string CurrencyCode { get; private set; } = "TRY";

    public decimal? FixedAmount { get; private set; }

    public bool HostedCheckoutEnabled { get; private set; }

    public Guid Id { get; private set; }

    public PaymentCollectionMode Mode { get; private set; } = PaymentCollectionMode.Disabled;

    public decimal? Percentage { get; private set; }

    public string ProviderKey { get; private set; } = string.Empty;

    public Guid TenantId { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public Guid UpdatedByUserAccountId { get; private set; }

    public static PaymentPolicy CreateDisabled(
        Guid tenantId,
        Guid? branchId,
        Guid actorUserAccountId,
        DateTimeOffset updatedAtUtc)
    {
        return new PaymentPolicy(
            Guid.CreateVersion7(),
            tenantId,
            branchId,
            PaymentCollectionMode.Disabled,
            fixedAmount: null,
            percentage: null,
            currencyCode: "TRY",
            providerKey: string.Empty,
            hostedCheckoutEnabled: false,
            actorUserAccountId,
            updatedAtUtc);
    }

    public void Configure(
        PaymentCollectionMode mode,
        decimal? fixedAmount,
        decimal? percentage,
        string currencyCode,
        string providerKey,
        bool hostedCheckoutEnabled,
        Guid actorUserAccountId,
        DateTimeOffset updatedAtUtc)
    {
        RequireNonEmpty(actorUserAccountId, nameof(actorUserAccountId));

        UpdatedByUserAccountId = actorUserAccountId;
        UpdatedAtUtc = updatedAtUtc;

        Apply(
            mode,
            fixedAmount,
            percentage,
            currencyCode,
            providerKey,
            hostedCheckoutEnabled);
    }

    private void Apply(
        PaymentCollectionMode mode,
        decimal? fixedAmount,
        decimal? percentage,
        string currencyCode,
        string providerKey,
        bool hostedCheckoutEnabled)
    {
        if (mode == PaymentCollectionMode.Disabled && hostedCheckoutEnabled)
        {
            throw new ArgumentException(
                "Hosted checkout cannot be enabled for disabled collection mode.",
                nameof(hostedCheckoutEnabled));
        }

        if (mode == PaymentCollectionMode.PayAtStore && hostedCheckoutEnabled)
        {
            throw new ArgumentException(
                "Pay-at-store mode cannot require hosted checkout.",
                nameof(hostedCheckoutEnabled));
        }

        if (mode == PaymentCollectionMode.Deposit && fixedAmount is null && percentage is null)
        {
            throw new ArgumentException(
                "Deposit mode requires either a fixed amount or a percentage.",
                nameof(mode));
        }

        if (mode == PaymentCollectionMode.FullPrepayment && (fixedAmount is not null || percentage is not null))
        {
            throw new ArgumentException(
                "Full prepayment cannot define deposit amount fields.",
                nameof(mode));
        }

        if (fixedAmount is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fixedAmount), "Amount cannot be negative.");
        }

        if (percentage is <= 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percentage), "Percentage must be between 0 and 100.");
        }

        string normalizedProviderKey = NormalizeOptionalText(providerKey, maxLength: 80);

        if (hostedCheckoutEnabled && string.IsNullOrWhiteSpace(normalizedProviderKey))
        {
            throw new ArgumentException(
                "Hosted checkout requires a provider key.",
                nameof(providerKey));
        }

        Mode = mode;
        FixedAmount = fixedAmount;
        Percentage = percentage;
        CurrencyCode = NormalizeCurrencyCode(currencyCode);
        ProviderKey = normalizedProviderKey;
        HostedCheckoutEnabled = hostedCheckoutEnabled;
    }

    private static string NormalizeCurrencyCode(string value)
    {
        string normalized = NormalizeRequiredText(value, nameof(value)).ToUpperInvariant();

        if (normalized.Length != 3)
        {
            throw new ArgumentException("Currency code must be ISO-4217 length.", nameof(value));
        }

        return normalized;
    }

    private static string NormalizeRequiredText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }

    private static string NormalizeOptionalText(string? value, int maxLength)
    {
        string normalized = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException("Value is too long.", nameof(value));
        }

        return normalized;
    }

    private static void RequireNonEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value is required.", parameterName);
        }
    }
}
