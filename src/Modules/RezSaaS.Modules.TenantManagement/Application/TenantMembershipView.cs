using RezSaaS.Modules.TenantManagement.Domain;

namespace RezSaaS.Modules.TenantManagement.Application;

public sealed record TenantMembershipView(
    Guid Id,
    Guid TenantId,
    Guid UserAccountId,
    TenantMembershipRole Role,
    TenantMembershipStatus Status,
    Guid? BranchId,
    DateTimeOffset CreatedAtUtc);
