namespace RezSaaS.Modules.Integrations.Domain;

public enum WebhookDeliveryStatus
{
    Pending,
    Processing,
    Delivered,
    Failed,
    Cancelled,
}
