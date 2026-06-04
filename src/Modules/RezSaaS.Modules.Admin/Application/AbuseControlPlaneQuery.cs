namespace RezSaaS.Modules.Admin.Application;

public sealed record AbuseControlPlaneQuery(
    Guid? UserAccountId,
    Guid? TenantId,
    string? Severity,
    int Take);
