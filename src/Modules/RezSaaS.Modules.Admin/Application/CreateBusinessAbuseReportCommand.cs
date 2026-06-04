using RezSaaS.Modules.Admin.Domain;

namespace RezSaaS.Modules.Admin.Application;

public sealed record CreateBusinessAbuseReportCommand(
    Guid TenantId,
    Guid BranchId,
    Guid AppointmentRequestId,
    Guid ReportedUserAccountId,
    Guid ReportedByUserAccountId,
    AbuseReportReasonCode ReasonCode,
    string? Note);
