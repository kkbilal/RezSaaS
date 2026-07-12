import { createTenantApiClient } from "@/shared/api/client";
import { parseBranchDateTimeLocalValue } from "@/shared/lib/date-time";
import type { BusinessAppointment } from "./get-business-appointments";

/**
 * RANDEVU OPERASYONLARININ TEK KAYNAGI.
 *
 * Ayni POST'u takvim/panel yuzeyinde ve liste yuzeyinde AYRI AYRI yazmak, ileride birini
 * duzeltip digerini unutmak demektir (or. backend 409 semantigi degisince yalnizca bir
 * ekranin kullaniciya doru mesaj vermesi). Cagri mantigi, dogrulama kurallari ve hata
 * metinleri bu modulde toplanir; ekranlar YALNIZCA sunum yapar.
 *
 * Bu modul TARAYICIDA calisir (createTenantApiClient). Server component'ten import etme.
 */

export type AppointmentOperationKind =
  | "cancel"
  | "complete"
  | "no-show"
  | "note"
  | "rebook"
  | "resource-block";

/** Ekranlardan gelen ham girdi: metin kutulari ve datetime-local degerleri SUBE saatindedir. */
export type AppointmentOperationInput = {
  appointment: BusinessAppointment;
  /** datetime-local degeri, SUBE saat diliminde. Yalnizca rebook / resource-block icin. */
  endLocalValue?: string;
  idempotencyKey: string;
  kind: AppointmentOperationKind;
  /** datetime-local degeri, SUBE saat diliminde. Yalnizca rebook / resource-block icin. */
  startLocalValue?: string;
  tenantId: string | null;
  text: string;
};

/** Dogrulanmis, UTC'ye cevrilmis, gonderilmeye hazir istek. */
export type AppointmentOperationRequest = {
  appointmentId: string;
  endUtc: string | null;
  idempotencyKey: string;
  kind: AppointmentOperationKind;
  resourceId: string | null;
  staffMemberId: string | null;
  startUtc: string | null;
  tenantId: string;
  text: string;
};

export type AppointmentOperationResult =
  /** Backend kabul etti. `status` yeni randevu statusudur (iyimser guncelleme icin). */
  | { kind: "success"; message: string; status?: string | null }
  /**
   * Backend REDDETTI (4xx/5xx). Yerel liste artik bayat olabilir -> cagiran taraf
   * router.refresh() cagirmali.
   */
  | { kind: "rejected"; message: string }
  /** Ag/istisna. Istek sunucuya ulasmamis olabilir; liste bayat DEGIL -> refresh etme. */
  | { kind: "failed"; message: string };

/**
 * Istemci tarafi dogrulama + sube saati -> UTC cevrimi.
 *
 * Cagri YAPILMADAN once calisir; boylece gecersiz girdide spinner bile donmez.
 * Basarisizsa kullaniciya gosterilecek Turkce mesaji doner.
 */
export function prepareAppointmentOperation(
  input: AppointmentOperationInput
):
  | { ok: true; request: AppointmentOperationRequest }
  | { ok: false; message: string } {
  const appointmentId = input.appointment.appointmentId;
  const text = input.text.trim();

  if (!input.tenantId || !appointmentId) {
    return {
      ok: false,
      message: "İşlem için randevu bilgisi doğrulanmalı."
    };
  }

  if (operationNeedsReason(input.kind) && text.length < 3) {
    return {
      ok: false,
      message:
        "İptal, gelmedi ve yeniden planlama gibi kararlar için kısa sebep gerekli."
    };
  }

  const needsTimeRange = operationNeedsTimeRange(input.kind);
  const branchTimeZoneId = input.appointment.branchTimeZoneId;

  // Sube baska sehirde olabilir: girilen saat SUBE saatidir, tarayici saati DEGIL.
  const startUtc = needsTimeRange
    ? parseBranchDateTimeLocalValue(input.startLocalValue ?? "", branchTimeZoneId)
    : null;
  const endUtc = needsTimeRange
    ? parseBranchDateTimeLocalValue(input.endLocalValue ?? "", branchTimeZoneId)
    : null;

  if (needsTimeRange && (!startUtc || !endUtc)) {
    return {
      ok: false,
      message: "Başlangıç ve bitiş şube zamanı geçerli olmalı."
    };
  }

  if (
    startUtc &&
    endUtc &&
    new Date(endUtc).getTime() <= new Date(startUtc).getTime()
  ) {
    return {
      ok: false,
      message: "Bitiş zamanı başlangıçtan sonra olmalı."
    };
  }

  if (input.kind === "resource-block" && !input.appointment.resourceId) {
    return {
      ok: false,
      message: "Kaynak bloklama için iç kaynak bilgisi doğrulanmalı."
    };
  }

  return {
    ok: true,
    request: {
      appointmentId,
      endUtc,
      idempotencyKey: input.idempotencyKey,
      kind: input.kind,
      resourceId: input.appointment.resourceId ?? null,
      staffMemberId: input.appointment.staffMemberId ?? null,
      startUtc,
      tenantId: input.tenantId,
      text
    }
  };
}

