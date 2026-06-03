using RezSaaS.Modules.TenantManagement.Domain;

namespace RezSaaS.Modules.TenantManagement.Application;

public sealed record AddTenantMembershipCommand(
    Guid TenantId,
    Guid ActorUserAccountId,
    Guid UserAccountId,
    TenantMembershipRole Role,
    Guid? BranchId);
