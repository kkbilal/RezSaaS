import { createTenantApiClient } from "@/shared/api/client";
import type { ApiSchema } from "@/shared/api/types";

/**
 * PERSONEL + IZIN (musaitsizlik) MUTASYONLARININ TEK KAYNAGI.
 *
 * Ekranlar bu modulu cagirir; ham `createTenantApiClient` component icinde kullanilmaz.
 *
 * IKI TUZAK burada kapatiliyor:
 *  1. Personel SUBE ALTINDA NESTED. list/create/rename/archive ucları `branchId` ISTER.
 *     branchId gonderilmezse backend 403/404 doner. Cagiran taraf once subeyi cozer.
 *  2. Izin (unavailable) ucları personele DOGRUDAN baglidir (branchId'siz):
 *     /api/business/staff/{staffMemberId}/unavailable. Ayni personelin subesi onemli degil.
 *
 * Bu modul TARAYICIDA calisir. Server component'ten import etme.
 */

export type BusinessStaff = ApiSchema<"BusinessStaffResponse">;
export type StaffUnavailable = ApiSchema<"BusinessStaffUnavailableResponse">;

export type StaffResult<TData> =
  /** Backend kabul etti. `data` null olabilir (govde beklenen alanlari tasimiyor). */
  | { kind: "success"; data: TData | null }
  /** Backend REDDETTI (4xx/5xx). Yerel liste bayat olabilir -> cagiran taraf refresh etmeli. */
  | { kind: "rejected"; message: string }
  /** Ag/istisna. Istek sunucuya ulasmamis olabilir -> refresh ETME (yazilani sifirlar). */
  | { kind: "failed"; message: string };

type ErrorBody = { errorCode?: string | null };

const NETWORK_FAILURE = "Bağlantı kurulamadı. İnternetini kontrol edip tekrar dene.";

/** Hata kodunu salon sahibinin diline cevirir. Backend'deki sabitlerle birebir. */
function describeStaffError(errorCode: string | null | undefined, status: number): string {
  switch (errorCode) {
    case "STAFF_INVALID_REQUEST":
      return "Personel adı 2 ile 200 karakter arasında olmalı.";
    case "STAFF_UNAVAILABLE_INVALID_REQUEST":
      // Backend: bitis <= baslangic VEYA sebep > 200 karakter.
      return "Bitiş zamanı başlangıçtan sonra olmalı; sebep en fazla 200 karakter olabilir.";
    case "STAFF_UNAVAILABLE_OVERLAP":
      return "Bu tarih aralığı bu personelin mevcut bir izniyle çakışıyor.";
    case "STAFF_HAS_UPCOMING_APPOINTMENTS":
      // Backend, gelecekte aktif randevusu olan personelin arsivlenmesini engelliyor
      // (aksi halde o randevular sahipsiz kalirdi). Salona ne yapmasi gerektigini soyle.
      return "Bu personelin ileri tarihli randevuları var. Önce o randevuları iptal et veya başka bir personele taşı, sonra arşivle.";
    case "STAFF_NOT_FOUND":
    case "STAFF_UNAVAILABLE_NOT_FOUND":
      return "Kayıt bulunamadı. Sayfayı yenileyip tekrar dene.";
    case "BRANCH_NOT_FOUND":
      return "Şube bulunamadı. Sayfayı yenileyip tekrar dene.";
    default:
      if (status === 403) return "Bu işlem için yetkin yok.";
      if (status === 401) return "Oturumun düşmüş görünüyor. Tekrar giriş yap.";
      return "İşlem tamamlanamadı. Tekrar dene.";
  }
}

function rejected(error: unknown, status: number): StaffResult<never> {
  const errorCode =
    error && typeof error === "object" && "errorCode" in error
      ? (error as ErrorBody).errorCode
      : null;

  return { kind: "rejected", message: describeStaffError(errorCode, status) };
}

/* ---------------------------------------------------------------------------
   PERSONEL -- hepsi sube altinda nested (branchId zorunlu)
   --------------------------------------------------------------------------- */

