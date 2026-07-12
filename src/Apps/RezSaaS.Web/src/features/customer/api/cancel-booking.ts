import { apiClient } from "@/shared/api/client";
import {
  getCancelErrorMessage,
  type CancelErrorBody
} from "@/features/customer/lib/appointment-view";

/**
 * MUSTERI IPTALI -- iki ayri backend ucu, TEK cagiran yuzey.
 *
 * Musteri icin "talebi iptal et" ile "randevuyu iptal et" ayni jesttir; ayrimi
 * ekranin yapmasi gereksiz. Ayrim burada, uc secilirken yapilir.
 *
 * Ikisi de IDEMPOTENT: ayni Idempotency-Key ile tekrarlanan cagri yeni bir iptal
 * uretmez. Anahtar cagiran tarafta (intent basina) uretilir, burada degil -- yoksa
 * her retry yeni anahtar alir ve idempotency'nin anlami kalmazdi.
 */

export type CancelResult =
  | { kind: "ok"; status: string }
  | { kind: "error"; message: string };

export async function cancelAppointmentRequest(
  businessSlug: string,
  appointmentRequestId: string,
  idempotencyKey: string
): Promise<CancelResult> {
  try {
    const { data, error, response } = await apiClient.POST(
      "/api/public/businesses/{slug}/appointment-requests/{appointmentRequestId}/cancel",
      {
        params: {
          header: { "Idempotency-Key": idempotencyKey },
          path: { appointmentRequestId, slug: businessSlug }
        }
      }
    );

    if (!response.ok) {
      return {
        kind: "error",
        message: getCancelErrorMessage(response.status, error as CancelErrorBody)
      };
    }

    return { kind: "ok", status: data?.status ?? "CancelledByCustomer" };
  } catch {
    return {
      kind: "error",
      message: "Bağlantı kurulamadı. Lütfen tekrar deneyin."
    };
  }
}

export async function cancelAppointment(
  businessSlug: string,
  appointmentId: string,
  idempotencyKey: string
): Promise<CancelResult> {
  try {
    const { data, error, response } = await apiClient.POST(
      "/api/public/businesses/{slug}/appointments/{appointmentId}/cancel",
      {
        params: {
          header: { "Idempotency-Key": idempotencyKey },
          path: { appointmentId, slug: businessSlug }
        }
      }
    );

    if (!response.ok) {
      // Iptal politikasi ihlali (409 APPOINTMENT_CANCEL_TOO_LATE) burada, govdedeki
      // cancellationCutoffHours ile birlikte, okunabilir bir cumleye cevrilir.
      return {
        kind: "error",
        message: getCancelErrorMessage(response.status, error as CancelErrorBody)
      };
    }

    return { kind: "ok", status: data?.status ?? "Cancelled" };
  } catch {
    return {
      kind: "error",
      message: "Bağlantı kurulamadı. Lütfen tekrar deneyin."
    };
  }
}
