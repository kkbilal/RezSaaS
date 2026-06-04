using Microsoft.EntityFrameworkCore;
using RezSaaS.Modules.Messaging.Domain;
using RezSaaS.Modules.Messaging.Infrastructure.Persistence;

namespace RezSaaS.Modules.Messaging.Application;

public sealed class PlatformNotificationReconciliationQueryService
{
    private readonly MessagingDbContext dbContext;

    public PlatformNotificationReconciliationQueryService(MessagingDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<PlatformNotificationReconciliationSnapshot> InspectAsync(
        PlatformNotificationReconciliationQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(query.SampleSize);
        int sampleSize = Math.Min(query.SampleSize, 100);
        IQueryable<PlatformTransactionalMessage> messages =
            dbContext.PlatformTransactionalMessages.AsNoTracking();
        IQueryable<PlatformTransactionalMessage> failed = messages
            .Where(entity => entity.Status == PlatformTransactionalMessageStatus.Failed);
        IQueryable<PlatformTransactionalMessage> staleProcessing = messages
            .Where(entity => entity.Status == PlatformTransactionalMessageStatus.Processing
                && entity.LockedUntilUtc != null
                && entity.LockedUntilUtc <= query.StaleProcessingBeforeUtc);
        IQueryable<PlatformTransactionalMessage> callbackPending = messages
            .Where(entity => entity.SentAtUtc != null
                && entity.SentAtUtc <= query.CallbackPendingBeforeUtc
                && entity.Status != PlatformTransactionalMessageStatus.Sent
                && entity.Status != PlatformTransactionalMessageStatus.Cancelled);

        int failedCount = await failed.CountAsync(cancellationToken);
        int staleProcessingCount = await staleProcessing.CountAsync(cancellationToken);
        int callbackPendingCount = await callbackPending.CountAsync(cancellationToken);
        List<Guid> failedMessageIds = await failed
            .OrderBy(entity => entity.CompletedAtUtc)
            .Take(sampleSize)
            .Select(entity => entity.Id)
            .ToListAsync(cancellationToken);
        List<Guid> staleProcessingMessageIds = await staleProcessing
            .OrderBy(entity => entity.LockedUntilUtc)
            .Take(sampleSize)
            .Select(entity => entity.Id)
            .ToListAsync(cancellationToken);
        List<Guid> callbackPendingMessageIds = await callbackPending
            .OrderBy(entity => entity.SentAtUtc)
            .Take(sampleSize)
            .Select(entity => entity.Id)
            .ToListAsync(cancellationToken);

        return new PlatformNotificationReconciliationSnapshot(
            failedCount,
            staleProcessingCount,
            callbackPendingCount,
            failedMessageIds,
            staleProcessingMessageIds,
            callbackPendingMessageIds);
    }
}
