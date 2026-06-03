namespace RezSaaS.Api.Admin;

public sealed record AdminTenantProvisioningResponse(
    Guid TenantId,
    string Slug,
    string DisplayName,
    Guid OwnerUserAccountId);
