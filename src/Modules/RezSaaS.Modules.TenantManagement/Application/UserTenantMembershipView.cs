using RezSaaS.Modules.TenantManagement.Domain;

namespace RezSaaS.Modules.TenantManagement.Application;

public sealed record UserTenantMembershipView(
    Guid Id,
    Guid TenantId,
    string TenantSlug,
    string TenantDisplayName,
    TenantStatus TenantStatus,
    TenantMembershipRole Role,
    TenantMembershipStatus Status,
    Guid? BranchId,
    DateTimeOffset CreatedAtUtc);
