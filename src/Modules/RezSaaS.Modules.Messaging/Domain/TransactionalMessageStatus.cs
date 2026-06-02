namespace RezSaaS.Modules.Messaging.Domain;

public enum TransactionalMessageStatus
{
    Pending,
    Sent,
    Failed,
    Cancelled,
}
