/**
 * MUSTERI RANDEVU GORUNUMU -- saf sunum mantigi.
 *
 * ZIHINSEL MODEL: Musterinin kafasinda "talep" diye bir nesne YOKTUR.
 * "Randevu aldim, onay bekliyorum" vardir. Bu yuzden backend'in ItemType ayrimi
 * (AppointmentRequest | Appointment) burada bir SEKME degil, bir ROZET'e cevrilir.
 * Sekmeler zamana gore ayrisir: Yaklasan | Gecmis.
 *
 * Bu modul BILEREK next/* ve @/shared/api'den bagimsizdir: `node --test` altinda
 * dogrudan calisabilsin diye. (get-appointment-history.ts next/headers cekiyor.)
 * Bu yuzden asagidaki tipler YAPISAL (structural) minimum sekillerdir; uretimdeki
 * CustomerAppointmentHistoryItem bunlara atanabilir.
 */

export type AppointmentViewLine = {
  serviceNameSnapshot?: string | null;
  durationMinutes?: number;
  priceAmount?: number;
  currencyCode?: string | null;
};

export type AppointmentViewItem = {
  itemType?: string | null;
  appointmentId?: string | null;
  appointmentRequestId?: string | null;
  businessSlug?: string | null;
  businessDisplayName?: string | null;
  branchDisplayName?: string | null;
  branchTimeZoneId?: string | null;
  staffMemberDisplayName?: string | null;
  startUtc?: string;
  endUtc?: string;
  expiresAtUtc?: string | null;
  status?: string | null;
  lines?: AppointmentViewLine[] | null;
};

export type StatusTone = "pending" | "confirmed" | "neutral" | "negative";

export type StatusPresentation = {
  /** GORUNUR metin. Renk TEK sinyal olamaz -- rozet her zaman bunu yazar. */
  label: string;
  tone: StatusTone;
};

/**
 * Musterinin okuyacagi statu metinleri. "Superseded"/"Declined" gibi sistem
 * kelimeleri musteriye ASLA sizmaz; salonun ne yaptigi insan diliyle yazilir.
 */
const statusPresentations: Record<string, StatusPresentation> = {
  Approved: { label: "Onaylandı", tone: "confirmed" },
  Cancelled: { label: "İptal edildi", tone: "neutral" },
  CancelledByAppeal: { label: "İptal edildi", tone: "neutral" },
  CancelledByCustomer: { label: "İptal edildi", tone: "neutral" },
  Completed: { label: "Tamamlandı", tone: "neutral" },
  Confirmed: { label: "Onaylandı", tone: "confirmed" },
  Declined: { label: "Salon kabul etmedi", tone: "negative" },
  Expired: { label: "Süresi doldu", tone: "negative" },
  NoShow: { label: "Gelinmedi", tone: "negative" },
  PendingApproval: { label: "Onay bekliyor", tone: "pending" },
  Rebooked: { label: "Yeniden planlandı", tone: "neutral" },
  Superseded: { label: "Salon başka bir randevu aldı", tone: "neutral" }
};

/** Taninmayan statu icin UYDURMA yapmayiz; ham degeri gosterip notr davraniriz. */
export function getStatusPresentation(
  status: string | null | undefined
): StatusPresentation {
  if (!status) {
    return { label: "Durum bilinmiyor", tone: "neutral" };
  }

  return statusPresentations[status] ?? { label: status, tone: "neutral" };
}

/** Hala "yasayan" kayitlar: musteri bunlar icin bir sey bekliyor ya da gelecek. */
const activeStatuses = new Set(["PendingApproval", "Approved", "Confirmed"]);

export function isActiveStatus(status: string | null | undefined): boolean {
  return !!status && activeStatuses.has(status);
}

export function isPendingRequest(item: AppointmentViewItem): boolean {
  return (
    item.itemType === "AppointmentRequest" && item.status === "PendingApproval"
  );
}

export function isConfirmedAppointment(item: AppointmentViewItem): boolean {
  return item.itemType === "Appointment" && item.status === "Confirmed";
}

/**
 * Iptal edilebilirlik -- SADECE butonu gosterip gostermemek icin.
 *
 * DOGRULUK KAYNAGI BACKEND'DIR: iptal politikasi penceresi (CancellationCutoffHours)
 * burada HESAPLANMAZ. Butonu onceden disable etmeye calismak, saat kaymasi yuzunden
 * musteriye "iptal edemezsin" yalanı soyleyebilir. Butona basilir, backend karar verir,
 * 409 gelirse SEBEP GORUNUR sekilde yazilir.
 */
export function getCancelKind(
  item: AppointmentViewItem
): "request" | "appointment" | null {
  if (isPendingRequest(item) && item.appointmentRequestId && item.businessSlug) {
    return "request";
  }

  if (isConfirmedAppointment(item) && item.appointmentId && item.businessSlug) {
    return "appointment";
  }

  return null;
}

export type AppointmentTab = "upcoming" | "past";

export type PartitionedAppointments = {
  upcoming: AppointmentViewItem[];
  past: AppointmentViewItem[];
};

