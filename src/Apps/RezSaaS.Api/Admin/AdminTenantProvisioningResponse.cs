namespace RezSaaS.Api.Admin;

public sealed record AdminTenantProvisioningResponse(
    Guid TenantId,
    string Slug,
    string DisplayName,
    Guid OwnerUserAccountId,
    // Provisioning artik salonun public kimligini (Business) de olusturuyor.
    // Cagiran taraf salonun gercekten yayina hazir oldugunu buradan dogrulayabilir.
    Guid BusinessId);