/** Dogrulanmis istegi backend'e gonderir. Asla firlatmaz; sonucu normalize eder. */
export async function runAppointmentOperation(
  request: AppointmentOperationRequest
): Promise<AppointmentOperationResult> {
  const client = createTenantApiClient(request.tenantId);
  const { appointmentId, idempotencyKey, text } = request;
  const headerParams = {
    header: {
      "Idempotency-Key": idempotencyKey
    },
    path: {
      appointmentId
    }
  };

  try {
    const result =
      request.kind === "cancel"
        ? await client.POST("/api/business/appointments/{appointmentId}/cancel", {
            body: { reason: text },
            params: headerParams
          })
        : request.kind === "complete"
          ? await client.POST(
              "/api/business/appointments/{appointmentId}/complete",
              {
                // Tamamlanma notu OPSIYONEL: bos metin null'a duser.
                body: { note: text || null },
                params: headerParams
              }
            )
          : request.kind === "no-show"
            ? await client.POST(
                "/api/business/appointments/{appointmentId}/no-show",
                {
                  body: { reason: text },
                  params: headerParams
                }
              )
            : request.kind === "rebook"
              ? await client.POST(
                  "/api/business/appointments/{appointmentId}/rebook",
                  {
                    body: {
                      endUtc: request.endUtc!,
                      reason: text,
                      resourceId: request.resourceId,
                      staffMemberId: request.staffMemberId,
                      startUtc: request.startUtc!
                    },
                    params: headerParams
                  }
                )
              : request.kind === "resource-block"
                ? await client.POST("/api/business/resources/{resourceId}/blocks", {
                    body: {
                      endUtc: request.endUtc!,
                      reason: text,
                      startUtc: request.startUtc!
                    },
                    params: {
                      path: {
                        // prepare() bu alanin dolu oldugunu dogruladi.
                        resourceId: request.resourceId!
                      }
                    }
                  })
                : await client.POST(
                    "/api/business/appointments/{appointmentId}/notes",
                    {
                      body: { note: text || null },
                      params: headerParams
                    }
                  );

    if (!result.response.ok) {
      return {
        kind: "rejected",
        message: getOperationErrorCopy(result.response.status, request.kind)
      };
    }

    return {
      kind: "success",
      message: getOperationSuccessCopy(request.kind),
      status:
        result.data && "status" in result.data ? result.data.status : undefined
    };
  } catch {
    return {
      kind: "failed",
      message: "Randevu işlemi şu anda tamamlanamadı. Lütfen tekrar dene."
    };
  }
}

export function operationNeedsReason(kind: AppointmentOperationKind) {
  return (
    kind === "cancel" ||
    kind === "no-show" ||
    kind === "rebook" ||
    kind === "resource-block"
  );
}

export function operationNeedsTimeRange(kind: AppointmentOperationKind) {
  return kind === "rebook" || kind === "resource-block";
}

/** Yikici (geri alinamaz) operasyonlar: onay adimi ZORUNLU. */
export function operationIsDestructive(kind: AppointmentOperationKind) {
  return kind === "cancel" || kind === "no-show";
}

/**
 * Backend "tamamlandi"yi yalnizca BITIS saatinden sonra kabul eder.
 * Buton acilmadan once ayni kurali burada uygularsak kullanici bosuna 409 yemez.
 */
export function canCompleteAppointment(appointment: BusinessAppointment) {
  if (getAppointmentStatus(appointment) !== "Confirmed" || !appointment.endUtc) {
    return false;
  }

  const end = new Date(appointment.endUtc).getTime();

  return !Number.isNaN(end) && end <= Date.now();
}

/** Backend "gelmedi"yi yalnizca BASLANGIC saatinden sonra kabul eder. */
export function canMarkAppointmentNoShow(appointment: BusinessAppointment) {
  if (getAppointmentStatus(appointment) !== "Confirmed" || !appointment.startUtc) {
    return false;
  }

  const start = new Date(appointment.startUtc).getTime();

  return !Number.isNaN(start) && start <= Date.now();
}

export function getAppointmentStatus(appointment: BusinessAppointment) {
  return appointment.status ?? "Unknown";
}

