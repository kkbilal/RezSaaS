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

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentException("Sanction type is invalid.", nameof(type));
        }

        if ((type == UserSanctionType.Warning || type == UserSanctionType.PermanentClosure)
            && endsAtUtc is not null)
        {
            throw new ArgumentException("This sanction type cannot have an end time.", nameof(endsAtUtc));
        }

        if ((type == UserSanctionType.Cooldown || type == UserSanctionType.TemporaryBan)
            && endsAtUtc is null)
        {
            throw new ArgumentException("This sanction type requires an end time.", nameof(endsAtUtc));
        }

        Id = id;
        UserAccountId = userAccountId;
        Type = type;
        Reason = NormalizeRequiredText(reason, nameof(reason), maxLength: 300);
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
    }

    public DateTimeOffset? EndsAtUtc { get; private set; }

    public Guid Id { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public Guid? RevokedByUserAccountId { get; private set; }

    public string? RevocationReason { get; private set; }

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

    public void Revoke(
        Guid actorUserAccountId,
        string reason,
        DateTimeOffset revokedAtUtc)
    {
        if (actorUserAccountId == Guid.Empty)
        {
            throw new ArgumentException("Value is required.", nameof(actorUserAccountId));
        }

        if (Type == UserSanctionType.Warning)
        {
            throw new InvalidOperationException("Warnings cannot be revoked.");
        }

        if (RevokedAtUtc is not null)
        {
            return;
        }

        if (revokedAtUtc < StartsAtUtc)
        {
            throw new ArgumentException("Revocation cannot predate the sanction.", nameof(revokedAtUtc));
        }

        string normalizedReason = NormalizeRequiredText(reason, nameof(reason), maxLength: 300);
        RevokedAtUtc = revokedAtUtc;
        RevokedByUserAccountId = actorUserAccountId;
        RevocationReason = normalizedReason;
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
}
