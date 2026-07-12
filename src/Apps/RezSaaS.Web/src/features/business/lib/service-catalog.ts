/**
 * KATALOG (HIZMET + VARYANT) SUNUM VE SOZLESME MANTIGI.
 *
 * Saf fonksiyonlar: React yok, `@/` alias yok, ag cagrisi yok. Boylece bu modul
 * `node --test` ile dogrudan calisabilir (nav-manifest.ts ile ayni gerekce).
 * Varyant istek govdesi de BURADA uretilir -- cunku asil tuzak (asagida) ancak
 * test edilebilir bir yerde durursa gercekten kapanir.
 */

/* ---------------------------------------------------------------------------
   BACKEND SINIRLARI
   Kaynak: Catalog modulu ServiceManagementService / ServiceVariantManagementService.
   UI burada durdurmazsa kullanici sinir ihlalini ancak sunucudan 400 olarak ogrenir.
   --------------------------------------------------------------------------- */

export const NAME_MIN = 2;
export const NAME_MAX = 160;
export const CATEGORY_MIN = 2;
export const CATEGORY_MAX = 80;
export const DURATION_MIN = 1;
export const DURATION_MAX = 1440;

/**
 * Katalogun TEK para birimi.
 *
 * Backend CurrencyCode'u serbest 3-karakter metin olarak tutar; ISO whitelist YOKTUR.
 * Kullaniciya secim actigimiz an ayni katalogda "TRY" ve "USD" varyantlar yan yana
 * gelebilir ve fiyat karsilastirmasi anlamsizlasir. Bu yuzden para birimi bir UI
 * alani DEGIL, bir SABITTIR.
 */
export const CATALOG_CURRENCY = "TRY";

/* ---------------------------------------------------------------------------
   VARYANT SOZLESMESI -- "PATCH adinda ama PUT davranisinda" tuzagi
   --------------------------------------------------------------------------- */

/**
 * Bir varyantin TAM hali.
 *
 * TUZAK: `PATCH /api/business/services/{id}/variants/{variantId}` adi PATCH ama
 * davranisi PUT'tur. Backend (ServiceVariantManagementService.UpdateAsync) gelen
 * govdeyi kosulsuz uygular:
 *
 *     variant.Rename(name);
 *     variant.UpdateDuration(durationMinutes);
 *     variant.UpdatePricing(priceAmount);
 *     variant.UpdateResourceType(requiredResourceTypeId);
 *
 * Yani alan GONDERMEMEK "dokunma" demek DEGIL:
 *   - durationMinutes yollanmazsa JSON'da 0'a duser -> "duration <= 0" -> 400.
 *   - requiredResourceTypeId yollanmazsa null'a duser -> kaynak turu SESSIZCE SILINIR.
 *
 * Bu yuzden burada HICBIR ALAN OPSIYONEL DEGIL. Kismi bir nesne bu tipe atanamaz;
 * eksik alanli gonderim DERLEME HATASI olur. Tuzak tip sistemiyle kapatilir.
 *
 * `currencyCode` BILEREK YOK: cagiran taraf para birimi secemez (bkz. CATALOG_CURRENCY).
 */
export type VariantFormValues = {
  readonly name: string;
  readonly durationMinutes: number;
  readonly priceAmount: number;
  /** null = "belirli bir kaynak turu gerekmiyor". Yollanmasi ZORUNLU, degeri opsiyonel. */
  readonly requiredResourceTypeId: string | null;
};

/** Tel uzerinden giden varyant govdesi. 5 alanin BESI de her zaman burada. */
export type VariantRequestPayload = {
  readonly name: string;
  readonly durationMinutes: number;
  readonly priceAmount: number;
  readonly currencyCode: string;
  readonly requiredResourceTypeId: string | null;
};

/**
 * Varyant govdesini ureten TEK yer. Hem CREATE hem UPDATE bunu kullanir.
 *
 * Govde tek bir fonksiyondan ciktigi icin "bir alani yollamayi unutmak" mumkun degil:
 * unutulacak alan yok, hepsi VariantFormValues'tan zorunlu olarak geliyor.
 */
export function buildVariantPayload(values: VariantFormValues): VariantRequestPayload {
  return {
    name: values.name.trim(),
    durationMinutes: values.durationMinutes,
    priceAmount: values.priceAmount,
    currencyCode: CATALOG_CURRENCY,
    requiredResourceTypeId: values.requiredResourceTypeId
  };
}

