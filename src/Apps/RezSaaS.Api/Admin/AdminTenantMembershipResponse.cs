namespace RezSaaS.Api.Admin;

public sealed record AdminTenantMembershipResponse(
    Guid MembershipId,
    Guid TenantId,
    Guid UserAccountId,
    string Role,
    string Status,
    Guid? BranchId,
    DateTimeOffset CreatedAtUtc);
