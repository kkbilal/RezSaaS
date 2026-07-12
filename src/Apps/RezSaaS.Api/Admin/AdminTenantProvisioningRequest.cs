namespace RezSaaS.Api.Admin;

public sealed record AdminTenantProvisioningRequest(
    string Slug,
    string DisplayName,
    Guid OwnerUserAccountId,
    // Salonun public kimligi (Business) ARTIK bu uc tarafindan olusturuluyor.
    // Kategori zorunlu: Business.Create bunu invariant olarak sart kosuyor ve public
    // kesif (/kesfet) kategoriye gore filtreliyor.
    // Onceden Business HIC olusturulmuyordu -> owner sube bile acamiyordu (BUSINESS_NOT_FOUND)
    // ve salon /kesfet'te hic gorunmuyordu. Bkz. BusinessProvisioningService.
    string CategoryKey);
