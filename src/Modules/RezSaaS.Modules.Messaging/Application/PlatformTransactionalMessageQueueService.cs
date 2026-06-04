using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RezSaaS.Modules.Messaging.Domain;
using RezSaaS.Modules.Messaging.Infrastructure.Persistence;

namespace RezSaaS.Modules.Messaging.Application;

public sealed class PlatformTransactionalMessageQueueService
{
    private readonly MessagingDbContext dbContext;
    private readonly TimeProvider timeProvider;

    public PlatformTransactionalMessageQueueService(
        MessagingDbContext dbContext,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.timeProvider = timeProvider;
    }

    public async Task<Guid> EnqueueAsync(
        PlatformTransactionalMessageEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        PlatformTransactionalMessage message = PlatformTransactionalMessage.Create(
            envelope.UserAccountId,
            envelope.Purpose,
            envelope.CorrelationId,
            envelope.DeliveryKey,
            envelope.Subject,
            envelope.Body,
            now);
        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);
        string lockKey = $"platform-message:{message.DeliveryKey}";
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))",
            cancellationToken);

        PlatformTransactionalMessage? existingMessage = await dbContext.PlatformTransactionalMessages
            .AsNoTracking()
            .Where(entity => entity.DeliveryKey == message.DeliveryKey)
            .SingleOrDefaultAsync(cancellationToken);

        if (existingMessage is not null)
        {
            if (!IsSameMessage(existingMessage, message))
            {
                throw new InvalidOperationException("Platform message delivery key collision.");
            }

            await transaction.CommitAsync(cancellationToken);
            return existingMessage.Id;
        }

        dbContext.PlatformTransactionalMessages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return message.Id;
    }

    private static bool IsSameMessage(
        PlatformTransactionalMessage existingMessage,
        PlatformTransactionalMessage candidateMessage)
    {
        return existingMessage.UserAccountId == candidateMessage.UserAccountId
            && existingMessage.Purpose == candidateMessage.Purpose
            && existingMessage.CorrelationId == candidateMessage.CorrelationId
            && string.Equals(
                existingMessage.Subject,
                candidateMessage.Subject,
                StringComparison.Ordinal)
            && string.Equals(
                existingMessage.Body,
                candidateMessage.Body,
                StringComparison.Ordinal);
    }

    public async Task<IReadOnlyCollection<PlatformTransactionalMessageDeliveryView>> ClaimDueAsync(
        int batchSize,
        TimeSpan lockDuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(lockDuration, TimeSpan.Zero);

        DateTimeOffset now = timeProvider.GetUtcNow();
        string pending = PlatformTransactionalMessageStatus.Pending.ToString();
        string processing = PlatformTransactionalMessageStatus.Processing.ToString();
        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);
        List<PlatformTransactionalMessage> messages = await dbContext.PlatformTransactionalMessages
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM messaging."PlatformTransactionalMessages"
                WHERE ("Status" = {pending} AND "NextAttemptAtUtc" <= {now})
                    OR ("Status" = {processing} AND "LockedUntilUtc" <= {now})
                ORDER BY "CreatedAtUtc"
                FOR UPDATE SKIP LOCKED
                LIMIT {batchSize}
                """)
            .ToListAsync(cancellationToken);

        foreach (PlatformTransactionalMessage message in messages)
        {
            message.BeginAttempt(now, now.Add(lockDuration));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return messages
            .Select(entity => new PlatformTransactionalMessageDeliveryView(
                entity.Id,
                entity.UserAccountId,
                entity.Purpose,
                entity.CorrelationId,
                entity.Subject,
                entity.Body,
                entity.AttemptCount,
                entity.SentAtUtc))
            .ToArray();
    }

    public Task CancelAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        return MutateAsync(
            messageId,
            message => message.Cancel(timeProvider.GetUtcNow()),
            cancellationToken);
    }

    public Task CompleteAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        return MutateAsync(
            messageId,
            message => message.Complete(timeProvider.GetUtcNow()),
            cancellationToken);
    }

    public Task MarkDeliveryAcceptedAsync(
        Guid messageId,
        DateTimeOffset sentAtUtc,
        CancellationToken cancellationToken = default)
    {
        return MutateAsync(
            messageId,
            message => message.MarkDeliveryAccepted(sentAtUtc),
            cancellationToken);
    }

    public Task ScheduleRetryAsync(
        Guid messageId,
        string errorCode,
        TimeSpan retryDelay,
        int maxAttempts,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();

        return MutateAsync(
            messageId,
            message => message.ScheduleRetry(
                errorCode,
                now,
                now.Add(retryDelay),
                maxAttempts),
            cancellationToken);
    }

    private async Task MutateAsync(
        Guid messageId,
        Action<PlatformTransactionalMessage> mutate,
        CancellationToken cancellationToken)
    {
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException("Message id is required.", nameof(messageId));
        }

        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);
        PlatformTransactionalMessage message = await dbContext.PlatformTransactionalMessages
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM messaging."PlatformTransactionalMessages"
                WHERE "Id" = {messageId}
                FOR UPDATE
                """)
            .SingleAsync(cancellationToken);
        mutate(message);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
