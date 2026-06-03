namespace RezSaaS.Modules.TenantManagement.Application;

public sealed record ChangeTenantMembershipStatusCommand(
    Guid TenantId,
    Guid MembershipId,
    Guid ActorUserAccountId);
