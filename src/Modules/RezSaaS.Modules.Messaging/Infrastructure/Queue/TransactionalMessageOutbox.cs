using RezSaaS.BuildingBlocks.Messaging;
using RezSaaS.Modules.Messaging.Domain;
using RezSaaS.Modules.Messaging.Infrastructure.Persistence;

namespace RezSaaS.Modules.Messaging.Infrastructure.Queue;

public sealed class TransactionalMessageOutbox : ITransactionalMessageOutbox
{
    private readonly MessagingDbContext dbContext;

    public TransactionalMessageOutbox(MessagingDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<Guid> EnqueueAsync(
        TransactionalMessageEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        TransactionalMessage message = TransactionalMessage.Create(
            envelope.TenantId,
            MapChannel(envelope.Channel),
            envelope.RecipientMasked,
            envelope.TemplateKey,
            envelope.PayloadJson,
            envelope.CreatedAtUtc);

        dbContext.TransactionalMessages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);

        return message.Id;
    }

    private static MessageChannel MapChannel(TransactionalMessageChannel channel)
    {
        return channel switch
        {
            TransactionalMessageChannel.Email => MessageChannel.Email,
            TransactionalMessageChannel.Sms => MessageChannel.Sms,
            TransactionalMessageChannel.WhatsApp => MessageChannel.WhatsApp,
            _ => MessageChannel.Email,
        };
    }
}
