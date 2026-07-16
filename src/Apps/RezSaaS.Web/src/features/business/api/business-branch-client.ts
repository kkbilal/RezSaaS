import { createTenantApiClient } from "@/shared/api/client";
import type { ApiSchema } from "@/shared/api/types";

/**
 * SUBE MUTASYONLARININ TEK KAYNAGI (tarayici tarafi).
 *
 * Ekranlar bu modulu cagirir; ham `createTenantApiClient` component icinde kullanilmaz.
 * Personel client'iyle ayni ucuclu sonuc modelini kullanir (success/rejected/failed) ki
 * cagiran taraf "backend reddetti" (liste bayat -> refresh) ile "ag koptu" (yazilani
 * koru -> refresh ETME) arasini ayirt edebilsin.
 *
 * TUZAK 3 (TIMEZONE): backend artik gecersiz TZ'yi 400 BRANCH_INVALID_TIMEZONE ile
 * reddediyor. Yine de form serbest metin YERINE kuratorlu bir IANA listesi sunar;
 * bu hata sadece son emniyet supabi olarak anlasilir metne cevrilir.
 */

export type BusinessBranchResponse = ApiSchema<"BusinessBranchResponse">;

export type CreateBranchRequest = {
  slug: string;
  displayName: string;
  timeZoneId: string;
  city?: string;
  district?: string;
  addressLine?: string;
};

export type UpdateBranchRequest = {
  displayName: string;
  city?: string;
  district?: string;
  addressLine?: string;
};

export type UpdateBranchSlotSettingsRequest = {
  slotIntervalMinutes: number | null;
  maxPublicSlots: number | null;
};

export type BranchResult<TData> =
  /** Backend kabul etti. `data` null olabilir (govde beklenen alanlari tasimiyor). */
  | { kind: "success"; data: TData | null }
  /** Backend REDDETTI (4xx/5xx). Yerel liste bayat olabilir -> cagiran taraf refresh etmeli. */
  | { kind: "rejected"; message: string }
  /** Ag/istisna. Istek sunucuya ulasmamis olabilir -> refresh ETME (yazilani sifirlar). */
  | { kind: "failed"; message: string };

type ErrorBody = { errorCode?: string | null };

const NETWORK_FAILURE = "Bağlantı kurulamadı. İnternetini kontrol edip tekrar dene.";

/** Backend'deki BranchManagementService sabitleriyle birebir; salon sahibinin diline cevirir. */
function describeBranchError(errorCode: string | null | undefined, status: number): string {
  switch (errorCode) {
    case "BRANCH_INVALID_REQUEST":
      return "Şube bilgileri geçerli değil. Şube kodu ve adı 2 ile 64/200 karakter arasında olmalı.";
    case "BRANCH_INVALID_TIMEZONE":
      // Kuratorlu Select ile bu neredeyse imkansiz; yine de anlasilir kalsin.
      return "Seçilen zaman dilimi tanınmadı. Lütfen listeden geçerli bir zaman dilimi seç.";
    case "BRANCH_SLUG_CONFLICT":
      return "Bu şube kodu zaten kullanılıyor. Farklı bir kod gir.";
    case "BRANCH_HAS_STAFF":
      return "Bu şubeye bağlı personel var. Önce personeli başka bir şubeye taşı veya arşivle, sonra şubeyi arşivle.";
    case "BRANCH_NOT_FOUND":
      return "Şube bulunamadı. Sayfayı yenileyip tekrar dene.";
    case "BUSINESS_NOT_FOUND":
      return "Aktif işletme bulunamadı. Sayfayı yenileyip tekrar dene.";
    default:
      if (status === 403) return "Bu işlem için yetkin yok.";
      if (status === 401) return "Oturumun düşmüş görünüyor. Tekrar giriş yap.";
      if (status === 429) return "Çok sık işlem denendi. Kısa süre sonra tekrar dene.";
      return "İşlem tamamlanamadı. Tekrar dene.";
  }
}

function rejected(error: unknown, status: number): BranchResult<never> {
  const errorCode =
    error && typeof error === "object" && "errorCode" in error
      ? (error as ErrorBody).errorCode
      : null;

  return { kind: "rejected", message: describeBranchError(errorCode, status) };
}

export async function createBranch(
  tenantId: string,
  request: CreateBranchRequest
): Promise<BranchResult<BusinessBranchResponse>> {
  try {
    const { data, error, response } = await createTenantApiClient(tenantId).POST(
      "/api/business/branches",
      { body: request as never }
    );

    if (!response.ok) {
      return rejected(error, response.status);
    }

    return { kind: "success", data: data ?? null };
  } catch {
    return { kind: "failed", message: NETWORK_FAILURE };
  }
}

export async function updateBranch(
  tenantId: string,
  branchId: string,
  request: UpdateBranchRequest
): Promise<BranchResult<BusinessBranchResponse>> {
  try {
    const { data, error, response } = await createTenantApiClient(tenantId).PATCH(
      "/api/business/branches/{branchId}",
      { params: { path: { branchId } }, body: request as never }
    );

    if (!response.ok) {
      return rejected(error, response.status);
    }

    return { kind: "success", data: data ?? null };
  } catch {
    return { kind: "failed", message: NETWORK_FAILURE };
  }
}

export async function updateBranchSlotSettings(
  tenantId: string,
  branchId: string,
  request: UpdateBranchSlotSettingsRequest
): Promise<BranchResult<BusinessBranchResponse>> {
  try {
    const { data, error, response } = await createTenantApiClient(tenantId).PATCH(
      "/api/business/branches/{branchId}/slot-settings",
      { params: { path: { branchId } }, body: request as never }
    );

    if (!response.ok) {
      return rejected(error, response.status);
    }

    return { kind: "success", data: data ?? null };
  } catch {
    return { kind: "failed", message: NETWORK_FAILURE };
  }
}

export async function archiveBranch(
  tenantId: string,
  branchId: string
): Promise<BranchResult<BusinessBranchResponse>> {
  try {
    const { data, error, response } = await createTenantApiClient(tenantId).POST(
      "/api/business/branches/{branchId}/archive",
      { params: { path: { branchId } } }
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
   KURATORLU ZAMAN DILIMI LISTESI (TUZAK 3)
   Kullaniciya serbest metin YAZDIRMA. Backend gecersizi 400 ile reddediyor ama
   en dogrusu hic yanlis girme sansi vermemek. Hepsi gecerli IANA kimlikleri.
   --------------------------------------------------------------------------- */
export const BRANCH_TIME_ZONE_OPTIONS: ReadonlyArray<{ id: string; label: string }> = [
  { id: "Europe/Istanbul", label: "İstanbul (Türkiye) — GMT+3" },
  { id: "Europe/Nicosia", label: "Lefkoşa (Kıbrıs)" },
  { id: "Europe/London", label: "Londra (İngiltere)" },
  { id: "Europe/Berlin", label: "Berlin (Almanya)" },
  { id: "Europe/Amsterdam", label: "Amsterdam (Hollanda)" }
];

export const DEFAULT_BRANCH_TIME_ZONE = "Europe/Istanbul";
