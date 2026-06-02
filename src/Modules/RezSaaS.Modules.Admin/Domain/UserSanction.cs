namespace RezSaaS.Modules.Admin.Domain;

public sealed class UserSanction
{
    private UserSanction()
    {
    }

    private UserSanction(
        Guid id,
        Guid userAccountId,
        UserSanctionType type,
        string reason,
        DateTimeOffset startsAtUtc,
        DateTimeOffset? endsAtUtc)
    {
        if (userAccountId == Guid.Empty)
        {
            throw new ArgumentException("Value is required.", nameof(userAccountId));
        }

        if (endsAtUtc is not null && endsAtUtc <= startsAtUtc)
        {
            throw new ArgumentException("End must be later than start.", nameof(endsAtUtc));
        }

        Id = id;
        UserAccountId = userAccountId;
        Type = type;
        Reason = NormalizeRequiredText(reason, nameof(reason));
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
    }

    public DateTimeOffset? EndsAtUtc { get; private set; }

    public Guid Id { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public DateTimeOffset StartsAtUtc { get; private set; }

    public UserSanctionType Type { get; private set; }

    public Guid UserAccountId { get; private set; }

    public static UserSanction Create(
        Guid userAccountId,
        UserSanctionType type,
        string reason,
        DateTimeOffset startsAtUtc,
        DateTimeOffset? endsAtUtc = null)
    {
        return new UserSanction(Guid.CreateVersion7(), userAccountId, type, reason, startsAtUtc, endsAtUtc);
    }

    private static string NormalizeRequiredText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }
}
