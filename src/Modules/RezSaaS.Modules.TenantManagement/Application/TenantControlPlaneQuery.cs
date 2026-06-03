namespace RezSaaS.Modules.TenantManagement.Application;

public sealed record TenantControlPlaneQuery(
    string? Search,
    string? Status,
    int Take);
