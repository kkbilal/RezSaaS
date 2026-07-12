import {
  buildServicePayload,
  buildVariantPayload,
  describeCatalogError,
  toCatalogService,
  toCatalogVariant,
  type CatalogService,
  type CatalogVariant,
  type ServiceFormValues,
  type VariantFormValues
} from "../lib/service-catalog";
import { createTenantApiClient } from "@/shared/api/client";

/**
 * HIZMET + VARYANT MUTASYONLARININ TEK KAYNAGI.
 *
 * Ekranlar bu modulu cagirir; ham `createTenantApiClient` cagrisi component icinde
 * YASAKTIR (service-catalog.test.ts bunu kaynak metninde dogruluyor). Gerekce:
 * varyant guncelleme ucu "PATCH" adini tasir ama PUT gibi davranir; gonderilmeyen
 * alan korunmaz, SIFIRLANIR. Govdeyi ekran ekran elle kurmak, er ya da gec bir alani
 * unutup varyantin kaynak turunu silmek demektir. Govde YALNIZCA buildVariantPayload'dan cikar.
 *
 * Bu modul TARAYICIDA calisir. Server component'ten import etme.
 */

export type CatalogResult<TData> =
  /**
   * Backend kabul etti.
   *
   * `data` null olabilir: yazma BASARILI ama donen govde beklenen alanlari tasimiyor
   * (or. id yok). Boyle bir durumda yerel listeyi tahminle yamamayiz -- cagiran taraf
   * router.refresh() ile gercegi sunucudan alir.
   */
  | { kind: "success"; data: TData | null }
  /**
   * Backend REDDETTI (4xx/5xx). Yerel liste artik bayat olabilir ->
   * cagiran taraf router.refresh() cagirmali.
   */
  | { kind: "rejected"; message: string }
  /** Ag/istisna. Istek sunucuya ulasmamis olabilir; liste bayat DEGIL -> refresh etme. */
  | { kind: "failed"; message: string };

/** Hata govdesi her iki ucta da `{ errorCode }` seklinde. */
type ErrorBody = { errorCode?: string | null };

function rejected(error: unknown, status: number): CatalogResult<never> {
  const errorCode =
    error && typeof error === "object" && "errorCode" in error
      ? (error as ErrorBody).errorCode
      : null;

  return { kind: "rejected", message: describeCatalogError(errorCode, status) };
}

const NETWORK_FAILURE = "Bağlantı kurulamadı. İnternetini kontrol edip tekrar dene.";

/* ---------------------------------------------------------------------------
   HIZMET
   --------------------------------------------------------------------------- */

export async function createService(
  tenantId: string,
  values: ServiceFormValues
): Promise<CatalogResult<CatalogService>> {
  try {
    const { data, error, response } = await createTenantApiClient(tenantId).POST(
      "/api/business/services",
      { body: buildServicePayload(values) }
    );

    if (!response.ok) {
      return rejected(error, response.status);
    }

    return { data: data ? toCatalogService(data) : null, kind: "success" };
  } catch {
    return { kind: "failed", message: NETWORK_FAILURE };
  }
}

/**
 * Hizmeti yeniden adlandirir VE kategorisini yazar.
 *
 * Ad ve kategori BIRLIKTE gider: backend PATCH'i ikisini de kosulsuz uygular
 * (Rename + UpdateCategory), yani kategoriyi gondermemek onu bozar.
 */
export async function updateService(
  tenantId: string,
  serviceId: string,
  values: ServiceFormValues
): Promise<CatalogResult<CatalogService>> {
  try {
    const { data, error, response } = await createTenantApiClient(tenantId).PATCH(
      "/api/business/services/{serviceId}",
      { body: buildServicePayload(values), params: { path: { serviceId } } }
    );

    if (!response.ok) {
      return rejected(error, response.status);
    }

    return { data: data ? toCatalogService(data) : null, kind: "success" };
  } catch {
    return { kind: "failed", message: NETWORK_FAILURE };
  }
}

/**
 * "Arsivler" -- ama gercekte KALICI SILER.
 *
 * ServiceManagementService.ArchiveAsync domaindeki Service.Archive()'i cagirmaz,
 * `Services.Remove(...)` yapar. Ayrica varyanti olan hizmeti 409 SERVICE_HAS_VARIANTS
 * ile reddeder. Cagiran taraf bunu kullaniciya oldugu gibi anlatmali
 * (bkz. canArchiveService ve ArchiveServiceDialog).
 */
export async function archiveService(
  tenantId: string,
  serviceId: string
): Promise<CatalogResult<CatalogService>> {
  try {
    const { data, error, response } = await createTenantApiClient(tenantId).POST(
      "/api/business/services/{serviceId}/archive",
      { params: { path: { serviceId } } }
    );

    if (!response.ok) {
      return rejected(error, response.status);
    }

    return { data: data ? toCatalogService(data) : null, kind: "success" };
  } catch {
    return { kind: "failed", message: NETWORK_FAILURE };
  }
}

/* ---------------------------------------------------------------------------
   VARYANT -- fiyat ve sure BURADA yasar
   --------------------------------------------------------------------------- */

export async function createVariant(
  tenantId: string,
  serviceId: string,
  values: VariantFormValues
): Promise<CatalogResult<CatalogVariant>> {
  try {
    const { data, error, response } = await createTenantApiClient(tenantId).POST(
      "/api/business/services/{serviceId}/variants",
      { body: buildVariantPayload(values), params: { path: { serviceId } } }
    );

    if (!response.ok) {
      return rejected(error, response.status);
    }

    return { data: data ? toCatalogVariant(data) : null, kind: "success" };
  } catch {
    return { kind: "failed", message: NETWORK_FAILURE };
  }
}

/**
 * Varyanti gunceller. GOVDE TAMDIR -- bes alanin besi de gider.
 *
 * `values` tipi VariantFormValues oldugu icin eksik alanli bir nesne buraya
 * ATANAMAZ; kismi gonderim derleme hatasidir. Tuzak tip sisteminde kapali.
 */
export async function updateVariant(
  tenantId: string,
  serviceId: string,
  variantId: string,
  values: VariantFormValues
): Promise<CatalogResult<CatalogVariant>> {
  try {
    const { data, error, response } = await createTenantApiClient(tenantId).PATCH(
      "/api/business/services/{serviceId}/variants/{variantId}",
      {
        body: buildVariantPayload(values),
        params: { path: { serviceId, variantId } }
      }
    );

    if (!response.ok) {
      return rejected(error, response.status);
    }

    return { data: data ? toCatalogVariant(data) : null, kind: "success" };
  } catch {
    return { kind: "failed", message: NETWORK_FAILURE };
  }
}

export async function deleteVariant(
  tenantId: string,
  serviceId: string,
  variantId: string
): Promise<CatalogResult<null>> {
  try {
    const { error, response } = await createTenantApiClient(tenantId).DELETE(
      "/api/business/services/{serviceId}/variants/{variantId}",
      { params: { path: { serviceId, variantId } } }
    );

    if (!response.ok) {
      return rejected(error, response.status);
    }

    return { data: null, kind: "success" };
  } catch {
    return { kind: "failed", message: NETWORK_FAILURE };
  }
}