/**
 * SEKME AYRIMI -- client-side.
 *
 * Backend'in ?status filtresi artik dogru calisiyor, ama yine de TEK cagriyla tum
 * gecmisi cekip burada ayiriyoruz. Sebep: "Yaklasan" iki eksenin BILESIMI
 * (statu AKTIF **ve** bitis zamani GELECEKTE) -- tek bir status parametresi bunu ifade
 * edemez. Ayrica sekme degistirmek ag istegi yapmaz, telefonda aninda cevap verir.
 *
 * Yaklasan  = aktif statu (Onay bekliyor / Onaylandi) VE bitisi henuz gecmemis
 * Gecmis    = kapanmis statuler + zamani gecmis ama salonun hala kapatmadigi kayitlar
 *             (or. dun Confirmed kalmis bir randevu "Yaklasan"da asili kalmaz)
 */
export function partitionAppointments(
  items: readonly AppointmentViewItem[],
  nowUtc: string
): PartitionedAppointments {
  const now = Date.parse(nowUtc);
  const upcoming: AppointmentViewItem[] = [];
  const past: AppointmentViewItem[] = [];

  for (const item of items) {
    const end = item.endUtc ? Date.parse(item.endUtc) : Number.NaN;
    const stillAhead = !Number.isNaN(end) && !Number.isNaN(now) && end > now;

    if (isActiveStatus(item.status) && stillAhead) {
      upcoming.push(item);
    } else {
      past.push(item);
    }
  }

  // Yaklasan: en yakin tarih ustte (musteri "sirada ne var" diye bakar).
  upcoming.sort((left, right) => startTime(left) - startTime(right));
  // Gecmis: en son olan ustte.
  past.sort((left, right) => startTime(right) - startTime(left));

  return { past, upcoming };
}

function startTime(item: AppointmentViewItem): number {
  const value = item.startUtc ? Date.parse(item.startUtc) : Number.NaN;
  return Number.isNaN(value) ? 0 : value;
}

export function getItemKey(item: AppointmentViewItem): string | null {
  return item.appointmentId ?? item.appointmentRequestId ?? null;
}

export function getServiceSummary(item: AppointmentViewItem): string {
  const lines = item.lines ?? [];
  const first = lines.at(0)?.serviceNameSnapshot ?? "Hizmet detayı yok";

  if (lines.length <= 1) {
    return first;
  }

  return `${first} + ${lines.length - 1} hizmet`;
}

export function getTotalDurationMinutes(item: AppointmentViewItem): number {
  return (item.lines ?? []).reduce(
    (total, line) => total + (line.durationMinutes ?? 0),
    0
  );
}

export function formatTotalPrice(item: AppointmentViewItem): string {
  const lines = item.lines ?? [];
  const amount = lines.reduce((total, line) => total + (line.priceAmount ?? 0), 0);
  const currencyCode =
    lines.find((line) => line.currencyCode)?.currencyCode ?? "TRY";

  try {
    return new Intl.NumberFormat("tr-TR", {
      currency: currencyCode,
      maximumFractionDigits: 0,
      style: "currency"
    }).format(amount);
  } catch {
    // Gecersiz para birimi kodu Intl'i patlatir; ekran cokmesin.
    return `${amount} ${currencyCode}`;
  }
}

/* ---------------------------------------------------------------------------
   IPTAL HATA METINLERI
   --------------------------------------------------------------------------- */

export type CancelErrorBody = {
  errorCode?: string | null;
  cancellationCutoffHours?: number | null;
};

/**
 * Backend'in errorCode'unu musterinin ANLAYACAGI cumleye cevirir.
 *
 * APPOINTMENT_CANCEL_TOO_LATE ozel: yanittaki cancellationCutoffHours'u cumleye GOMERIZ.
 * "Iptal edilemiyor" demek yetmez -- musteri NEDEN olmadigini ve ne yapacagini bilmeli.
 */
export function getCancelErrorMessage(
  httpStatus: number,
  body: CancelErrorBody | null | undefined
): string {
  const errorCode = body?.errorCode ?? null;

  if (errorCode === "APPOINTMENT_CANCEL_TOO_LATE") {
    const hours = body?.cancellationCutoffHours;

    if (typeof hours === "number" && hours > 0) {
      return `Randevu saatine ${hours} saatten az kaldığı için iptal edilemiyor. Lütfen salonu arayın.`;
    }

    return "Randevu saati çok yaklaştığı için iptal edilemiyor. Lütfen salonu arayın.";
  }

  if (errorCode === "APPOINTMENT_ALREADY_CLOSED") {
    return "Bu randevu zaten kapanmış. Listeyi yenileyin.";
  }

  if (errorCode === "APPOINTMENT_NOT_FOUND") {
    return "Randevu bulunamadı. Listeyi yenileyin.";
  }

  if (errorCode === "IDEMPOTENCY_KEY_REUSED") {
    return "Bu iptal isteği zaten işleniyor. Lütfen birkaç saniye sonra tekrar deneyin.";
  }

  if (httpStatus === 401) {
    return "Oturumunuz doğrulanamadı. Lütfen yeniden giriş yapın.";
  }

  if (httpStatus === 404) {
    return "Randevu bulunamadı veya bu hesapla görüntülenemiyor.";
  }

  if (httpStatus === 409) {
    return "Bu randevu artık iptal edilebilir durumda değil.";
  }

  return "İptal edilemedi. Lütfen tekrar deneyin.";
}
