namespace RezSaaS.Modules.Payments.Domain;

public enum PaymentIntentStatus
{
    PendingCheckout,
    CheckoutCreated,
    Paid,
    Failed,
    Cancelled,
    Expired,
    Refunded,
}
