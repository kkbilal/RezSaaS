namespace RezSaaS.Modules.Payments.Domain;

public sealed class PaymentIntent
{
    private PaymentIntent()
    {
    }

    private PaymentIntent(
        Guid id,
        Guid tenantId,
        Guid customerUserAccountId,
        Guid? appointmentRequestId,
        Guid? appointmentId,
        PaymentIntentPurpose purpose,
        decimal amount,
        string currencyCode,
        string providerKey,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? expiresAtUtc)
    {
        RequireNonEmpty(tenantId, nameof(tenantId));
        RequireNonEmpty(customerUserAccountId, nameof(customerUserAccountId));

        Id = id;
        TenantId = tenantId;
        CustomerUserAccountId = customerUserAccountId;
        AppointmentRequestId = appointmentRequestId;
        AppointmentId = appointmentId;
        Purpose = purpose;
        Amount = NormalizeAmount(amount);
        CurrencyCode = NormalizeCurrencyCode(currencyCode);
        ProviderKey = NormalizeRequiredText(providerKey, nameof(providerKey), maxLength: 80);
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        Status = PaymentIntentStatus.PendingCheckout;
    }

    public decimal Amount { get; private set; }

    public Guid? AppointmentId { get; private set; }

    public Guid? AppointmentRequestId { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string CurrencyCode { get; private set; } = "TRY";

    public Guid CustomerUserAccountId { get; private set; }

    public DateTimeOffset? ExpiresAtUtc { get; private set; }

    public Guid Id { get; private set; }

    public string ProviderCheckoutUrl { get; private set; } = string.Empty;

    public string ProviderKey { get; private set; } = string.Empty;

    public string ProviderReference { get; private set; } = string.Empty;

    public PaymentIntentPurpose Purpose { get; private set; }

    public PaymentIntentStatus Status { get; private set; }

    public Guid TenantId { get; private set; }

    public static PaymentIntent CreateHostedCheckout(
        Guid tenantId,
        Guid customerUserAccountId,
        Guid? appointmentRequestId,
        Guid? appointmentId,
        PaymentIntentPurpose purpose,
        decimal amount,
        string currencyCode,
        string providerKey,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? expiresAtUtc)
    {
        if (appointmentRequestId is null && appointmentId is null)
        {
            throw new ArgumentException(
                "Payment intent must reference an appointment request or appointment.",
                nameof(appointmentRequestId));
        }

        return new PaymentIntent(
            Guid.CreateVersion7(),
            tenantId,
            customerUserAccountId,
            appointmentRequestId,
            appointmentId,
            purpose,
            amount,
            currencyCode,
            providerKey,
            createdAtUtc,
            expiresAtUtc);
    }

    public void AttachCheckout(
        string providerReference,
        string providerCheckoutUrl)
    {
        if (Status != PaymentIntentStatus.PendingCheckout)
        {
            throw new InvalidOperationException("Checkout can only be attached to pending intents.");
        }

        ProviderReference = NormalizeRequiredText(
            providerReference,
            nameof(providerReference),
            maxLength: 180);
        ProviderCheckoutUrl = NormalizeRequiredText(
            providerCheckoutUrl,
            nameof(providerCheckoutUrl),
            maxLength: 1_000);
        Status = PaymentIntentStatus.CheckoutCreated;
    }

    public void MarkPaid(DateTimeOffset paidAtUtc)
    {
        if (Status is PaymentIntentStatus.Cancelled or PaymentIntentStatus.Expired or PaymentIntentStatus.Refunded)
        {
            throw new InvalidOperationException("Terminal payment intent cannot be marked paid.");
        }

        Status = PaymentIntentStatus.Paid;
        CompletedAtUtc = paidAtUtc;
    }

    public void MarkFailed(DateTimeOffset failedAtUtc)
    {
        if (Status == PaymentIntentStatus.Paid)
        {
            throw new InvalidOperationException("Paid payment intent cannot be marked failed.");
        }

        Status = PaymentIntentStatus.Failed;
        CompletedAtUtc = failedAtUtc;
    }

    private static decimal NormalizeAmount(decimal value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Amount must be positive.");
        }

        return decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    private static string NormalizeCurrencyCode(string value)
    {
        string normalized = NormalizeRequiredText(value, nameof(value), maxLength: 3).ToUpperInvariant();

        if (normalized.Length != 3)
        {
            throw new ArgumentException("Currency code must be ISO-4217 length.", nameof(value));
        }

        return normalized;
    }

    private static string NormalizeRequiredText(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        string normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException("Value is too long.", parameterName);
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
