import { createTenantApiClient } from "@/shared/api/client";
import type { ApiSchema } from "@/shared/api/types";

/**
 * KAYNAK (koltuk/oda/cihaz) + KAYNAK TIPI (ekipman turu) MUTASYONLARININ TEK KAYNAGI.
 *
 * IKI SEVIYE:
 *  1. Ekipman turleri (resource-types) TENANT seviyesinde -- branchId'siz.
 *     GET/POST /api/business/resource-types, DELETE .../{id}
 *  2. Kaynaklar (resources) SUBE altinda nested -- branchId ZORUNLU.
 *     GET/POST /api/business/branches/{branchId}/resources
 *     PATCH .../{resourceId} (rename), POST .../out-of-service, POST .../restore
 *
 * Bir kaynak olusturmak icin ONCE en az bir ekipman turu gerekir (resourceTypeId zorunlu).
 * status backend enum ToString: "Active" | "OutOfService".
 *
 * Bu modul TARAYICIDA calisir. Server component'ten import etme.
 */

export type BusinessResource = ApiSchema<"BusinessResourceResponse">;
export type BusinessResourceType = ApiSchema<"BusinessResourceTypeResponse">;

export type ResourceResult<TData> =
  /** Backend kabul etti. `data` null olabilir (govde beklenen alanlari tasimiyor). */
  | { kind: "success"; data: TData | null }
  /** Backend REDDETTI (4xx/5xx). Yerel liste bayat olabilir -> cagiran taraf refresh etmeli. */
  | { kind: "rejected"; message: string }
  /** Ag/istisna. Istek sunucuya ulasmamis olabilir -> refresh ETME (yazilani sifirlar). */
  | { kind: "failed"; message: string };

type ErrorBody = { errorCode?: string | null };

const NETWORK_FAILURE = "Bağlantı kurulamadı. İnternetini kontrol edip tekrar dene.";

/** Hata kodunu salon sahibinin diline cevirir. Backend'deki sabitlerle birebir. */
function describeError(errorCode: string | null | undefined, status: number): string {
  switch (errorCode) {
    // Kaynak
    case "RESOURCE_INVALID_REQUEST":
      return "Ad 2 ile 160 karakter arasında olmalı.";
    case "RESOURCE_NOT_FOUND":
      return "Kaynak bulunamadı. Sayfayı yenileyip tekrar dene.";
    case "RESOURCE_TYPE_NOT_FOUND":
      return "Seçilen ekipman türü bulunamadı. Sayfayı yenileyip tekrar dene.";
    case "RESOURCE_TYPE_INACTIVE":
      return "Seçilen ekipman türü artık kullanılamıyor.";
    // Ekipman turu
    case "RESOURCE_TYPE_INVALID_REQUEST":
      return "Anahtar ve görünen ad 2 ile 160 karakter arasında olmalı.";
    case "RESOURCE_TYPE_KEY_CONFLICT":
      return "Bu anahtar zaten kullanımda. Farklı bir anahtar gir.";
    case "RESOURCE_TYPE_IN_USE":
      return "Bu türe bağlı kaynaklar var. Önce o kaynakları sil, sonra türü sil.";
    default:
      if (status === 403) return "Bu işlem için yetkin yok.";
      if (status === 401) return "Oturumun düşmüş görünüyor. Tekrar giriş yap.";
      return "İşlem tamamlanamadı. Tekrar dene.";
  }
}

function rejected(error: unknown, status: number): ResourceResult<never> {
  const errorCode =
    error && typeof error === "object" && "errorCode" in error
      ? (error as ErrorBody).errorCode
      : null;

  return { kind: "rejected", message: describeError(errorCode, status) };
}

/* ---------------------------------------------------------------------------
   EKIPMAN TURLERI (resource-types) -- tenant seviyesinde, branchId'siz
   --------------------------------------------------------------------------- */

export async function listResourceTypes(
  tenantId: string
): Promise<ResourceResult<BusinessResourceType[]>> {
  try {
    const { data, error, response } = await createTenantApiClient(tenantId).GET(
      "/api/business/resource-types"
    );

    if (!response.ok) {
      return rejected(error, response.status);
    }

    return { kind: "success", data: data ?? [] };
  } catch {
    return { kind: "failed", message: NETWORK_FAILURE };
  }
}

export async function createResourceType(
  tenantId: string,
  input: { key: string; displayName: string }
): Promise<ResourceResult<BusinessResourceType>> {
  try {
    const { data, error, response } = await createTenantApiClient(tenantId).POST(
      "/api/business/resource-types",
      { body: { key: input.key, displayName: input.displayName } }
    );

    if (!response.ok) {
      return rejected(error, response.status);
    }

    return { kind: "success", data: data ?? null };
  } catch {
    return { kind: "failed", message: NETWORK_FAILURE };
  }
}

