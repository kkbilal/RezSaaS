namespace RezSaaS.Modules.Payments.Application;

public interface IPaymentProviderAdapter
{
    Task<HostedCheckoutResult> CreateCheckoutSessionAsync(
        CreateCheckoutCommand command,
        CancellationToken cancellationToken = default);

    Task<WebhookProcessingResult> ProcessWebhookEventAsync(
        ProcessWebhookCommand command,
        CancellationToken cancellationToken = default);

    Task<PaymentStatus> GetPaymentStatusAsync(
        string providerReference,
        CancellationToken cancellationToken = default);
}

public sealed record CreateCheckoutCommand(
    Guid PaymentIntentId,
    decimal Amount,
    string CurrencyCode,
    string Description,
    string SuccessReturnUrl,
    string CancelReturnUrl);

public sealed record HostedCheckoutResult(
    string ProviderReference,
    string CheckoutUrl);

public sealed record ProcessWebhookCommand(
    string EventId,
    string EventType,
    byte[] RawPayload,
    string Signature);

public sealed record WebhookProcessingResult(
    WebhookProcessingStatus Status,
    string? ProviderReference = null,
    string? Error = null);

public enum WebhookProcessingStatus
{
    Success,
    Failed,
    Ignored
}

public enum PaymentStatus
{
    Pending,
    Succeeded,
    Failed,
    Canceled,
    RequiresAction
}