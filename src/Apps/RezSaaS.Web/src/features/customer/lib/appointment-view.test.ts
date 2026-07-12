import assert from "node:assert/strict";
import test from "node:test";

import {
  getCancelErrorMessage,
  getCancelKind,
  getStatusPresentation,
  partitionAppointments,
  type AppointmentViewItem
} from "./appointment-view.ts";

const now = "2026-07-12T12:00:00.000Z";

function item(overrides: Partial<AppointmentViewItem> = {}): AppointmentViewItem {
  return {
    appointmentId: "a-1",
    businessSlug: "guzellik-merkezi",
    endUtc: "2026-07-13T11:00:00.000Z",
    itemType: "Appointment",
    startUtc: "2026-07-13T10:00:00.000Z",
    status: "Confirmed",
    ...overrides
  };
}

/* ===========================================================================
   SEKME AYRIMI: Yaklasan = AKTIF STATU **VE** ZAMANI GELMEMIS
   =========================================================================== */

test("aktif ve gelecekteki kayitlar Yaklasan'a dusser", () => {
  const { upcoming, past } = partitionAppointments(
    [
      item({ status: "Confirmed" }),
      item({ appointmentId: "a-2", itemType: "AppointmentRequest", status: "PendingApproval" })
    ],
    now
  );

  assert.equal(upcoming.length, 2);
  assert.equal(past.length, 0);
});

test("zamani GECMIS ama hala Confirmed olan randevu Yaklasan'da ASILI KALMAZ", () => {
  // Salon randevuyu 'Completed' yapmayi unutursa, dunku randevu musterinin
  // "Yaklasan" sekmesinde durmaya devam ederdi. Zaman ekseni de kontrol edilir.
  const { upcoming, past } = partitionAppointments(
    [
      item({
        endUtc: "2026-07-11T11:00:00.000Z",
        startUtc: "2026-07-11T10:00:00.000Z",
        status: "Confirmed"
      })
    ],
    now
  );

  assert.equal(upcoming.length, 0);
  assert.equal(past.length, 1);
});

test("kapanmis statuler gelecekte olsalar bile Gecmis'e dusser", () => {
  const { upcoming, past } = partitionAppointments(
    [item({ status: "Cancelled" }), item({ appointmentId: "a-2", status: "Declined" })],
    now
  );

  assert.equal(upcoming.length, 0);
  assert.equal(past.length, 2);
});

test("Yaklasan en yakin tarih ustte, Gecmis en yeni ustte siralanir", () => {
  const { upcoming, past } = partitionAppointments(
    [
      item({ appointmentId: "gec", startUtc: "2026-07-20T10:00:00.000Z", endUtc: "2026-07-20T11:00:00.000Z" }),
      item({ appointmentId: "yakin", startUtc: "2026-07-13T10:00:00.000Z", endUtc: "2026-07-13T11:00:00.000Z" }),
      item({ appointmentId: "eski", startUtc: "2026-07-01T10:00:00.000Z", endUtc: "2026-07-01T11:00:00.000Z", status: "Completed" }),
      item({ appointmentId: "yeni-gecmis", startUtc: "2026-07-10T10:00:00.000Z", endUtc: "2026-07-10T11:00:00.000Z", status: "Completed" })
    ],
    now
  );

  assert.deepEqual(upcoming.map((entry) => entry.appointmentId), ["yakin", "gec"]);
  assert.deepEqual(past.map((entry) => entry.appointmentId), ["yeni-gecmis", "eski"]);
});

/* ===========================================================================
   IPTAL EDILEBILIRLIK: iki ayri uc, iki ayri kosul
   =========================================================================== */

test("bekleyen TALEP talep-iptal ucuna, onaylanmis RANDEVU randevu-iptal ucuna gider", () => {
  assert.equal(
    getCancelKind(
      item({
        appointmentId: null,
        appointmentRequestId: "r-1",
        itemType: "AppointmentRequest",
        status: "PendingApproval"
      })
    ),
    "request"
  );

  assert.equal(getCancelKind(item({ itemType: "Appointment", status: "Confirmed" })), "appointment");
});

test("kapanmis kayitlarda iptal aksiyonu YOKTUR", () => {
  assert.equal(getCancelKind(item({ status: "Completed" })), null);
  assert.equal(getCancelKind(item({ status: "Cancelled" })), null);
  assert.equal(getCancelKind(item({ itemType: "AppointmentRequest", status: "Declined" })), null);
});

test("slug yoksa iptal aksiyonu YOKTUR (uc slug ister; ureteceğimiz URL kirik olurdu)", () => {
  assert.equal(getCancelKind(item({ businessSlug: null })), null);
});

/* ===========================================================================
   STATU ROZETI: renk TEK sinyal degil -- her statu METIN tasir
   =========================================================================== */

test("her statu musterinin dilinde GORUNUR bir metin tasir", () => {
  assert.equal(getStatusPresentation("PendingApproval").label, "Onay bekliyor");
  assert.equal(getStatusPresentation("Confirmed").label, "Onaylandı");
  assert.equal(getStatusPresentation("Approved").label, "Onaylandı");
  assert.equal(getStatusPresentation("Completed").label, "Tamamlandı");
  assert.equal(getStatusPresentation("Cancelled").label, "İptal edildi");
  assert.equal(getStatusPresentation("CancelledByCustomer").label, "İptal edildi");
  assert.equal(getStatusPresentation("Declined").label, "Salon kabul etmedi");
  assert.equal(getStatusPresentation("Expired").label, "Süresi doldu");
  assert.equal(getStatusPresentation("Superseded").label, "Salon başka bir randevu aldı");
  assert.equal(getStatusPresentation("NoShow").label, "Gelinmedi");
  assert.equal(getStatusPresentation("Rebooked").label, "Yeniden planlandı");
});

test("taninmayan statu ekrani cokertmez, uydurma da yapmaz", () => {
  assert.equal(getStatusPresentation("SomethingNew").label, "SomethingNew");
  assert.equal(getStatusPresentation(null).label, "Durum bilinmiyor");
});

/* ===========================================================================
   IPTAL HATALARI: 'too late' mesaji SAYIYI tasimali
   =========================================================================== */

test("APPOINTMENT_CANCEL_TOO_LATE mesaji backend'den gelen saat esigini YAZAR", () => {
  const message = getCancelErrorMessage(409, {
    cancellationCutoffHours: 2,
    errorCode: "APPOINTMENT_CANCEL_TOO_LATE"
  });

  assert.ok(message.includes("2 saatten az"), message);
  assert.ok(message.includes("salonu arayın"), message);
});

test("esik gelmezse bile mesaj hala anlasilir (sayi UYDURULMAZ)", () => {
  const message = getCancelErrorMessage(409, {
    cancellationCutoffHours: null,
    errorCode: "APPOINTMENT_CANCEL_TOO_LATE"
  });

  assert.ok(!message.includes("null"), message);
  assert.ok(message.includes("salonu arayın"), message);
});

test("diger hata kodlari kendi metnini alir", () => {
  assert.match(
    getCancelErrorMessage(409, { errorCode: "APPOINTMENT_ALREADY_CLOSED" }),
    /zaten kapanmış/
  );
  assert.match(
    getCancelErrorMessage(409, { errorCode: "IDEMPOTENCY_KEY_REUSED" }),
    /zaten işleniyor/
  );
  assert.match(getCancelErrorMessage(401, null), /giriş/);
});
