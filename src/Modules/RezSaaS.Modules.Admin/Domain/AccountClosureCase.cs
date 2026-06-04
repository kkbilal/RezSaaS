namespace RezSaaS.Modules.Admin.Domain;

public sealed class AccountClosureCase
{
    private AccountClosureCase()
    {
    }

    private AccountClosureCase(
        Guid id,
        Guid userAccountId,
        Guid proposedByUserAccountId,
        string internalReason,
        string customerNotice,
        DateTimeOffset proposedAtUtc)
    {
        RequireNonEmpty(userAccountId, nameof(userAccountId));
        RequireNonEmpty(proposedByUserAccountId, nameof(proposedByUserAccountId));

        if (userAccountId == proposedByUserAccountId)
        {
            throw new ArgumentException("Actor cannot target their own account.", nameof(proposedByUserAccountId));
        }

        Id = id;
        UserAccountId = userAccountId;
        ProposedByUserAccountId = proposedByUserAccountId;
        InternalReason = NormalizeRequiredText(internalReason, nameof(internalReason), maxLength: 500);
        CustomerNotice = NormalizeRequiredText(customerNotice, nameof(customerNotice), maxLength: 500);
        ProposedAtUtc = proposedAtUtc;
    }

    public string? DecisionReason { get; private set; }

    public DateTimeOffset? DecidedAtUtc { get; private set; }

    public string CustomerNotice { get; private set; } = string.Empty;

    public DateTimeOffset? CustomerNoticeDeliveredAtUtc { get; private set; }

    public DateTimeOffset? EligibleForExecutionAtUtc { get; private set; }

    public DateTimeOffset? ExecutedAtUtc { get; private set; }

    public Guid? ExecutedByUserAccountId { get; private set; }

    public DateTimeOffset? ExecutionStartedAtUtc { get; private set; }

    public Guid? ExecutionStartedByUserAccountId { get; private set; }

    public Guid Id { get; private set; }

    public string InternalReason { get; private set; } = string.Empty;

    public DateTimeOffset ProposedAtUtc { get; private set; }

    public Guid ProposedByUserAccountId { get; private set; }

    public Guid? ReviewedByUserAccountId { get; private set; }

    public AccountClosureCaseStatus Status { get; private set; } = AccountClosureCaseStatus.PendingApproval;

    public Guid UserAccountId { get; private set; }

    public static AccountClosureCase Create(
        Guid userAccountId,
        Guid proposedByUserAccountId,
        string internalReason,
        string customerNotice,
        DateTimeOffset proposedAtUtc)
    {
        return new AccountClosureCase(
            Guid.CreateVersion7(),
            userAccountId,
            proposedByUserAccountId,
            internalReason,
            customerNotice,
            proposedAtUtc);
    }

    public void Approve(
        Guid actorUserAccountId,
        string reason,
        DateTimeOffset decidedAtUtc)
    {
        Decide(AccountClosureCaseStatus.Approved, actorUserAccountId, reason, decidedAtUtc);
    }

    public void BeginExecution(
        Guid actorUserAccountId,
        DateTimeOffset startedAtUtc)
    {
        RequireNonEmpty(actorUserAccountId, nameof(actorUserAccountId));
        EnsureActorIsNotTarget(actorUserAccountId);

        if (Status == AccountClosureCaseStatus.Executing)
        {
            return;
        }

        if (Status != AccountClosureCaseStatus.Approved)
        {
            throw new InvalidOperationException("Closure case is not approved.");
        }

        if (CustomerNoticeDeliveredAtUtc is null || EligibleForExecutionAtUtc is null)
        {
            throw new InvalidOperationException("Customer notice has not been delivered.");
        }

        if (startedAtUtc < EligibleForExecutionAtUtc.Value)
        {
            throw new InvalidOperationException("Appeal window is still open.");
        }

        Status = AccountClosureCaseStatus.Executing;
        ExecutionStartedByUserAccountId = actorUserAccountId;
        ExecutionStartedAtUtc = startedAtUtc;
    }