export async function listStaff(
  tenantId: string,
  branchId: string
): Promise<StaffResult<BusinessStaff[]>> {
  try {
    const { data, error, response } = await createTenantApiClient(tenantId).GET(
      "/api/business/branches/{branchId}/staff",
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

export async function createStaff(
  tenantId: string,
  branchId: string,
  displayName: string
): Promise<StaffResult<BusinessStaff>> {
  try {
    // userAccountId GONDERILMEZ: personel bir TAKVIM kaydidir, login'i olan kullanici degil.
    const { data, error, response } = await createTenantApiClient(tenantId).POST(
      "/api/business/branches/{branchId}/staff",
      { body: { displayName }, params: { path: { branchId } } }
    );

    if (!response.ok) {
      return rejected(error, response.status);
    }

    return { kind: "success", data: data ?? null };
  } catch {
    return { kind: "failed", message: NETWORK_FAILURE };
  }
}

export async function renameStaff(
  tenantId: string,
  branchId: string,
  staffId: string,
  displayName: string
): Promise<StaffResult<BusinessStaff>> {
  try {
    const { data, error, response } = await createTenantApiClient(tenantId).PATCH(
      "/api/business/branches/{branchId}/staff/{staffId}",
      { body: { displayName }, params: { path: { branchId, staffId } } }
    );

    if (!response.ok) {
      return rejected(error, response.status);
    }

    return { kind: "success", data: data ?? null };
  } catch {
    return { kind: "failed", message: NETWORK_FAILURE };
  }
}

export async function archiveStaff(
  tenantId: string,
  branchId: string,
  staffId: string
): Promise<StaffResult<BusinessStaff>> {
  try {
    const { data, error, response } = await createTenantApiClient(tenantId).POST(
      "/api/business/branches/{branchId}/staff/{staffId}/archive",
      { params: { path: { branchId, staffId } } }
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
   IZIN / MUSAIT DEGIL -- personele dogrudan bagli (branchId'siz)
   --------------------------------------------------------------------------- */

export async function listUnavailable(
  tenantId: string,
  staffMemberId: string
): Promise<StaffResult<StaffUnavailable[]>> {
  try {
    const { data, error, response } = await createTenantApiClient(tenantId).GET(
      "/api/business/staff/{staffMemberId}/unavailable",
      { params: { path: { staffMemberId } } }
    );

    if (!response.ok) {
      return rejected(error, response.status);
    }

    return { kind: "success", data: data ?? [] };
  } catch {
    return { kind: "failed", message: NETWORK_FAILURE };
  }
}

export async function createUnavailable(
  tenantId: string,
  staffMemberId: string,
  input: { startUtc: string; endUtc: string; reason: string }
): Promise<StaffResult<StaffUnavailable>> {
  try {
    const { data, error, response } = await createTenantApiClient(tenantId).POST(
      "/api/business/staff/{staffMemberId}/unavailable",
      {
        body: {
          startUtc: input.startUtc,
          endUtc: input.endUtc,
          reason: input.reason
        },
        params: { path: { staffMemberId } }
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

export async function deleteUnavailable(
  tenantId: string,
  staffMemberId: string,
  unavailableId: string
): Promise<StaffResult<null>> {
  try {
    const { error, response } = await createTenantApiClient(tenantId).DELETE(
      "/api/business/staff/{staffMemberId}/unavailable/{unavailableId}",
      { params: { path: { staffMemberId, unavailableId } } }
    );

    if (!response.ok) {
      return rejected(error, response.status);
    }

    return { kind: "success", data: null };
  } catch {
    return { kind: "failed", message: NETWORK_FAILURE };
  }
}

/* ---------------------------------------------------------------------------
   GORUNUM YARDIMCILARI
   --------------------------------------------------------------------------- */

export type StaffStatusView = {
  label: string;
  tone: "active" | "inactive";
  isArchived: boolean;
};

/** status backend enum'unun ToString'i: "Active" | "Suspended" | "Archived". */
export function describeStaffStatus(status: string | null | undefined): StaffStatusView {
  switch (status) {
    case "Active":
      return { label: "Aktif", tone: "active", isArchived: false };
    case "Suspended":
      return { label: "Askıda", tone: "inactive", isArchived: false };
    case "Archived":
      return { label: "Arşivli", tone: "inactive", isArchived: true };
    default:
      // Bilinmeyen bir statuyu "Aktif" gibi gostermeyiz; oldugu gibi ama pasif tonla.
      return { label: status ?? "Bilinmiyor", tone: "inactive", isArchived: false };
  }
}
