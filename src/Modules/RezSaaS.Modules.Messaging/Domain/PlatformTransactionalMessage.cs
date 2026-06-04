namespace RezSaaS.Modules.Messaging.Domain;

public sealed class PlatformTransactionalMessage
{
    private PlatformTransactionalMessage()
    {
    }

    private PlatformTransactionalMessage(
        Guid id,
        Guid userAccountId,
        PlatformMessagePurpose purpose,
        Guid correlationId,
        string deliveryKey,
        string subject,
        string body,
        DateTimeOffset createdAtUtc)
    {
        RequireNonEmpty(userAccountId, nameof(userAccountId));
        RequireNonEmpty(correlationId, nameof(correlationId));

        if (!Enum.IsDefined(purpose))
        {
            throw new ArgumentException("Purpose is invalid.", nameof(purpose));
        }

        Id = id;
        UserAccountId = userAccountId;
        Purpose = purpose;
        CorrelationId = correlationId;
        DeliveryKey = NormalizeRequiredText(deliveryKey, nameof(deliveryKey), maxLength: 180);
        Subject = NormalizeRequiredText(subject, nameof(subject), maxLength: 200);
        Body = NormalizeRequiredText(body, nameof(body), maxLength: 4000);
        CreatedAtUtc = createdAtUtc;
        NextAttemptAtUtc = createdAtUtc;
    }

    public int AttemptCount { get; private set; }

    public string Body { get; private set; } = string.Empty;

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public Guid CorrelationId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string DeliveryKey { get; private set; } = string.Empty;

    public Guid Id { get; private set; }

    public DateTimeOffset? LastAttemptAtUtc { get; private set; }

    public string? LastErrorCode { get; private set; }

    public DateTimeOffset? LockedUntilUtc { get; private set; }

    public DateTimeOffset? NextAttemptAtUtc { get; private set; }

    public PlatformMessagePurpose Purpose { get; private set; }

    public DateTimeOffset? SentAtUtc { get; private set; }

    public PlatformTransactionalMessageStatus Status { get; private set; } =
        PlatformTransactionalMessageStatus.Pending;

    public string Subject { get; private set; } = string.Empty;

    public Guid UserAccountId { get; private set; }

    public static PlatformTransactionalMessage Create(
        Guid userAccountId,
        PlatformMessagePurpose purpose,
        Guid correlationId,
        string deliveryKey,
        string subject,
        string body,
        DateTimeOffset createdAtUtc)
    {
        return new PlatformTransactionalMessage(
            Guid.CreateVersion7(),
            userAccountId,
            purpose,
            correlationId,
            deliveryKey,
            subject,
            body,
            createdAtUtc);
    }

    public void BeginAttempt(DateTimeOffset attemptedAtUtc, DateTimeOffset lockedUntilUtc)
    {
        if (Status == PlatformTransactionalMessageStatus.Sent
            || Status == PlatformTransactionalMessageStatus.Failed
            || Status == PlatformTransactionalMessageStatus.Cancelled)
        {
            throw new InvalidOperationException("Terminal platform message cannot be claimed.");
        }

        if (lockedUntilUtc <= attemptedAtUtc)
        {
            throw new ArgumentException("Lock expiry must be later than attempt time.", nameof(lockedUntilUtc));
        }

        AttemptCount++;
        LastAttemptAtUtc = attemptedAtUtc;
        LastErrorCode = null;
        LockedUntilUtc = lockedUntilUtc;
        NextAttemptAtUtc = null;
        Status = PlatformTransactionalMessageStatus.Processing;
    }

    public void Cancel(DateTimeOffset completedAtUtc)
    {
        if (Status == PlatformTransactionalMessageStatus.Cancelled)
        {
            return;
        }

        if (Status == PlatformTransactionalMessageStatus.Sent
            || Status == PlatformTransactionalMessageStatus.Failed)
        {
            throw new InvalidOperationException("Terminal platform message cannot be cancelled.");
        }

        CompleteTerminalState(PlatformTransactionalMessageStatus.Cancelled, completedAtUtc);
    }

    public void Complete(DateTimeOffset completedAtUtc)
    {
        if (Status == PlatformTransactionalMessageStatus.Sent)
        {
            return;
        }

        if (Status != PlatformTransactionalMessageStatus.Processing || SentAtUtc is null)
        {
            throw new InvalidOperationException("Platform message delivery is not accepted.");
        }

        CompleteTerminalState(PlatformTransactionalMessageStatus.Sent, completedAtUtc);
    }

    public void MarkDeliveryAccepted(DateTimeOffset sentAtUtc)
    {
        if (SentAtUtc is not null)
        {
            return;
        }

        if (Status != PlatformTransactionalMessageStatus.Processing)
        {
            throw new InvalidOperationException("Platform message is not being processed.");
        }

        if (sentAtUtc < CreatedAtUtc)
        {
            throw new ArgumentException("Delivery acceptance cannot predate message creation.", nameof(sentAtUtc));
        }

        SentAtUtc = sentAtUtc;
    }

    public void ScheduleRetry(
        string errorCode,
        DateTimeOffset failedAtUtc,
        DateTimeOffset nextAttemptAtUtc,
        int maxAttempts)
    {
        if (Status != PlatformTransactionalMessageStatus.Processing)
        {
            throw new InvalidOperationException("Platform message is not being processed.");
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxAttempts);

        LastErrorCode = NormalizeRequiredText(errorCode, nameof(errorCode), maxLength: 120);
        LockedUntilUtc = null;

        if (AttemptCount >= maxAttempts)
        {
            CompleteTerminalState(PlatformTransactionalMessageStatus.Failed, failedAtUtc);
            LastErrorCode = errorCode.Trim();
            return;
        }

        if (nextAttemptAtUtc <= failedAtUtc)
        {
            throw new ArgumentException("Next attempt must be later than failure time.", nameof(nextAttemptAtUtc));
        }

        NextAttemptAtUtc = nextAttemptAtUtc;
        Status = PlatformTransactionalMessageStatus.Pending;
    }

    private void CompleteTerminalState(
        PlatformTransactionalMessageStatus status,
        DateTimeOffset completedAtUtc)
    {
        if (completedAtUtc < CreatedAtUtc)
        {
            throw new ArgumentException("Completion cannot predate message creation.", nameof(completedAtUtc));
        }

        Status = status;
        CompletedAtUtc = completedAtUtc;
        LockedUntilUtc = null;
        NextAttemptAtUtc = null;
    }

    private static string NormalizeRequiredText(
        string value,
        string parameterName,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        string normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException("Value is too long.", parameterName);
        }

        return normalized;
    }

    private static void RequireNonEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value is required.", parameterName);
        }
    }
}
