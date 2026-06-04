namespace RezSaaS.Modules.Messaging.Domain;

public enum PlatformTransactionalMessageStatus
{
    Pending,
    Processing,
    Sent,
    Failed,
    Cancelled,
}
