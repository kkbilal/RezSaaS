namespace RezSaaS.Modules.Identity.Domain;

public sealed class StepUpSession
{
    private StepUpSession()
    {
    }

    private StepUpSession(
        Guid id,
        Guid userAccountId,
        string tokenHash,
        string method,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        if (userAccountId == Guid.Empty)
        {
            throw new ArgumentException("User account id is required.", nameof(userAccountId));
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("Token hash is required.", nameof(tokenHash));
        }

        if (string.IsNullOrWhiteSpace(method))
        {
            throw new ArgumentException("Method is required.", nameof(method));
        }

        if (expiresAtUtc <= createdAtUtc)
        {
            throw new ArgumentException("Expiry must be after creation time.", nameof(expiresAtUtc));
        }

        Id = id;
        UserAccountId = userAccountId;
        TokenHash = tokenHash.Trim();
        Method = method.Trim();
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public Guid Id { get; private set; }

    public string Method { get; private set; } = string.Empty;

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public Guid UserAccountId { get; private set; }

    public static StepUpSession Create(
        Guid userAccountId,
        string tokenHash,
        string method,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        return new StepUpSession(
            Guid.CreateVersion7(),
            userAccountId,
            tokenHash,
            method,
            createdAtUtc,
            expiresAtUtc);
    }

    public bool IsActive(DateTimeOffset now)
    {
        return RevokedAtUtc is null && ExpiresAtUtc > now;
    }

    public void Revoke(DateTimeOffset revokedAtUtc)
    {
        RevokedAtUtc ??= revokedAtUtc;
    }
}
