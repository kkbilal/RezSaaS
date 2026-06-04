using RezSaaS.Modules.Admin.Domain;

namespace RezSaaS.Modules.Admin.Application;

public sealed record BusinessAbuseReportView(
    Guid Id,
    Guid TenantId,
    Guid BranchId,
    Guid AppointmentRequestId,
    Guid ReportedUserAccountId,
    Guid ReportedByUserAccountId,
    AbuseReportReasonCode ReasonCode,
    string? Note,
    AbuseReportStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReviewedAtUtc,
    Guid? ReviewedByUserAccountId,
    string? ReviewReason);
