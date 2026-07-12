namespace RezSaaS.Modules.Organization.Application;

public sealed record UpdateBusinessProfileSettingsCommand(
    Guid ActorUserAccountId,
    string DisplayName,
    string Description,
    string PublicRules,
    string SeoTitle,
    string SeoDescription,
    string StaffDisplayPolicy,
    // Musterinin onaylanmis randevusunu iptal edebilecegi son an (saat). 0 = kural yok.
    //
    // NULLABLE: gonderilmezse MEVCUT DEGER KORUNUR, sifirlanmaz.
    // Diger alanlar "PATCH ama davranisi PUT" kalibinda (hepsi her seferinde gonderilir);
    // burada ayni sey yapilsaydi, alani gondermeyi unutan bir istemci isletmenin iptal
    // politikasini SESSIZCE 0'a (kural yok) dusururdu.
    int? CancellationCutoffHours = null);
