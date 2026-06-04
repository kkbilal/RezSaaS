namespace RezSaaS.Modules.Admin.Domain;

public sealed class UserStrike
{
    private UserStrike()
    {
    }

    private UserStrike(
        Guid id,
        Guid userAccountId,
        Guid tenantId,
        Guid sourceAbuseReportId,
        AbuseReportReasonCode reasonCode,
        Guid issuedByUserAccountId,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        RequireNonEmpty(userAccountId, nameof(userAccountId));
        RequireNonEmpty(tenantId, nameof(tenantId));
        RequireNonEmpty(sourceAbuseReportId, nameof(sourceAbuseReportId));
        RequireNonEmpty(issuedByUserAccountId, nameof(issuedByUserAccountId));

        if (!Enum.IsDefined(reasonCode))
        {
            throw new ArgumentException("Reason code is invalid.", nameof(reasonCode));
        }

        if (expiresAtUtc <= issuedAtUtc)
        {
            throw new ArgumentException("Expiry must be later than issue time.", nameof(expiresAtUtc));
        }

        Id = id;
        UserAccountId = userAccountId;
        TenantId = tenantId;
        SourceAbuseReportId = sourceAbuseReportId;
        ReasonCode = reasonCode;
        IssuedByUserAccountId = issuedByUserAccountId;
        IssuedAtUtc = issuedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public Guid Id { get; private set; }

    public DateTimeOffset IssuedAtUtc { get; private set; }

    public Guid IssuedByUserAccountId { get; private set; }

    public AbuseReportReasonCode ReasonCode { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public Guid? RevokedByUserAccountId { get; private set; }

    public string? RevocationReason { get; private set; }

    public Guid SourceAbuseReportId { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid UserAccountId { get; private set; }

    public static UserStrike Create(
        Guid userAccountId,
        Guid tenantId,
        Guid sourceAbuseReportId,
        AbuseReportReasonCode reasonCode,
        Guid issuedByUserAccountId,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        return new UserStrike(
            Guid.CreateVersion7(),
            userAccountId,
            tenantId,
            sourceAbuseReportId,
            reasonCode,
            issuedByUserAccountId,
            issuedAtUtc,
            expiresAtUtc);
    }

    public void Revoke(
        Guid revokedByUserAccountId,
        string reason,
        DateTimeOffset revokedAtUtc)
    {
        RequireNonEmpty(revokedByUserAccountId, nameof(revokedByUserAccountId));

        if (RevokedAtUtc is not null)
        {
            return;
        }

        if (revokedAtUtc < IssuedAtUtc)
        {
            throw new ArgumentException("Revocation cannot predate the strike.", nameof(revokedAtUtc));
        }

        string normalizedReason = NormalizeRequiredText(reason, nameof(reason), maxLength: 300);
        RevokedAtUtc = revokedAtUtc;
        RevokedByUserAccountId = revokedByUserAccountId;
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

    private static void RequireNonEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value is required.", parameterName);
        }
    }
}
