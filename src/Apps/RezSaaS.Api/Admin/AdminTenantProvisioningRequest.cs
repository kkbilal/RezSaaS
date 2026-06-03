namespace RezSaaS.Api.Admin;

public sealed record AdminTenantProvisioningRequest(
    string Slug,
    string DisplayName,
    Guid OwnerUserAccountId);