    public void MarkCustomerNoticeDelivered(
        DateTimeOffset deliveredAtUtc,
        TimeSpan appealWindow)
    {
        if (CustomerNoticeDeliveredAtUtc is not null)
        {
            return;
        }

        if (Status is not AccountClosureCaseStatus.PendingApproval
            and not AccountClosureCaseStatus.Approved)
        {
            throw new InvalidOperationException("Customer notice is no longer required.");
        }

        if (deliveredAtUtc < ProposedAtUtc)
        {
            throw new ArgumentException("Notice delivery cannot predate the proposal.", nameof(deliveredAtUtc));
        }

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(appealWindow, TimeSpan.Zero);

        CustomerNoticeDeliveredAtUtc = deliveredAtUtc;
        EligibleForExecutionAtUtc = deliveredAtUtc.Add(appealWindow);
    }

    public void CancelByAcceptedAppeal(
        Guid actorUserAccountId,
        string reason,
        DateTimeOffset decidedAtUtc)
    {
        RequireNonEmpty(actorUserAccountId, nameof(actorUserAccountId));
        EnsureActorIsNotTarget(actorUserAccountId);

        if (Status == AccountClosureCaseStatus.CancelledByAppeal)
        {
            return;
        }

        if (Status is not AccountClosureCaseStatus.PendingApproval
            and not AccountClosureCaseStatus.Approved)
        {
            throw new InvalidOperationException("Closure case cannot be cancelled by appeal.");
        }

        string normalizedReason = NormalizeRequiredText(reason, nameof(reason), maxLength: 500);

        if (decidedAtUtc < ProposedAtUtc)
        {
            throw new ArgumentException("Decision cannot predate the proposal.", nameof(decidedAtUtc));
        }

        Status = AccountClosureCaseStatus.CancelledByAppeal;
        ReviewedByUserAccountId = actorUserAccountId;
        DecisionReason = normalizedReason;
        DecidedAtUtc = decidedAtUtc;
    }

    public void CompleteExecution(
        Guid actorUserAccountId,
        DateTimeOffset executedAtUtc)
    {
        RequireNonEmpty(actorUserAccountId, nameof(actorUserAccountId));
        EnsureActorIsNotTarget(actorUserAccountId);

        if (Status == AccountClosureCaseStatus.Executed)
        {
            return;
        }

        if (Status != AccountClosureCaseStatus.Executing)
        {
            throw new InvalidOperationException("Closure case execution has not started.");
        }

        if (ExecutionStartedAtUtc is null || executedAtUtc < ExecutionStartedAtUtc.Value)
        {
            throw new ArgumentException("Execution completion cannot predate its start.", nameof(executedAtUtc));
        }

        Status = AccountClosureCaseStatus.Executed;
        ExecutedByUserAccountId = actorUserAccountId;
        ExecutedAtUtc = executedAtUtc;
    }

    public void Reject(
        Guid actorUserAccountId,
        string reason,
        DateTimeOffset decidedAtUtc)
    {
        Decide(AccountClosureCaseStatus.Rejected, actorUserAccountId, reason, decidedAtUtc);
    }

    private void Decide(
        AccountClosureCaseStatus decision,
        Guid actorUserAccountId,
        string reason,
        DateTimeOffset decidedAtUtc)
    {
        RequireNonEmpty(actorUserAccountId, nameof(actorUserAccountId));
        EnsureActorIsNotTarget(actorUserAccountId);

        if (decision is not AccountClosureCaseStatus.Approved and not AccountClosureCaseStatus.Rejected)
        {
            throw new ArgumentException("Decision is invalid.", nameof(decision));
        }

        if (Status == decision)
        {
            return;
        }

        if (Status != AccountClosureCaseStatus.PendingApproval)
        {
            throw new InvalidOperationException("Closure case is already reviewed.");
        }

        if (actorUserAccountId == ProposedByUserAccountId)
        {
            throw new InvalidOperationException("Proposer cannot review their own closure case.");
        }

        if (decidedAtUtc < ProposedAtUtc)
        {
            throw new ArgumentException("Decision cannot predate the proposal.", nameof(decidedAtUtc));
        }

        string normalizedReason = NormalizeRequiredText(reason, nameof(reason), maxLength: 500);
        Status = decision;
        ReviewedByUserAccountId = actorUserAccountId;
        DecisionReason = normalizedReason;
        DecidedAtUtc = decidedAtUtc;
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

    private void EnsureActorIsNotTarget(Guid actorUserAccountId)
    {
        if (actorUserAccountId == UserAccountId)
        {
            throw new InvalidOperationException("Target account cannot operate its own closure case.");
        }
    }

    private static void RequireNonEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value is required.", parameterName);
        }
    }
}
