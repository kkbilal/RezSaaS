namespace RezSaaS.Api.Business;

public sealed record BusinessProfileSettingsResponse(
    Guid BusinessId,
    string Slug,
    string DisplayName,
    string CategoryKey,
    string Description,
    string PublicRules,
    string SeoTitle,
    string SeoDescription,
    string StaffDisplayPolicy,
    // BUG FIX: bu alan EKSIKTI -- politika PATCH ile yazilabiliyor ama GET'te HIC donmuyordu.
    // "PATCH ama davranisi PUT" olan bir ucta istemci GET->PATCH round-trip YAPAMAZDI:
    // mevcut degeri okuyamadigi icin her kaydetmede politikayi kaybederdi.
    int CancellationCutoffHours);