/**
 * Hizmet govdesi. Servis PATCH'i de kategoriyi kosulsuz uygular
 * (service.Rename + service.UpdateCategory), dolayisiyla ayni kural: iki alan da zorunlu.
 */
export type ServiceFormValues = {
  readonly name: string;
  readonly categoryKey: string;
};

export function buildServicePayload(values: ServiceFormValues) {
  return {
    name: values.name.trim(),
    categoryKey: values.categoryKey.trim()
  };
}

/* ---------------------------------------------------------------------------
   NORMALIZASYON -- tel uzerindeki opsiyonelligi SINIRDA cozeriz.

   OpenAPI dokumaninda `required` listesi yok, bu yuzden uretilen tiplerde HER alan
   opsiyonel: `priceAmount?: number`. Oysa backend'de alan non-nullable.

   Bu opsiyonelligi ekrana kadar tasimak her satirda `?? 0` savunmasi demek olurdu --
   ve `priceAmount ?? 0` gercek bir fiyati SESSIZCE "0,00 TL" diye gosterir. Fiyat
   ekraninda bundan daha kotu bir hata yok. Donusumu tek yerde yapiyoruz: zorunlu alani
   eksik gelen kayit ISLENEMEZ sayilir ve listeye hic girmez (uydurma deger konmaz).

   Girdi tipleri YAPISAL yazildi (ApiSchema import edilmedi): bu modul `@/` alias'i
   olmadan `node --test` ile calisabilmeli.
   --------------------------------------------------------------------------- */

export type CatalogService = {
  readonly id: string;
  readonly name: string;
  readonly categoryKey: string;
  readonly status: string;
};

export type CatalogVariant = {
  readonly id: string;
  readonly serviceId: string;
  readonly name: string;
  readonly durationMinutes: number;
  readonly priceAmount: number;
  readonly currencyCode: string;
  readonly requiredResourceTypeId: string | null;
};

export type CatalogResourceType = {
  readonly id: string;
  readonly displayName: string;
};

/**
 * Hizmet + varyantlari. Ekranin calistigi asil tip.
 *
 * Sunucu modulunde DEGIL burada duruyor: client component'ler bu tipi kullaniyor ve
 * "server-only" isaretli bir modulu -- tip icin bile olsa -- import grafiklerine
 * sokmalari gerekmemeli.
 */
export type ServiceWithVariants = CatalogService & {
  readonly variants: CatalogVariant[];
  /**
   * Varyant listesi CEKILEMEDI (hizmetin varyanti yok DEMEK DEGIL).
   *
   * Ayrimi tutmak sart: bos liste "fiyat yok" diye gosterilir ve "arsivlenebilir"
   * sayilir. Cekilemeyen listeyi bos saymak, kullaniciya fiyatlari yokmus gibi gosterir
   * ve varyantli bir hizmeti silmeye calismaya iter.
   */
  readonly variantsUnavailable: boolean;
};

type WireService = {
  id?: string;
  name?: string | null;
  categoryKey?: string | null;
  status?: string | null;
};

type WireVariant = {
  id?: string;
  serviceId?: string;
  name?: string | null;
  durationMinutes?: number;
  priceAmount?: number;
  currencyCode?: string | null;
  requiredResourceTypeId?: string | null;
};

type WireResourceType = {
  id?: string;
  key?: string | null;
  displayName?: string | null;
};

export function toCatalogService(wire: WireService): CatalogService | null {
  if (!wire.id) {
    return null;
  }

  return {
    categoryKey: wire.categoryKey?.trim() ?? "",
    id: wire.id,
    name: wire.name?.trim() || "Adsız hizmet",
    status: wire.status?.trim() ?? ""
  };
}

export function toCatalogVariant(wire: WireVariant): CatalogVariant | null {
  // Fiyat ve sure UYDURULAMAZ. Eksiklerse kayit gosterilmez.
  if (
    !wire.id ||
    !wire.serviceId ||
    typeof wire.durationMinutes !== "number" ||
    typeof wire.priceAmount !== "number"
  ) {
    return null;
  }

  return {
    currencyCode: normalizeCurrency(wire.currencyCode),
    durationMinutes: wire.durationMinutes,
    id: wire.id,
    name: wire.name?.trim() || "Adsız seçenek",
    priceAmount: wire.priceAmount,
    requiredResourceTypeId: wire.requiredResourceTypeId ?? null,
    serviceId: wire.serviceId
  };
}