export async function deleteResourceType(
  tenantId: string,
  resourceTypeId: string
): Promise<ResourceResult<BusinessResourceType>> {
  try {
    const { data, error, response } = await createTenantApiClient(tenantId).DELETE(
      "/api/business/resource-types/{resourceTypeId}",
      { params: { path: { resourceTypeId } } }
    );

    if (!response.ok) {
      return rejected(error, response.status);
    }

    return { kind: "success", data: data ?? null };
  } catch {
    return { kind: "failed", message: NETWORK_FAILURE };
  }
}

/* ---------------------------------------------------------------------------
   KAYNAKLAR (resources) -- sube altinda nested (branchId zorunlu)
   --------------------------------------------------------------------------- */

export async function listResources(
  tenantId: string,
  branchId: string
): Promise<ResourceResult<BusinessResource[]>> {
  try {
    const { data, error, response } = await createTenantApiClient(tenantId).GET(
      "/api/business/branches/{branchId}/resources",
      { params: { path: { branchId } } }
    );

    if (!response.ok) {
      return rejected(error, response.status);
    }

    return { kind: "success", data: data ?? [] };
  } catch {
    return { kind: "failed", message: NETWORK_FAILURE };
  }
}

export async function createResource(
  tenantId: string,
  branchId: string,
  input: { resourceTypeId: string; displayName: string }
): Promise<ResourceResult<BusinessResource>> {
  try {
    const { data, error, response } = await createTenantApiClient(tenantId).POST(
      "/api/business/branches/{branchId}/resources",
      {
        body: {
          resourceTypeId: input.resourceTypeId,
          displayName: input.displayName
        },
        params: { path: { branchId } }
      }
    );

    if (!response.ok) {
      return rejected(error, response.status);
    }

    return { kind: "success", data: data ?? null };
  } catch {
    return { kind: "failed", message: NETWORK_FAILURE };
  }
}

export async function renameResource(
  tenantId: string,
  branchId: string,
  resourceId: string,
  displayName: string
): Promise<ResourceResult<BusinessResource>> {
  try {
    const { data, error, response } = await createTenantApiClient(tenantId).PATCH(
      "/api/business/branches/{branchId}/resources/{resourceId}",
      { body: { displayName }, params: { path: { branchId, resourceId } } }
    );

    if (!response.ok) {
      return rejected(error, response.status);
    }

    return { kind: "success", data: data ?? null };
  } catch {
    return { kind: "failed", message: NETWORK_FAILURE };
  }
}

export async function markResourceOutOfService(
  tenantId: string,
  branchId: string,
  resourceId: string
): Promise<ResourceResult<BusinessResource>> {
  try {
    const { data, error, response } = await createTenantApiClient(tenantId).POST(
      "/api/business/branches/{branchId}/resources/{resourceId}/out-of-service",
      { params: { path: { branchId, resourceId } } }
    );

    if (!response.ok) {
      return rejected(error, response.status);
    }

    return { kind: "success", data: data ?? null };
  } catch {
    return { kind: "failed", message: NETWORK_FAILURE };
  }
}

export async function restoreResource(
  tenantId: string,
  branchId: string,
  resourceId: string
): Promise<ResourceResult<BusinessResource>> {
  try {
    const { data, error, response } = await createTenantApiClient(tenantId).POST(
      "/api/business/branches/{branchId}/resources/{resourceId}/restore",
      { params: { path: { branchId, resourceId } } }
    );

    if (!response.ok) {
      return rejected(error, response.status);
    }

    return { kind: "success", data: data ?? null };
  } catch {
    return { kind: "failed", message: NETWORK_FAILURE };
  }
}

/* ---------------------------------------------------------------------------
   GORUNUM YARDIMCILARI
   --------------------------------------------------------------------------- */

export type ResourceStatusView = {
  label: string;
  tone: "active" | "inactive";
  isOutOfService: boolean;
};

/** status backend enum'unun ToString'i: "Active" | "OutOfService". */
export function describeResourceStatus(
  status: string | null | undefined
): ResourceStatusView {
  switch (status) {
    case "Active":
      return { label: "Hizmette", tone: "active", isOutOfService: false };
    case "OutOfService":
      return { label: "Hizmet dışı", tone: "inactive", isOutOfService: true };
    default:
      return { label: status ?? "Bilinmiyor", tone: "inactive", isOutOfService: false };
  }
}
