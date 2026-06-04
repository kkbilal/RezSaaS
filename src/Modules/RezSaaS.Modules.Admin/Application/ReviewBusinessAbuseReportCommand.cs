using RezSaaS.Modules.Admin.Domain;

namespace RezSaaS.Modules.Admin.Application;

public sealed record ReviewBusinessAbuseReportCommand(
    Guid ActorUserAccountId,
    Guid ReportId,
    AbuseReportStatus Decision,
    string Reason);
