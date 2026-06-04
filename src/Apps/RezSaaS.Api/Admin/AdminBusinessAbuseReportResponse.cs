namespace RezSaaS.Api.Admin;

public sealed record AdminBusinessAbuseReportResponse(
    Guid ReportId,
    Guid TenantId,
    Guid BranchId,
    Guid AppointmentRequestId,
    Guid ReportedUserAccountId,
    Guid ReportedByUserAccountId,
    string ReasonCode,
    string? Note,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReviewedAtUtc,
    Guid? ReviewedByUserAccountId,
    string? ReviewReason);