export function getOperationDetails(kind: AppointmentOperationKind) {
  if (kind === "cancel") {
    return {
      helper:
        "Kısa ve operasyonel bir sebep yaz; telefon, e-posta, gizli bilgi veya fazla kişisel detay ekleme.",
      maxLength: 500,
      placeholder: "Örn. Müşteri talebiyle iptal edildi",
      submitLabel: "Randevuyu iptal et",
      textareaLabel: "İptal sebebi",
      title: "Randevuyu iptal et"
    };
  }

  if (kind === "complete") {
    return {
      helper:
        "Not opsiyoneldir. Randevu yalnızca bitiş saatinden sonra tamamlanabilir.",
      maxLength: 500,
      placeholder: "Opsiyonel tamamlanma notu",
      submitLabel: "Tamamlandı yap",
      textareaLabel: "Tamamlanma notu",
      title: "Randevuyu tamamlandı yap"
    };
  }

  if (kind === "no-show") {
    return {
      helper:
        "Gelmedi kararı slotu erken boşaltmaz; backend bu aksiyonu yalnızca başlangıçtan sonra kabul eder.",
      maxLength: 500,
      placeholder: "Örn. Müşteri randevu saatinde gelmedi",
      submitLabel: "Gelmedi olarak işaretle",
      textareaLabel: "Gelmedi sebebi",
      title: "Müşteri gelmedi"
    };
  }

  if (kind === "rebook") {
    return {
      helper:
        "Yeni zamanı şube saatine göre gir. Frontend UTC'ye çevirir; backend aynı personel ve iç kaynak için çakışmayı tekrar doğrular.",
      maxLength: 500,
      placeholder: "Örn. Müşteri talebiyle yeni saate alındı",
      submitLabel: "Yeniden planla",
      textareaLabel: "Yeniden planlama sebebi",
      title: "Randevuyu yeniden planla"
    };
  }

  if (kind === "resource-block") {
    return {
      helper:
        "Bu işlem seçili iç kaynağı belirtilen şube saati aralığında kullanılamaz yapar. Public slot hesaplama bu bloğu kapasite engeli olarak görür.",
      maxLength: 500,
      placeholder: "Örn. Bakım / arıza / oda kullanılamıyor",
      submitLabel: "Kaynağı blokla",
      textareaLabel: "Blok sebebi",
      title: "İç kaynağı blokla"
    };
  }

  return {
    helper:
      "Bu not yalnızca işletme operasyon yüzeyinde tutulur. Hassas veya gereksiz kişisel bilgi yazma.",
    maxLength: 1000,
    placeholder: "İşletme içi kısa not",
    submitLabel: "Notu kaydet",
    textareaLabel: "İşletme notu",
    title: "Randevu notu"
  };
}

export function getOperationSuccessCopy(kind: AppointmentOperationKind) {
  if (kind === "cancel") {
    return "Randevu iptal edildi; liste güncelleniyor.";
  }

  if (kind === "complete") {
    return "Randevu tamamlandı olarak işaretlendi.";
  }

  if (kind === "no-show") {
    return "Randevu gelmedi olarak işaretlendi.";
  }

  if (kind === "rebook") {
    return "Randevu yeniden planlandı; yeni confirmed kayıt listeye yenilemeyle düşecek.";
  }

  if (kind === "resource-block") {
    return "İç kaynak belirtilen aralıkta bloklandı.";
  }

  return "Randevu notu güncellendi.";
}

export function getOperationErrorCopy(
  status: number,
  kind: AppointmentOperationKind
) {
  if (status === 401) {
    return "Oturum doğrulanamadı; tekrar giriş yapmak gerekebilir.";
  }

  if (status === 403) {
    return "Bu işletme veya şube için randevu işlem yetkin yok.";
  }

  if (status === 404) {
    return "Randevu bulunamadı veya bu hesapla görüntülenemiyor.";
  }

  if (status === 409) {
    if (kind === "complete") {
      return "Randevu bitmeden tamamlandı yapılamaz veya kayıt artık açık değil.";
    }

    if (kind === "no-show") {
      return "Randevu başlamadan gelmedi yapılamaz veya kayıt artık açık değil.";
    }

    if (kind === "rebook") {
      return "Yeni zaman aynı personel veya iç kaynak için çakışıyor olabilir.";
    }

    if (kind === "resource-block") {
      return "Bu iç kaynak için aynı zaman aralığında mevcut bir blok var.";
    }

    return "Bu randevu artık aynı şekilde güncellenemiyor. Liste yenileniyor.";
  }

  return "Randevu işlemi tamamlanamadı. Lütfen tekrar dene.";
}
