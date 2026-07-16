import type { BusinessAppointment } from "@/features/business/api/get-business-appointments";
import {
  formatBranchDateLabel,
  formatBranchTimeLabel
} from "@/shared/lib/date-time";

/**
 * Randevu SUNUM yardimcilari -- TEK KAYNAK.
 *
 * Hem randevu LISTESI (/panel/randevular) hem TAKVIM (/panel/takvim) ayni randevu
 * ozetlerini gosterir. Ozet metnini iki ekranda ayri yazmak, birini duzeltip digerini
 * bayat birakmak demektir; bu yuzden formatlama burada toplanir. Saf fonksiyonlar --
 * client/server ayrimindan bagimsiz, test edilebilir.
 */

/** "Sac kesimi + 1 hizmet" gibi kisa hizmet ozeti. */
export function getServiceSummary(appointment: BusinessAppointment): string {
  const lines = appointment.lines ?? [];
  const firstLine = lines.at(0);
  const firstService = firstLine?.serviceNameSnapshot ?? "Hizmet detayı yok";

  if (lines.length <= 1) {
    return firstService;
  }

  return `${firstService} + ${lines.length - 1} hizmet`;
}

/** Toplam sure (dk). Satir sureleri yoksa baslangic/bitisten hesaplar. */
export function getDurationMinutes(appointment: BusinessAppointment): number {
  const lineDuration = (appointment.lines ?? []).reduce(
    (totalMinutes, line) => totalMinutes + (line.durationMinutes ?? 0),
    0
  );

  if (lineDuration > 0) {
    return lineDuration;
  }

  if (!appointment.startUtc || !appointment.endUtc) {
    return 0;
  }

  const start = new Date(appointment.startUtc).getTime();
  const end = new Date(appointment.endUtc).getTime();

  if (Number.isNaN(start) || Number.isNaN(end) || end <= start) {
    return 0;
  }

  return Math.round((end - start) / 60000);
}

/** "12 Temmuz Cumartesi · 14:30 - 15:15" -- her zaman SUBE saat diliminde. */
export function formatWindow(appointment: BusinessAppointment): string {
  const { branchTimeZoneId, endUtc, startUtc } = appointment;

  if (!startUtc) {
    return "Zaman bilgisi yok";
  }

  if (!branchTimeZoneId) {
    // Saat dilimi yoksa UYDURMAYIZ: ham UTC yazip bunu acikca soyleriz.
    return endUtc ? `${startUtc} - ${endUtc} (UTC)` : `${startUtc} (UTC)`;
  }

  const day = formatBranchDateLabel(startUtc, branchTimeZoneId);
  const start = formatBranchTimeLabel(startUtc, branchTimeZoneId);

  if (!endUtc) {
    return `${day} · ${start}`;
  }

  return `${day} · ${start} - ${formatBranchTimeLabel(endUtc, branchTimeZoneId)}`;
}
