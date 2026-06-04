namespace RezSaaS.Modules.Admin.Domain;

public sealed class AbuseAppeal
{
    private AbuseAppeal()
    {
    }

    private AbuseAppeal(
        Guid id,
        Guid userAccountId,
        AbuseAppealTargetType targetType,
        Guid targetId,
        string statement,
        DateTimeOffset createdAtUtc)
    {
        RequireNonEmpty(userAccountId, nameof(userAccountId));
        RequireNonEmpty(targetId, nameof(targetId));

        if (!Enum.IsDefined(targetType))
        {
            throw new ArgumentException("Target type is invalid.", nameof(targetType));
        }

        Id = id;
        UserAccountId = userAccountId;
        TargetType = targetType;
        TargetId = targetId;
        Statement = NormalizeRequiredText(statement, nameof(statement), maxLength: 1000);
        CreatedAtUtc = createdAtUtc;
    }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid Id { get; private set; }

    public DateTimeOffset? ReviewedAtUtc { get; private set; }

    public Guid? ReviewedByUserAccountId { get; private set; }

    public string? ReviewReason { get; private set; }

    public string Statement { get; private set; } = string.Empty;

    public AbuseAppealStatus Status { get; private set; } = AbuseAppealStatus.PendingReview;

    public Guid TargetId { get; private set; }

    public AbuseAppealTargetType TargetType { get; private set; }

    public Guid UserAccountId { get; private set; }

    public static AbuseAppeal Create(
        Guid userAccountId,
        AbuseAppealTargetType targetType,
        Guid targetId,
        string statement,
        DateTimeOffset createdAtUtc)
    {
        return new AbuseAppeal(
            Guid.CreateVersion7(),
            userAccountId,
            targetType,
            targetId,
            statement,
            createdAtUtc);
    }

    public void Review(
        AbuseAppealStatus decision,
        Guid reviewedByUserAccountId,
        string reason,
        DateTimeOffset reviewedAtUtc)
    {
        RequireNonEmpty(reviewedByUserAccountId, nameof(reviewedByUserAccountId));

        if (reviewedByUserAccountId == UserAccountId)
        {
            throw new InvalidOperationException("Users cannot review their own appeals.");
        }

        if (decision is not AbuseAppealStatus.Accepted and not AbuseAppealStatus.Rejected)
        {
            throw new ArgumentException("Review decision is invalid.", nameof(decision));
        }

        if (Status == decision)
        {
            return;
        }

        if (Status != AbuseAppealStatus.PendingReview)
        {
            throw new InvalidOperationException("Appeal is already reviewed.");
        }

        if (reviewedAtUtc < CreatedAtUtc)
        {
            throw new ArgumentException("Review cannot predate the appeal.", nameof(reviewedAtUtc));
        }

        string normalizedReason = NormalizeRequiredText(reason, nameof(reason), maxLength: 500);
        Status = decision;
        ReviewedByUserAccountId = reviewedByUserAccountId;
        ReviewReason = normalizedReason;
        ReviewedAtUtc = reviewedAtUtc;
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
