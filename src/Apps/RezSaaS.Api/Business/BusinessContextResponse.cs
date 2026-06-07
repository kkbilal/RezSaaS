namespace RezSaaS.Api.Business;

public sealed record BusinessContextResponse(
    IReadOnlyCollection<BusinessTenantContextResponse> Tenants);

public sealed record BusinessTenantContextResponse(
    Guid MembershipId,
    Guid TenantId,
    string TenantSlug,
    string TenantDisplayName,
    string Role,
    Guid? BranchId,
    bool IsTenantWide,
    IReadOnlyCollection<string> Capabilities);