export function toCatalogResourceType(
  wire: WireResourceType
): CatalogResourceType | null {
  if (!wire.id) {
    return null;
  }

  return {
    displayName: wire.displayName?.trim() || wire.key?.trim() || "Adsız tür",
    id: wire.id
  };
}

/** Cevrilemeyen kayitlari eleyen yardimci. */
export function mapDefined<TIn, TOut>(
  items: readonly TIn[],
  map: (item: TIn) => TOut | null
): TOut[] {
  const result: TOut[] = [];

  for (const item of items) {
    const mapped = map(item);

    if (mapped !== null) {
      result.push(mapped);
    }
  }

  return result;
}

/* ---------------------------------------------------------------------------
   ARSIVLEME ON KOSULU
   --------------------------------------------------------------------------- */

/**
 * DIKKAT -- ucun adi "archive" ama davranisi KALICI SILME.
 *
 * ServiceManagementService.ArchiveAsync, domaindeki Service.Archive() metodunu
 * CAGIRMAZ; dogrudan `dbContext.Services.Remove(service)` yapar. Ayrica varyanti olan
 * hizmeti reddeder (409 SERVICE_HAS_VARIANTS).
 *
 * Sonuc: hizmet "arsivlenmis" olarak listede kalmaz, TAMAMEN kaybolur ve geri alinamaz.
 * UI bunu gizlemez: onay metni gercegi soyler, varyant varsa islem hic baslatilmaz.
 */
export function canArchiveService(variantCount: number): boolean {
  return variantCount === 0;
}

/* ---------------------------------------------------------------------------
   BICIMLEME
   --------------------------------------------------------------------------- */

type PricedVariant = {
  readonly priceAmount: number;
  readonly currencyCode?: string | null;
};

function normalizeCurrency(code: string | null | undefined): string {
  const normalized = (code ?? "").trim().toUpperCase();
  return normalized === "" ? CATALOG_CURRENCY : normalized;
}

/**
 * Para bicimleme.
 *
 * CurrencyCode serbest metin oldugu icin gecersiz bir kod (or. "XX") Intl'i RangeError
 * ile patlatir. Patlamak yerine kodu ham yazariz: yanlis bir para birimi SEMBOLU basmak,
 * hic sembol basmamaktan daha kotudur.
 */
export function formatPrice(
  amount: number,
  currencyCode: string | null | undefined = CATALOG_CURRENCY
): string {
  const code = normalizeCurrency(currencyCode);

  try {
    return new Intl.NumberFormat("tr-TR", {
      currency: code,
      maximumFractionDigits: 2,
      minimumFractionDigits: 2,
      style: "currency"
    }).format(amount);
  } catch {
    return `${amount.toFixed(2)} ${code}`;
  }
}

/** 30 -> "30 dk", 60 -> "1 sa", 90 -> "1 sa 30 dk". */
export function formatDuration(minutes: number): string {
  if (!Number.isFinite(minutes) || minutes <= 0) {
    return "—";
  }

  const hours = Math.floor(minutes / 60);
  const rest = Math.round(minutes % 60);

  if (hours === 0) {
    return `${rest} dk`;
  }

  if (rest === 0) {
    return `${hours} sa`;
  }

  return `${hours} sa ${rest} dk`;
}

/**
 * Hizmet satirindaki fiyat araligi ("en dusuk - en yuksek").
 *
 * Karisik para birimi bir ANOMALIDIR (panel yalnizca TRY yazar) ama backend engellemez.
 * Boyle bir durumda tek bir aralik cizmek "400 TL - 600 TL" gibi YANLIS bir cumle kurar;
 * onun yerine fiyatlari kendi kodlariyla tek tek yazariz.
 */
export function formatPriceRange(variants: readonly PricedVariant[]): string {
  if (variants.length === 0) {
    return "Fiyat yok";
  }

  const base = normalizeCurrency(variants[0]?.currencyCode);
  const mixed = variants.some(
    (variant) => normalizeCurrency(variant.currencyCode) !== base
  );

  if (mixed) {
    return variants
      .map((variant) => formatPrice(variant.priceAmount, variant.currencyCode))
      .join(" · ");
  }

  const prices = variants.map((variant) => variant.priceAmount);
  const min = Math.min(...prices);
  const max = Math.max(...prices);

  return min === max
    ? formatPrice(min, base)
    : `${formatPrice(min, base)} – ${formatPrice(max, base)}`;
}

