namespace RezSaaS.Modules.Payments.Domain;

public enum PaymentWebhookEventStatus
{
    Received,
    Processing,
    Processed,
    Failed,
    Ignored,
}
