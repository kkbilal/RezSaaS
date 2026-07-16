import { createTenantApiClient } from "@/shared/api/client";
import type { ApiSchema } from "@/shared/api/types";

/**
 * CALISMA SAATLERI MUTASYONLARININ TEK KAYNAGI.
 *
 * TUZAK'lar (koddan/sozlesmeden dogrulandi):
 *  1. Calisma saatleri SUBE seviyesinde (personel bazli DEGIL). Tum ucler branchId ister.
 *  2. dayOfWeek path parametresi backend'de `Enum.TryParse<DayOfWeek>` ile cozulur
 *     -> .NET DayOfWeek ENUM ADLARI ("Monday", "Sunday" ...). Sunday=0 ama backend
 *     adi parse ettigi icin numeric degerle ugrasmaya gerek yok; adlari gonderiyoruz.
 *     GET yaniti da `dayOfWeek.ToString()` = ayni adlari doner -> birebir eslesir.
 *  3. opensAt/closesAt backend'de TimeOnly. Yanit "HH:mm" olarak gelir; upsert
 *     `TimeOnly.TryParse` ile "HH:mm"'i kabul eder. Bu yuzden her yerde "HH:mm".
 *  4. Kapali DEGILSE backend closesAt > opensAt SART kosar (WORKING_HOURS_INVALID_REQUEST).
 *     Ayni kontrolu client'ta da yapip gereksiz istek atmiyoruz.
 *
 * Bu modul TARAYICIDA calisir. Server component'ten import etme.
 */

export type WorkingHours = ApiSchema<"BusinessWorkingHoursResponse">;

export type WorkingHoursInput = {
  /** "HH:mm" -- backend TimeOnly.TryParse ile kabul eder. */
  opensAt: string;
  closesAt: string;
  isClosed: boolean;
};

export type WorkingHoursResult<TData> =
  /** Backend kabul etti. `data` null olabilir (govde beklenen alanlari tasimiyor). */
  | { kind: "success"; data: TData | null }
  /** Backend REDDETTI (4xx/5xx). Yerel durum bayat olabilir -> cagiran taraf refresh etmeli. */
  | { kind: "rejected"; message: string }
  /** Ag/istisna. Istek sunucuya ulasmamis olabilir -> refresh ETME (yazilani sifirlar). */
  | { kind: "failed"; message: string };

type ErrorBody = { errorCode?: string | null };

const NETWORK_FAILURE = "Bağlantı kurulamadı. İnternetini kontrol edip tekrar dene.";

/**
 * HAFTANIN GUNLERI -- Pazartesi'den baslar (TR takvim alışkanlığı), Pazar sonda.
 * `key` degerleri backend'in bekledigi .NET DayOfWeek enum ADLARIDIR; DEGISTIRME.
 */
export const WEEK_DAYS = [
  { key: "Monday", label: "Pazartesi" },
  { key: "Tuesday", label: "Salı" },
  { key: "Wednesday", label: "Çarşamba" },
  { key: "Thursday", label: "Perşembe" },
  { key: "Friday", label: "Cuma" },
  { key: "Saturday", label: "Cumartesi" },
  { key: "Sunday", label: "Pazar" }
] as const;

export type DayKey = (typeof WEEK_DAYS)[number]["key"];

/** Hata kodunu salon sahibinin diline cevirir. Backend'deki sabitlerle birebir. */
function describeError(errorCode: string | null | undefined, status: number): string {
  switch (errorCode) {
    case "WORKING_HOURS_INVALID_REQUEST":
      // Backend: kapali degilken kapanis <= acilis.
      return "Kapanış saati açılış saatinden sonra olmalı.";
    case "INVALID_TIME_FORMAT":
      return "Saat biçimi geçersiz. Açılış ve kapanış saatini gir.";
    case "INVALID_DAY_OF_WEEK":
      return "Geçersiz gün. Sayfayı yenileyip tekrar dene.";
    case "BRANCH_NOT_FOUND":
      return "Şube bulunamadı. Sayfayı yenileyip tekrar dene.";
    case "WORKING_HOURS_NOT_FOUND":
      return "Sıfırlanacak çalışma saati bulunamadı.";
    default:
      if (status === 403) return "Bu işlem için yetkin yok.";
      if (status === 401) return "Oturumun düşmüş görünüyor. Tekrar giriş yap.";
      return "İşlem tamamlanamadı. Tekrar dene.";
  }
}

function rejected(error: unknown, status: number): WorkingHoursResult<never> {
  const errorCode =
    error && typeof error === "object" && "errorCode" in error
      ? (error as ErrorBody).errorCode
      : null;

  return { kind: "rejected", message: describeError(errorCode, status) };
}

export async function listWorkingHours(
  tenantId: string,
  branchId: string
): Promise<WorkingHoursResult<WorkingHours[]>> {
  try {
    const { data, error, response } = await createTenantApiClient(tenantId).GET(
      "/api/business/branches/{branchId}/working-hours",
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

export async function upsertWorkingHours(
  tenantId: string,
  branchId: string,
  dayOfWeek: DayKey,
  input: WorkingHoursInput
): Promise<WorkingHoursResult<WorkingHours>> {
  try {
    const { data, error, response } = await createTenantApiClient(tenantId).PUT(
      "/api/business/branches/{branchId}/working-hours/{dayOfWeek}",
      {
        body: {
          opensAt: input.opensAt,
          closesAt: input.closesAt,
          isClosed: input.isClosed
        },
        params: { path: { branchId, dayOfWeek } }
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
