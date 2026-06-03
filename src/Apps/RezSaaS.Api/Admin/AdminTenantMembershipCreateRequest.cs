namespace RezSaaS.Api.Admin;

public sealed record AdminTenantMembershipCreateRequest(
    Guid UserAccountId,
    string Role,
    Guid? BranchId);
