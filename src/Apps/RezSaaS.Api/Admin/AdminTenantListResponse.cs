namespace RezSaaS.Api.Admin;

public sealed record AdminTenantListResponse(
    IReadOnlyCollection<AdminTenantListItemResponse> Tenants);
