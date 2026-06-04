namespace RezSaaS.Modules.Admin.Domain;

public sealed class BusinessAbuseReport
{
    private BusinessAbuseReport()
    {
    }

    private BusinessAbuseReport(
        Guid id,
        Guid tenantId,
        Guid branchId,
        Guid appointmentRequestId,
        Guid reportedUserAccountId,
        Guid reportedByUserAccountId,
        AbuseReportReasonCode reasonCode,
        string? note,
        DateTimeOffset createdAtUtc)
    {
        RequireNonEmpty(tenantId, nameof(tenantId));
        RequireNonEmpty(branchId, nameof(branchId));
        RequireNonEmpty(appointmentRequestId, nameof(appointmentRequestId));
        RequireNonEmpty(reportedUserAccountId, nameof(reportedUserAccountId));
        RequireNonEmpty(reportedByUserAccountId, nameof(reportedByUserAccountId));

        if (reportedUserAccountId == reportedByUserAccountId)
        {
            throw new ArgumentException("Reporter cannot report their own account.", nameof(reportedByUserAccountId));
        }

        if (!Enum.IsDefined(reasonCode))
        {
            throw new ArgumentException("Reason code is invalid.", nameof(reasonCode));
        }

        Id = id;
        TenantId = tenantId;
        BranchId = branchId;
        AppointmentRequestId = appointmentRequestId;
        ReportedUserAccountId = reportedUserAccountId;
        ReportedByUserAccountId = reportedByUserAccountId;
        ReasonCode = reasonCode;
        Note = NormalizeOptionalText(note, nameof(note), maxLength: 300);
        CreatedAtUtc = createdAtUtc;
    }

    public Guid AppointmentRequestId { get; private set; }

    public Guid BranchId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid Id { get; private set; }

    public string? Note { get; private set; }

    public AbuseReportReasonCode ReasonCode { get; private set; }

    public Guid ReportedByUserAccountId { get; private set; }

    public Guid ReportedUserAccountId { get; private set; }

    public DateTimeOffset? ReviewedAtUtc { get; private set; }

    public Guid? ReviewedByUserAccountId { get; private set; }

    public string? ReviewReason { get; private set; }

    public AbuseReportStatus Status { get; private set; } = AbuseReportStatus.PendingReview;

    public Guid TenantId { get; private set; }

    public static BusinessAbuseReport Create(
        Guid tenantId,
        Guid branchId,
        Guid appointmentRequestId,
        Guid reportedUserAccountId,
        Guid reportedByUserAccountId,
        AbuseReportReasonCode reasonCode,
        string? note,
        DateTimeOffset createdAtUtc)
    {
        return new BusinessAbuseReport(
            Guid.CreateVersion7(),
            tenantId,
            branchId,
            appointmentRequestId,
            reportedUserAccountId,
            reportedByUserAccountId,
            reasonCode,
            note,
            createdAtUtc);
    }

    public void Review(
        AbuseReportStatus decision,
        Guid reviewedByUserAccountId,
        string reason,
        DateTimeOffset reviewedAtUtc)
    {
        RequireNonEmpty(reviewedByUserAccountId, nameof(reviewedByUserAccountId));

        if (decision is not AbuseReportStatus.Confirmed and not AbuseReportStatus.Dismissed)
        {
            throw new ArgumentException("Review decision is invalid.", nameof(decision));
        }

        if (Status == decision)
        {
            return;
        }

        if (Status != AbuseReportStatus.PendingReview)
        {
            throw new InvalidOperationException("Report is already reviewed.");
        }

        if (reviewedAtUtc < CreatedAtUtc)
        {
            throw new ArgumentException("Review cannot predate the report.", nameof(reviewedAtUtc));
        }

        string normalizedReason = NormalizeRequiredText(reason, nameof(reason), maxLength: 300);
        Status = decision;
        ReviewedByUserAccountId = reviewedByUserAccountId;
        ReviewReason = normalizedReason;
        ReviewedAtUtc = reviewedAtUtc;
    }

    private static string? NormalizeOptionalText(
        string? value,
        string parameterName,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException("Value is too long.", parameterName);
        }

        return normalized;
    }

    private static string NormalizeRequiredText(
        string value,
        string parameterName,
        int maxLength)
    {
        return NormalizeOptionalText(value, parameterName, maxLength)
            ?? throw new ArgumentException("Value is required.", parameterName);
    }

    private static void RequireNonEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value is required.", parameterName);
        }
    }
}
