using Microsoft.Extensions.Options;
using RezSaaS.Modules.Admin.Application;
using RezSaaS.Modules.Identity.Application;
using RezSaaS.Modules.Messaging.Application;
using RezSaaS.Modules.Messaging.Domain;

namespace RezSaaS.Api.Configuration;

public sealed class PlatformNotificationDispatchService
{
    private const string CallbackTargetNotFound = "PLATFORM_NOTIFICATION_CALLBACK_TARGET_NOT_FOUND";
    private const string DeliveryFailed = "PLATFORM_NOTIFICATION_DELIVERY_FAILED";

    private readonly AccountClosureNoticeDeliveryService closureNoticeDeliveryService;
    private readonly UserTransactionalEmailService emailService;
    private readonly PlatformNotificationWorkerOptions options;
    private readonly PlatformTransactionalMessageQueueService queueService;
    private readonly TimeProvider timeProvider;

    public PlatformNotificationDispatchService(
        PlatformTransactionalMessageQueueService queueService,
        UserTransactionalEmailService emailService,
        AccountClosureNoticeDeliveryService closureNoticeDeliveryService,
        IOptions<PlatformNotificationWorkerOptions> options,
        TimeProvider timeProvider)
    {
        this.queueService = queueService;
        this.emailService = emailService;
        this.closureNoticeDeliveryService = closureNoticeDeliveryService;
        this.options = options.Value;
        this.timeProvider = timeProvider;
    }

    public async Task<int> DispatchDueAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<PlatformTransactionalMessageDeliveryView> messages =
            await queueService.ClaimDueAsync(
                options.BatchSize,
                options.LockDuration,
                cancellationToken);

        foreach (PlatformTransactionalMessageDeliveryView message in messages)
        {
            await DispatchAsync(message, cancellationToken);
        }

        return messages.Count;
    }

    private async Task DispatchAsync(
        PlatformTransactionalMessageDeliveryView message,
        CancellationToken cancellationToken)
    {
        try
        {
            if (message.Purpose == PlatformMessagePurpose.AccountClosureProposed)
            {
                AccountClosureNoticeDeliveryState state =
                    await closureNoticeDeliveryService.GetStateAsync(
                        message.CorrelationId,
                        cancellationToken);

                if (state == AccountClosureNoticeDeliveryState.NoLongerRequired
                    || state == AccountClosureNoticeDeliveryState.Delivered
                        && message.SentAtUtc is null)
                {
                    await queueService.CancelAsync(message.Id, cancellationToken);
                    return;
                }

                if (state == AccountClosureNoticeDeliveryState.NotFound)
                {
                    await ScheduleRetryAsync(message.Id, CallbackTargetNotFound, cancellationToken);
                    return;
                }
            }

            DateTimeOffset sentAtUtc;

            if (message.SentAtUtc is null)
            {
                UserTransactionalEmailResult deliveryResult =
                    await emailService.SendAsync(
                        message.UserAccountId,
                        message.Subject,
                        message.Body,
                        cancellationToken);

                if (!deliveryResult.Succeeded)
                {
                    await ScheduleRetryAsync(
                        message.Id,
                        deliveryResult.ErrorCode ?? DeliveryFailed,
                        cancellationToken);
                    return;
                }

                sentAtUtc = timeProvider.GetUtcNow();
                await queueService.MarkDeliveryAcceptedAsync(
                    message.Id,
                    sentAtUtc,
                    cancellationToken);
            }
            else
            {
                sentAtUtc = message.SentAtUtc.Value;
            }

            if (message.Purpose == PlatformMessagePurpose.AccountClosureProposed)
            {
                AccountClosureNoticeDeliveryState state =
                    await closureNoticeDeliveryService.MarkDeliveredAsync(
                        message.CorrelationId,
                        sentAtUtc,
                        cancellationToken);

                if (state == AccountClosureNoticeDeliveryState.NotFound)
                {
                    await ScheduleRetryAsync(message.Id, CallbackTargetNotFound, cancellationToken);
                    return;
                }
            }

            await queueService.CompleteAsync(message.Id, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            await ScheduleRetryAsync(message.Id, DeliveryFailed, cancellationToken);
        }
    }

    private Task ScheduleRetryAsync(
        Guid messageId,
        string errorCode,
        CancellationToken cancellationToken)
    {
        return queueService.ScheduleRetryAsync(
            messageId,
            errorCode,
            options.RetryDelay,
            options.MaxAttempts,
            cancellationToken);
    }
}
