/**
 * YETKI SOZLUGU
 *
 * Bu dosyadaki string'ler backend'in `BusinessCapabilityNames.cs` dosyasiyla BIREBIR aynidir.
 * Uydurma capability YAZILMAZ -- backend'de karsiligi olmayan bir izin, UI'da bir sey acsa bile
 * ilk API cagrisinda 403 doner ve kullaniciyi duvara carptirir.
 *
 * Backend kaynagi:
 *   src/Apps/RezSaaS.Api/Business/BusinessCapabilityNames.cs
 *   src/Apps/RezSaaS.Api/Business/BusinessContextComposer.cs  (rol -> capability eslemesi)
 */

export const BUSINESS_CAPABILITIES = {
  /** Talep onay/red kuyrugu + randevu operasyonlari (cancel/complete/no-show/notes/rebook). */
  manageAppointmentRequests: "business.appointmentRequests.manage",

  /**
   * Katalog, personel, sube, kaynak, calisma saati ve ayarlar.
   *
   * DIKKAT: /api/business altindaki 11 composer'in TAMAMI bu capability'yi arar
   * (TenantBookingAuthorizationService.CanManageBusinessSettingsAsync -> yalnizca BusinessOwner).
   * BranchManager'da bu capability YOKTUR; ona bu ogeleri gostermek, her ogesi 403 tuzagi
   * olan bir menu gostermektir.
   */
  manageSettings: "business.settings.manage",

  /** Slot spam / suistimal bildirimi. */
  reportAbuse: "business.appointmentRequests.reportAbuse"
} as const;

export type BusinessCapability =
  (typeof BUSINESS_CAPABILITIES)[keyof typeof BUSINESS_CAPABILITIES];

/**
 * Bir rota veya nav ogesi icin gereken izin.
 *
 * Bu bir UNION'dir ve nav-manifest'te ZORUNLU alandir (opsiyonel degil). Boylece yeni bir
 * sayfa eklerken izin yazmayi unutmak DERLEME HATASI verir -- "izin yazmayi unuttum, sayfa
 * herkese acildi" (fail-open) hatasi tip sistemi tarafindan engellenir.
 *
 * Referans projelerde bu koruma yoktu: biri API cokunce statik tam menuye dusuyordu
 * (en dusuk yetkili kullanici tam yonetici menusunu goruyordu), digerinde `canWrite` boolean'i
 * 161 dosyaya prop olarak elden ele geciyordu.
 */
export type Permission =
  /** Anonim erisim. */
  | "public"
  /** Herhangi bir aktif, kimligi dogrulanmis hesap (capability gerekmez). */
  | "auth"
  /** Aktif tenant'ta bu capability gerekir. */
  | BusinessCapability
  /** PlatformAdmin rolu gerekir. (Step-up kontrolu backend'de; UI ayrica gate gosterir.) */
  | "platform.admin";

const KNOWN_BUSINESS_CAPABILITIES = new Set<string>(
  Object.values(BUSINESS_CAPABILITIES)
);

/**
 * Backend'den gelen ham string'i bilinen bir capability'ye daraltir.
 * Taninmayan degeri REDDEDER -- backend yeni bir capability eklerse UI onu sessizce
 * "bilinen" saymaz; once buraya yazilmasi gerekir.
 */
export function isBusinessCapability(value: string): value is BusinessCapability {
  return KNOWN_BUSINESS_CAPABILITIES.has(value);
}
