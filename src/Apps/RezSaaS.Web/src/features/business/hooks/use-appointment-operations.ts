"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { toast } from "sonner";
import {
  prepareAppointmentOperation,
  runAppointmentOperation,
  type AppointmentOperationKind
} from "@/features/business/api/business-appointment-operations";
import type { BusinessAppointment } from "@/features/business/api/get-business-appointments";
import {
  createOperationDraft,
  type OperationDraft
} from "@/features/business/components/appointment-operation-surface";

/**
 * Randevu operasyon DURUM MAKINESI -- liste ve takvim ekranlari ortak kullanir.
 *
 * Taslak actma, backend'e gonderme, iyimser statu guncellemesi ve router.refresh()
 * mantigi TEK yerde toplanir; ekranlar YALNIZCA sunum yapar. Ayni akisi iki ekranda
 * ayri yazmak, birinde 409 sonrasi refresh'i unutup bayat liste gostermek demektir.
 */
export function useAppointmentOperations(tenantId: string | null) {
  const router = useRouter();
  const [draft, setDraft] = useState<OperationDraft | null>(null);
  // Backend yanitini bekleyip sayfa yenilenene kadar satirin bayat gorunmemesi icin.
  const [statusOverrides, setStatusOverrides] = useState<Record<string, string>>(
    {}
  );
  const [actingAppointmentId, setActingAppointmentId] = useState<string | null>(
    null
  );
  // Her basarili operasyondan sonra artar: takvim gibi kendi verisini tutan ekranlar
  // bunu izleyip gorunen gunu YENIDEN cekebilir (yeniden planlama YENI kayit yaratir,
  // statu override'i bunu gostermez).
  const [operationRevision, setOperationRevision] = useState(0);

  function openOperation(
    appointment: BusinessAppointment,
    kind: AppointmentOperationKind
  ) {
    if (!tenantId || !appointment.appointmentId) {
      toast.error("İşlem için yetkili işletme ve randevu bilgisi doğrulanmalı.");
      return;
    }

    setDraft(createOperationDraft(appointment, kind));
  }

  function closeDraft() {
    setDraft(null);
  }

  function updateDraft(patch: Partial<OperationDraft>) {
    setDraft((current) => (current ? { ...current, ...patch } : current));
  }

  async function submitOperation() {
    if (!draft) {
      return;
    }

    const prepared = prepareAppointmentOperation({
      appointment: draft.appointment,
      endLocalValue: draft.endLocalValue,
      idempotencyKey: draft.idempotencyKey,
      kind: draft.kind,
      startLocalValue: draft.startLocalValue,
      tenantId,
      text: draft.text
    });

    if (!prepared.ok) {
      toast.error(prepared.message);
      return;
    }

    const { appointmentId } = prepared.request;

    setActingAppointmentId(appointmentId);

    try {
      const result = await runAppointmentOperation(prepared.request);

      if (result.kind !== "success") {
        toast.error(result.message);

        // Backend reddettiyse elimizdeki liste bayat olabilir; ag hatasinda degildir.
        if (result.kind === "rejected") {
          router.refresh();
        }

        return;
      }

      if (result.status) {
        setStatusOverrides((current) => ({
          ...current,
          [appointmentId]: result.status as string
        }));
      }

      toast.success(result.message);
      setDraft(null);
      setOperationRevision((current) => current + 1);
      router.refresh();
    } finally {
      setActingAppointmentId(null);
    }
  }

  return {
    actingAppointmentId,
    closeDraft,
    draft,
    openOperation,
    operationRevision,
    statusOverrides,
    submitOperation,
    updateDraft
  };
}