export type ServiceStatusView = {
  readonly label: string;
  readonly tone: "active" | "archived" | "unknown";
};

/** Rozet METIN tasir; renk tek sinyal degildir (dokunmatik + renk korlugu). */
export function describeServiceStatus(
  status: string | null | undefined
): ServiceStatusView {
  switch ((status ?? "").trim().toLowerCase()) {
    case "active":
      return { label: "Aktif", tone: "active" };
    case "archived":
      return { label: "Arşivlendi", tone: "archived" };
    default: {
      const raw = (status ?? "").trim();
      return { label: raw === "" ? "Bilinmiyor" : raw, tone: "unknown" };
    }
  }
}

/* ---------------------------------------------------------------------------
   GIRDI AYRISTIRMA
   --------------------------------------------------------------------------- */

/**
 * Fiyat girdisi. Salon sahibi "400,50" yazar; "400.50" da kabul edilir.
 * Kurus (2 ondalik) desteklenir. Gecersizse null -> cagiran taraf hata gosterir.
 *
 * Binlik ayirici DESTEKLENMEZ: Turkce'de binlik "." ondalik ","dir, ama kullanicilar
 * ikisini karistirir. "1.250" girdisini 1250 mi 1.25 mi saymak gerektigi BELIRSIZ --
 * belirsiz girdiyi sessizce yorumlamak yerine reddediyoruz.
 */
export function parsePriceInput(raw: string): number | null {
  const compact = raw.trim().replace(/\s/g, "");

  if (compact === "") {
    return null;
  }

  const normalized = compact.replace(",", ".");

  if (!/^\d+(\.\d{1,2})?$/.test(normalized)) {
    return null;
  }

  const value = Number(normalized);

  return Number.isFinite(value) && value >= 0 ? value : null;
}

/** Sure girdisi: tam sayi dakika, backend siniri 1..1440. */
export function parseDurationInput(raw: string): number | null {
  const compact = raw.trim();

  if (!/^\d+$/.test(compact)) {
    return null;
  }

  const value = Number(compact);

  if (!Number.isInteger(value) || value < DURATION_MIN || value > DURATION_MAX) {
    return null;
  }

  return value;
}

/* ---------------------------------------------------------------------------
   HATA METINLERI
   Backend errorCode'lari: ServiceManagementService / ServiceVariantManagementService.
   --------------------------------------------------------------------------- */

export function describeCatalogError(
  errorCode: string | null | undefined,
  httpStatus: number
): string {
  switch ((errorCode ?? "").trim().toUpperCase()) {
    case "SERVICE_NAME_CONFLICT":
      return "Bu adda bir hizmet zaten var. Farklı bir ad seç.";
    case "VARIANT_NAME_CONFLICT":
      return "Bu hizmette aynı adlı bir seçenek zaten var. Farklı bir ad seç.";
    case "SERVICE_HAS_VARIANTS":
      return "Seçenekleri olan hizmet kaldırılamaz. Önce tüm seçenekleri sil.";
    case "SERVICE_NOT_FOUND":
      return "Hizmet bulunamadı. Liste güncel olmayabilir, sayfayı yenile.";
    case "VARIANT_NOT_FOUND":
      return "Seçenek bulunamadı. Liste güncel olmayabilir, sayfayı yenile.";
    case "SERVICE_INVALID_REQUEST":
    case "VARIANT_INVALID_REQUEST":
      return "Girilen bilgiler geçersiz. Ad, süre ve fiyatı kontrol et.";
    case "MISSING_TENANT_CONTEXT":
      return "İşletme bilgisi doğrulanamadı. Sayfayı yenileyip tekrar dene.";
    default:
      break;
  }

  if (httpStatus === 403) {
    return "Bu işlem için yetkin yok.";
  }

  if (httpStatus === 401) {
    return "Oturumun sonlanmış görünüyor. Tekrar giriş yap.";
  }

  return "İşlem şu anda tamamlanamadı. Lütfen tekrar dene.";
}
