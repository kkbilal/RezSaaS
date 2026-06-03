namespace RezSaaS.Api.Admin;

public sealed record AdminTenantMembershipListResponse(
    IReadOnlyCollection<AdminTenantMembershipResponse> Memberships);
