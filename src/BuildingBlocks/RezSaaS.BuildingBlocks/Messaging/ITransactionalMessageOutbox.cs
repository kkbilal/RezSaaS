namespace RezSaaS.BuildingBlocks.Messaging;

public interface ITransactionalMessageOutbox
{
    Task<Guid> EnqueueAsync(
        TransactionalMessageEnvelope envelope,
        CancellationToken cancellationToken = default);
}
