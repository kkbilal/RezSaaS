using RezSaaS.Modules.TenantManagement.Domain;

namespace RezSaaS.Modules.TenantManagement.Application;

public sealed record TenantMembershipScopeView(
    TenantMembershipRole Role,
    Guid? BranchId);
