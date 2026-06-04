namespace RezSaaS.Modules.Admin.Application;

public sealed record AbuseReportControlPlaneQuery(
    Guid? UserAccountId,
    Guid? TenantId,
    string? Status,
    int Take);
