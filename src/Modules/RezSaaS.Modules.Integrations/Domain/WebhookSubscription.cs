namespace RezSaaS.Modules.Integrations.Domain;

public sealed class WebhookSubscription
{
    private WebhookSubscription()
    {
    }

    private WebhookSubscription(
        Guid id,
        Guid tenantId,
        string displayName,
        string targetUrl,
        string eventTypes,
        string signingSecretHashSha256,
        Guid createdByUserAccountId,
        DateTimeOffset createdAtUtc)
    {
        RequireNonEmpty(tenantId, nameof(tenantId));
        RequireNonEmpty(createdByUserAccountId, nameof(createdByUserAccountId));

        Id = id;
        TenantId = tenantId;
        DisplayName = NormalizeRequiredText(displayName, nameof(displayName), maxLength: 120);
        TargetUrl = NormalizeWebhookUrl(targetUrl);
        EventTypes = NormalizeRequiredText(eventTypes, nameof(eventTypes), maxLength: 500);
        SigningSecretHashSha256 = NormalizeSha256(signingSecretHashSha256);
        CreatedByUserAccountId = createdByUserAccountId;
        CreatedAtUtc = createdAtUtc;
        UpdatedByUserAccountId = createdByUserAccountId;
        UpdatedAtUtc = createdAtUtc;
        Status = WebhookSubscriptionStatus.Active;
    }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid CreatedByUserAccountId { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public string EventTypes { get; private set; } = string.Empty;

    public Guid Id { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public Guid? RevokedByUserAccountId { get; private set; }

    public string RevocationReason { get; private set; } = string.Empty;

    public string SigningSecretHashSha256 { get; private set; } = string.Empty;

    public WebhookSubscriptionStatus Status { get; private set; }

    public string TargetUrl { get; private set; } = string.Empty;

    public Guid TenantId { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public Guid UpdatedByUserAccountId { get; private set; }

    public static WebhookSubscription Create(
        Guid tenantId,
        string displayName,
        string targetUrl,
        string eventTypes,
        string signingSecretHashSha256,
        Guid actorUserAccountId,
        DateTimeOffset createdAtUtc)
    {
        return new WebhookSubscription(
            Guid.CreateVersion7(),
            tenantId,
            displayName,
            targetUrl,
            eventTypes,
            signingSecretHashSha256,
            actorUserAccountId,
            createdAtUtc);
    }

    public void Pause(Guid actorUserAccountId, DateTimeOffset updatedAtUtc)
    {
        RequireNonEmpty(actorUserAccountId, nameof(actorUserAccountId));

        if (Status == WebhookSubscriptionStatus.Revoked)
        {
            throw new InvalidOperationException("Revoked webhook subscription cannot be paused.");
        }

        Status = WebhookSubscriptionStatus.Paused;
        UpdatedByUserAccountId = actorUserAccountId;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Reactivate(Guid actorUserAccountId, DateTimeOffset updatedAtUtc)
    {
        RequireNonEmpty(actorUserAccountId, nameof(actorUserAccountId));

        if (Status == WebhookSubscriptionStatus.Revoked)
        {
            throw new InvalidOperationException("Revoked webhook subscription cannot be reactivated.");
        }

        Status = WebhookSubscriptionStatus.Active;
        UpdatedByUserAccountId = actorUserAccountId;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Revoke(
        Guid actorUserAccountId,
        string reason,
        DateTimeOffset revokedAtUtc)
    {
        RequireNonEmpty(actorUserAccountId, nameof(actorUserAccountId));

        if (Status == WebhookSubscriptionStatus.Revoked)
        {
            return;
        }

        Status = WebhookSubscriptionStatus.Revoked;
        RevokedByUserAccountId = actorUserAccountId;
        RevokedAtUtc = revokedAtUtc;
        RevocationReason = NormalizeRequiredText(reason, nameof(reason), maxLength: 500);
        UpdatedByUserAccountId = actorUserAccountId;
        UpdatedAtUtc = revokedAtUtc;
    }

    private static string NormalizeWebhookUrl(string value)
    {
        string normalized = NormalizeRequiredText(value, nameof(value), maxLength: 1_000);

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out Uri? uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new ArgumentException(
                "Webhook target URL must be absolute HTTPS and must not contain user info, query or fragment.",
                nameof(value));
        }

        int schemeSeparatorIndex = normalized.IndexOf("://", StringComparison.Ordinal);

        return Uri.UriSchemeHttps + "://" + normalized[(schemeSeparatorIndex + 3)..];
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
