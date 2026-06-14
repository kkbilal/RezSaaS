namespace RezSaaS.Modules.Integrations.Domain;

public sealed class IntegrationApiClient
{
    private IntegrationApiClient()
    {
    }

    private IntegrationApiClient(
        Guid id,
        Guid tenantId,
        string displayName,
        string keyPrefix,
        string keyHashSha256,
        string scopeSet,
        Guid createdByUserAccountId,
        DateTimeOffset createdAtUtc)
    {
        RequireNonEmpty(tenantId, nameof(tenantId));
        RequireNonEmpty(createdByUserAccountId, nameof(createdByUserAccountId));

        Id = id;
        TenantId = tenantId;
        DisplayName = NormalizeRequiredText(displayName, nameof(displayName), maxLength: 120);
        KeyPrefix = NormalizeRequiredText(keyPrefix, nameof(keyPrefix), maxLength: 32);
        KeyHashSha256 = NormalizeSha256(keyHashSha256);
        ScopeSet = NormalizeRequiredText(scopeSet, nameof(scopeSet), maxLength: 500);
        CreatedByUserAccountId = createdByUserAccountId;
        CreatedAtUtc = createdAtUtc;
        Status = IntegrationApiClientStatus.Active;
    }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid CreatedByUserAccountId { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public Guid Id { get; private set; }

    public string KeyHashSha256 { get; private set; } = string.Empty;

    public string KeyPrefix { get; private set; } = string.Empty;

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public Guid? RevokedByUserAccountId { get; private set; }

    public string RevocationReason { get; private set; } = string.Empty;

    public string ScopeSet { get; private set; } = string.Empty;

    public IntegrationApiClientStatus Status { get; private set; }

    public Guid TenantId { get; private set; }

    public static IntegrationApiClient Create(
        Guid tenantId,
        string displayName,
        string keyPrefix,
        string keyHashSha256,
        string scopeSet,
        Guid actorUserAccountId,
        DateTimeOffset createdAtUtc)
    {
        return new IntegrationApiClient(
            Guid.CreateVersion7(),
            tenantId,
            displayName,
            keyPrefix,
            keyHashSha256,
            scopeSet,
            actorUserAccountId,
            createdAtUtc);
    }

    public void Revoke(
        Guid actorUserAccountId,
        string reason,
        DateTimeOffset revokedAtUtc)
    {
        RequireNonEmpty(actorUserAccountId, nameof(actorUserAccountId));

        if (Status == IntegrationApiClientStatus.Revoked)
        {
            return;
        }

        Status = IntegrationApiClientStatus.Revoked;
        RevokedByUserAccountId = actorUserAccountId;
        RevokedAtUtc = revokedAtUtc;
        RevocationReason = NormalizeRequiredText(reason, nameof(reason), maxLength: 500);
    }

    private static string NormalizeSha256(string value)
    {
        string normalized = NormalizeRequiredText(value, nameof(value), maxLength: 64)
            .ToUpperInvariant();

        if (normalized.Length != 64 || normalized.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException("Value must be a SHA-256 hex string.", nameof(value));
        }

        return normalized;
    }

    private static string NormalizeRequiredText(string value, string parameterName, int maxLength)
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
