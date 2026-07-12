"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import type {
  BusinessAppointment,
  BusinessAppointmentScheduleState
} from "@/features/business/api/get-business-appointments";
import { createTenantApiClient } from "@/shared/api/client";
import {
  formatBranchDateTime,
  parseBranchDateTimeLocalValue,
  toBranchDateTimeLocalValue
} from "@/shared/lib/date-time";
import { createWebIdempotencyKey } from "@/shared/lib/idempotency";
import { Button } from "@/shared/ui/button";
import { Card, CardDescription, CardTitle } from "@/shared/ui/card";
import { DialogOverlay, DialogPanel } from "@/shared/ui/dialog";
import { StatusBadge } from "@/shared/ui/status-badge";

type ScheduleFilter = "active" | "all" | "Completed" | "closed";
type AppointmentOperation =
  | "cancel"
  | "complete"
  | "no-show"
  | "note"
  | "rebook"
  | "resource-block";

type AppointmentOperationDraft = {
  appointment: BusinessAppointment;
  endUtcInput: string;
  idempotencyKey: string;
  kind: AppointmentOperation;
  startUtcInput: string;
  text: string;
};

type BusinessAppointmentScheduleProps = {
  onToast: (message: string) => void;
  schedule: BusinessAppointmentScheduleState;
  tenantId: string | null;
};

export function BusinessAppointmentSchedule({
  onToast,
  schedule,
  tenantId
}: BusinessAppointmentScheduleProps) {
  const router = useRouter();
  const [actingAppointmentId, setActingAppointmentId] = useState<string | null>(
    null
  );
  const [filter, setFilter] = useState<ScheduleFilter>("active");
  const [operationDraft, setOperationDraft] =
    useState<AppointmentOperationDraft | null>(null);
  const [search, setSearch] = useState("");
  const [statusOverrides, setStatusOverrides] = useState<Record<string, string>>(
    {}
  );
  const [noteOverrides, setNoteOverrides] = useState<Record<string, string>>({});

  const appointments = useMemo(() => {
    if (schedule.kind !== "ready") {
      return [];
    }

    return schedule.appointments.map((appointment) => {
      const appointmentId = appointment.appointmentId;
      const statusOverride = appointmentId
        ? statusOverrides[appointmentId]
        : undefined;
      const noteOverride = appointmentId ? noteOverrides[appointmentId] : undefined;

      return {
        ...appointment,
        ...(statusOverride ? { status: statusOverride } : {}),
        ...(noteOverride !== undefined ? { businessNote: noteOverride } : {})
      };
    });
  }, [noteOverrides, schedule, statusOverrides]);

  const visibleAppointments = useMemo(() => {
    return appointments.filter((appointment) => {
      const status = getAppointmentStatus(appointment);
      const matchesFilter =
        filter === "all"
          ? true
          : filter === "active"
            ? status === "Confirmed"
            : filter === "closed"
              ? ["Cancelled", "NoShow", "Rebooked"].includes(status)
              : status === filter;
      const haystack = [
        appointment.appointmentId,
        appointment.branchDisplayName,
        appointment.customer?.maskedEmail,
        appointment.customer?.maskedPhone,
        getCustomerHandle(appointment),
        getServiceSummary(appointment),
        appointment.staffMemberDisplayName,
        appointment.resourceDisplayName,
        status
      ]
        .join(" ")
        .toLocaleLowerCase("tr-TR");

      return matchesFilter && haystack.includes(search.toLocaleLowerCase("tr-TR"));
    });
  }, [appointments, filter, search]);

  function openOperation(
    appointment: BusinessAppointment,
    kind: AppointmentOperation
  ) {
    if (!tenantId || !appointment.appointmentId) {
      onToast("İşlem için yetkili işletme ve randevu bilgisi doğrulanmalı.");
      return;
    }

    setOperationDraft({
      appointment,
      endUtcInput: toBranchDateTimeLocalValue(
        appointment.endUtc,
        appointment.branchTimeZoneId
      ),
      idempotencyKey: createWebIdempotencyKey(`appointment-${kind}`),
      kind,
      startUtcInput: toBranchDateTimeLocalValue(
        appointment.startUtc,
        appointment.branchTimeZoneId
      ),
      text: kind === "note" ? appointment.businessNote ?? "" : ""
    });
  }

  async function submitOperation() {
    if (!operationDraft) {
      return;
    }

    const appointmentId = operationDraft.appointment.appointmentId;
    const text = operationDraft.text.trim();

    if (!tenantId || !appointmentId) {
      onToast("İşlem için randevu bilgisi doğrulanmalı.");
      return;
    }

    if (operationNeedsReason(operationDraft.kind) && text.length < 3) {
      onToast("İptal, gelmedi ve yeniden planlama gibi kararlar için kısa sebep gerekli.");
      return;
    }

    const needsTimeRange = operationNeedsTimeRange(operationDraft.kind);
    const branchTimeZoneId = operationDraft.appointment.branchTimeZoneId;
    const startUtc = needsTimeRange
      ? parseBranchDateTimeLocalValue(
          operationDraft.startUtcInput,
          branchTimeZoneId
        )
      : null;
    const endUtc = needsTimeRange
      ? parseBranchDateTimeLocalValue(operationDraft.endUtcInput, branchTimeZoneId)
      : null;

    if (needsTimeRange && (!startUtc || !endUtc)) {
      onToast("Başlangıç ve bitiş şube zamanı geçerli olmalı.");
      return;
    }

    if (startUtc && endUtc && new Date(endUtc).getTime() <= new Date(startUtc).getTime()) {
      onToast("Bitiş zamanı başlangıçtan sonra olmalı.");
      return;
    }

    if (operationDraft.kind === "resource-block" && !operationDraft.appointment.resourceId) {
      onToast("Kaynak bloklama için iç kaynak bilgisi doğrulanmalı.");
      return;
    }

    const client = createTenantApiClient(tenantId);
    const idempotencyKey = operationDraft.idempotencyKey;

    setActingAppointmentId(appointmentId);

    try {
      const result =
        operationDraft.kind === "cancel"
          ? await client.POST("/api/business/appointments/{appointmentId}/cancel", {
              body: {
                reason: text
              },
              params: {
                header: {
                  "Idempotency-Key": idempotencyKey
                },
                path: {
                  appointmentId
                }
              }
            })
          : operationDraft.kind === "complete"
            ? await client.POST(
                "/api/business/appointments/{appointmentId}/complete",
                {
                  body: {
                    note: text || null
                  },
                  params: {
                    header: {
                      "Idempotency-Key": idempotencyKey
                    },
                    path: {
                      appointmentId
                    }
                  }
                }
              )
            : operationDraft.kind === "no-show"
              ? await client.POST(
                  "/api/business/appointments/{appointmentId}/no-show",
                  {
                    body: {
                      reason: text
                    },
                    params: {
                      header: {
                        "Idempotency-Key": idempotencyKey
                      },
                      path: {
                        appointmentId
                      }
                    }
                  }
                )
              : operationDraft.kind === "rebook"
                ? await client.POST(
                    "/api/business/appointments/{appointmentId}/rebook",
                    {
                      body: {
                        endUtc: endUtc!,
                        reason: text,
                        resourceId: operationDraft.appointment.resourceId ?? null,
                        staffMemberId:
                          operationDraft.appointment.staffMemberId ?? null,
                        startUtc: startUtc!
                      },
                      params: {
                        header: {
                          "Idempotency-Key": idempotencyKey
                        },
                        path: {
                          appointmentId
                        }
                      }
                    }
                  )
                : operationDraft.kind === "resource-block"
                  ? await client.POST(
                      "/api/business/resources/{resourceId}/blocks",
                      {
                        body: {
                          endUtc: endUtc!,
                          reason: text,
                          startUtc: startUtc!
                        },
                        params: {
                          path: {
                            resourceId: operationDraft.appointment.resourceId!
                          }
                        }
                      }
                    )
                  : await client.POST(
                      "/api/business/appointments/{appointmentId}/notes",
                      {
                        body: {
                          note: text || null
                        },
                        params: {
                          header: {
                            "Idempotency-Key": idempotencyKey
                          },
                          path: {
                            appointmentId
                          }
                        }
                      }
                    );

      if (!result.response.ok) {
        onToast(getOperationErrorCopy(result.response.status, operationDraft.kind));
        router.refresh();
        return;
      }

      const nextStatus =
        result.data && "status" in result.data ? result.data.status : undefined;

      if (nextStatus) {
        setStatusOverrides((current) => ({
          ...current,
          [appointmentId]: nextStatus
        }));
      }

      if (operationDraft.kind === "note") {
        setNoteOverrides((current) => ({
          ...current,
          [appointmentId]: text
        }));
      }

      onToast(getOperationSuccessCopy(operationDraft.kind));
      setOperationDraft(null);
      router.refresh();
    } catch {
      onToast("Randevu işlemi şu anda tamamlanamadı. Lütfen tekrar dene.");
    } finally {
      setActingAppointmentId(null);
    }
  }

  return (
    <section className="space-y-4">
      <ScheduleToolbar
        activeFilter={filter}
        appointmentCount={appointments.length}
        onFilterChange={setFilter}
        onSearchChange={setSearch}
        search={search}
      />

      {schedule.kind === "unavailable" ? (
        <Card className="border-[var(--rs-warning-border)] bg-[var(--rs-warning-soft)] p-5 shadow-none">
          <CardTitle>Randevular yüklenemedi</CardTitle>
          <CardDescription className="mt-2 text-[var(--rs-warning)]">
            {schedule.reason} Lütfen kısa süre sonra tekrar dene.
          </CardDescription>
        </Card>
      ) : null}

      {schedule.kind === "ready" && visibleAppointments.length === 0 ? (
        <Card className="border-dashed bg-[var(--rs-glass)] p-10 text-center shadow-none">
          <CardTitle>Bu görünümde randevu yok</CardTitle>
          <CardDescription className="mx-auto mt-2 max-w-md">
            Filtreyi değiştirerek kesinleşmiş, tamamlanmış veya kapanmış
            randevuları inceleyebilirsin.
          </CardDescription>
        </Card>
      ) : null}

      {visibleAppointments.length > 0 ? (
        <div className="grid gap-4">
          {visibleAppointments.map((appointment, index) => (
            <BusinessAppointmentCard
              appointment={appointment}
              index={index}
              isSubmitting={actingAppointmentId === appointment.appointmentId}
              key={appointment.appointmentId ?? `${appointment.branchId}-${index}`}
              onOpenOperation={openOperation}
            />
          ))}
        </div>
      ) : null}

      {operationDraft ? (
        <AppointmentOperationDialog
          draft={operationDraft}
          isSubmitting={actingAppointmentId === operationDraft.appointment.appointmentId}
          onCancel={() => setOperationDraft(null)}
          onSubmit={() => void submitOperation()}
          onTextChange={(text) =>
            setOperationDraft((current) =>
              current
                ? {
                    ...current,
                    text
                  }
                : current
            )
          }
          onTimeChange={(field, value) =>
            setOperationDraft((current) =>
              current
                ? {
                    ...current,
                    [field]: value
                  }
                : current
            )
          }
        />
      ) : null}
    </section>
  );
}

function ScheduleToolbar({
  activeFilter,
  appointmentCount,
  onFilterChange,
  onSearchChange,
  search
}: {
  activeFilter: ScheduleFilter;
  appointmentCount: number;
  onFilterChange: (value: ScheduleFilter) => void;
  onSearchChange: (value: string) => void;
  search: string;
}) {
  const filters: Array<{ label: string; value: ScheduleFilter }> = [
    { label: "Aktif", value: "active" },
    { label: "Hepsi", value: "all" },
    { label: "Tamamlanan", value: "Completed" },
    { label: "Kapanan", value: "closed" }
  ];

  return (
    <Card className="fade-up p-4 [animation-delay:220ms]">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
        <div>
          <div className="flex flex-wrap items-center gap-3">
            <CardTitle>Kesinleşmiş randevular</CardTitle>
            <span className="rounded-full bg-[var(--rs-neutral-soft)] px-3 py-1 text-xs font-medium text-[var(--rs-muted)]">
              {appointmentCount} kayıt
            </span>
          </div>
          <CardDescription>
            Önümüzdeki operasyon penceresini şube saatine göre takip et.
          </CardDescription>
        </div>

        <div className="flex flex-col gap-3 sm:flex-row">
          <input
            className="min-h-11 rounded-full border border-[var(--rs-border)] bg-[var(--rs-surface)] px-5 text-sm text-[var(--rs-ink)] shadow-[var(--rs-shadow-soft)] outline-none transition focus:border-[var(--rs-accent)] focus:ring-4 focus:ring-[rgba(99_102_241_/_0.18)]"
            onChange={(event) => onSearchChange(event.target.value)}
            placeholder="Randevu, hizmet veya müşteri ara"
            type="search"
            value={search}
          />
          <div className="flex rounded-full border border-[var(--rs-border)] bg-[var(--rs-surface-muted)] p-1">
            {filters.map((filter) => (
              <button
                className={
                  activeFilter === filter.value
                    ? "rounded-full bg-[var(--rs-surface)] px-4 py-2 text-xs font-medium text-[var(--rs-ink)] shadow-[var(--rs-shadow-soft)]"
                    : "rounded-full px-4 py-2 text-xs font-medium text-[var(--rs-muted)] transition hover:text-[var(--rs-ink)]"
                }
                key={filter.value}
                onClick={() => onFilterChange(filter.value)}
                type="button"
              >
                {filter.label}
              </button>
            ))}
          </div>
        </div>
      </div>
    </Card>
  );
}

function BusinessAppointmentCard({
  appointment,
  index,
  isSubmitting,
  onOpenOperation
}: {
  appointment: BusinessAppointment;
  index: number;
  isSubmitting: boolean;
  onOpenOperation: (
    appointment: BusinessAppointment,
    kind: AppointmentOperation
  ) => void;
}) {
  const status = getAppointmentStatus(appointment);
  const isConfirmed = status === "Confirmed";
  const completeIsOpen = canComplete(appointment);
  const noShowIsOpen = canMarkNoShow(appointment);

  return (
    <article
      className="fade-up rounded-[2rem] border border-[var(--rs-border)] bg-[var(--rs-glass)] p-5 shadow-[var(--rs-shadow-soft)] backdrop-blur-xl transition duration-300 hover:-translate-y-0.5 hover:shadow-[var(--rs-shadow-card)]"
      style={{ animationDelay: `${240 + index * 45}ms` }}
    >
      <div className="grid gap-5 lg:grid-cols-[1fr_auto] lg:items-start">
        <div className="space-y-5">
          <div className="flex flex-wrap items-center gap-3">
            <span className="rounded-full bg-[var(--rs-neutral-soft)] px-3 py-1 font-mono text-xs text-[var(--rs-muted)]">
              {shortGuid(appointment.appointmentId)}
            </span>
            <StatusBadge status={status} />
            <span className="text-xs text-[var(--rs-muted)]">
              {appointment.branchDisplayName ?? "Şube adı yok"}
            </span>
          </div>

          <div>
            <h3 className="text-2xl font-semibold tracking-[-0.05em] text-[var(--rs-ink)]">
              {getServiceSummary(appointment)}
            </h3>
            <p className="mt-1 text-sm text-[var(--rs-muted)]">
              Personel:{" "}
              <span className="font-medium text-[var(--rs-muted-strong)]">
                {appointment.staffMemberDisplayName ?? "Personel adı yok"}
              </span>
            </p>
          </div>

          <div className="grid gap-3 md:grid-cols-3">
            <AppointmentInfoBlock
              label="Müşteri hesabı"
              value={getCustomerHandle(appointment)}
            />
            <AppointmentInfoBlock
              label="Telefon"
              value={appointment.customer?.maskedPhone ?? "Telefon yok"}
            />
            <AppointmentInfoBlock
              label="E-posta"
              value={appointment.customer?.maskedEmail ?? "E-posta yok"}
            />
          </div>

          <div className="rounded-[1.5rem] border border-[var(--rs-border)] bg-[var(--rs-surface-muted)] p-4">
            <div className="flex flex-col gap-3 text-sm md:flex-row md:items-center md:justify-between">
              <div>
                <p className="text-xs uppercase tracking-[0.2em] text-[var(--rs-muted)]">
                  Şube saati
                </p>
                <p className="mt-1 font-medium text-[var(--rs-ink)]">
                  {formatAppointmentWindow(appointment)}
                </p>
              </div>
              <div className="text-left md:text-right">
                <p className="text-xs uppercase tracking-[0.2em] text-[var(--rs-muted)]">
                  İç kaynak
                </p>
                <p className="mt-1 font-medium text-[var(--rs-accent-strong)]">
                  {appointment.resourceDisplayName ?? "Kaynak adı yok"}
                </p>
              </div>
            </div>
            <p className="mt-3 text-xs text-[var(--rs-muted)]">
              Toplam süre: {getDurationMinutes(appointment)} dk ·{" "}
              {formatTotalPrice(appointment)}
            </p>
          </div>

          {appointment.businessNote ? (
            <p className="rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface)] px-4 py-3 text-sm leading-6 text-[var(--rs-muted-strong)]">
              <span className="font-medium text-[var(--rs-ink)]">Not: </span>
              {appointment.businessNote}
            </p>
          ) : null}

          {isConfirmed && (!completeIsOpen || !noShowIsOpen) ? (
            <p className="rounded-2xl border border-[var(--rs-warning-border)] bg-[var(--rs-warning-soft)] px-4 py-3 text-sm leading-6 text-[var(--rs-warning)]">
              Tamamlandı aksiyonu randevu bitişinden sonra, gelmedi aksiyonu
              randevu başlangıcından sonra açılır.
            </p>
          ) : null}
        </div>

        <div className="flex flex-wrap gap-3 lg:min-w-48 lg:flex-col">
          <Button
            className="flex-1 lg:w-full"
            disabled={isSubmitting}
            onClick={() => onOpenOperation(appointment, "note")}
            type="button"
            variant="secondary"
          >
            Not
          </Button>

          {isConfirmed ? (
            <>
              <Button
                className="flex-1 lg:w-full"
                disabled={isSubmitting}
                onClick={() => onOpenOperation(appointment, "cancel")}
                type="button"
                variant="danger"
              >
                İptal
              </Button>
              <Button
                className="flex-1 lg:w-full"
                disabled={isSubmitting || !noShowIsOpen}
                onClick={() => onOpenOperation(appointment, "no-show")}
                type="button"
                variant="secondary"
              >
                Gelmedi
              </Button>
              <Button
                className="flex-1 lg:w-full"
                disabled={isSubmitting || !completeIsOpen}
                onClick={() => onOpenOperation(appointment, "complete")}
                type="button"
              >
                Tamamlandı
              </Button>
              <Button
                className="flex-1 lg:w-full"
                disabled={isSubmitting}
                onClick={() => onOpenOperation(appointment, "rebook")}
                type="button"
                variant="secondary"
              >
                Yeniden planla
              </Button>
            </>
          ) : (
            <Button className="flex-1 lg:w-full" disabled type="button" variant="secondary">
              Operasyon kapalı
            </Button>
          )}
          {appointment.resourceId ? (
            <Button
              className="flex-1 lg:w-full"
              disabled={isSubmitting}
              onClick={() => onOpenOperation(appointment, "resource-block")}
              type="button"
              variant="secondary"
            >
              Kaynağı blokla
            </Button>
          ) : null}
        </div>
      </div>
    </article>
  );
}

function AppointmentOperationDialog({
  draft,
  isSubmitting,
  onCancel,
  onSubmit,
  onTimeChange,
  onTextChange
}: {
  draft: AppointmentOperationDraft;
  isSubmitting: boolean;
  onCancel: () => void;
  onSubmit: () => void;
  onTimeChange: (
    field: "startUtcInput" | "endUtcInput",
    value: string
  ) => void;
  onTextChange: (text: string) => void;
}) {
  const details = getOperationDetails(draft.kind);
  const showTimeRange = operationNeedsTimeRange(draft.kind);

  return (
    <DialogOverlay onEscapeKeyDown={onCancel}>
      <DialogPanel
        descriptionId="business-appointment-operation-dialog-description"
        titleId="business-appointment-operation-dialog-title"
      >
        <div className="space-y-4">
          <StatusBadge status={getAppointmentStatus(draft.appointment)} />
          <h2
            className="text-3xl font-semibold tracking-[-0.06em] text-[var(--rs-ink)]"
            id="business-appointment-operation-dialog-title"
          >
            {details.title}
          </h2>
          <p
            className="text-sm leading-7 text-[var(--rs-muted)]"
            id="business-appointment-operation-dialog-description"
          >
            {getServiceSummary(draft.appointment)} ·{" "}
            {formatAppointmentWindow(draft.appointment)}
          </p>
        </div>

        {showTimeRange ? (
          <div className="mt-6 grid gap-3 sm:grid-cols-2">
            <label className="block text-sm font-medium text-[var(--rs-ink)]">
              Başlangıç şube zamanı
              <input
                className="mt-3 min-h-11 w-full rounded-full border border-[var(--rs-border)] bg-[var(--rs-surface)] px-4 text-sm text-[var(--rs-ink)] shadow-[var(--rs-shadow-soft)] outline-none transition focus:border-[var(--rs-accent)] focus:ring-4 focus:ring-[rgba(99_102_241_/_0.18)]"
                onChange={(event) =>
                  onTimeChange("startUtcInput", event.target.value)
                }
                type="datetime-local"
                value={draft.startUtcInput}
              />
            </label>
            <label className="block text-sm font-medium text-[var(--rs-ink)]">
              Bitiş şube zamanı
              <input
                className="mt-3 min-h-11 w-full rounded-full border border-[var(--rs-border)] bg-[var(--rs-surface)] px-4 text-sm text-[var(--rs-ink)] shadow-[var(--rs-shadow-soft)] outline-none transition focus:border-[var(--rs-accent)] focus:ring-4 focus:ring-[rgba(99_102_241_/_0.18)]"
                onChange={(event) =>
                  onTimeChange("endUtcInput", event.target.value)
                }
                type="datetime-local"
                value={draft.endUtcInput}
              />
            </label>
            <p className="rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface)] px-4 py-3 text-xs leading-5 text-[var(--rs-muted)] sm:col-span-2">
              Şube saati önizleme:{" "}
              <span className="font-medium text-[var(--rs-ink)]">
                {formatDraftBranchPreview(draft)}
              </span>
            </p>
          </div>
        ) : null}

        <label className="mt-6 block text-sm font-medium text-[var(--rs-ink)]">
          {details.textareaLabel}
          <textarea
            className="mt-3 min-h-32 w-full resize-y rounded-[1.25rem] border border-[var(--rs-border)] bg-[var(--rs-surface)] px-4 py-3 text-sm leading-6 text-[var(--rs-ink)] shadow-[var(--rs-shadow-soft)] outline-none transition focus:border-[var(--rs-accent)] focus:ring-4 focus:ring-[rgba(99_102_241_/_0.18)]"
            maxLength={details.maxLength}
            onChange={(event) => onTextChange(event.target.value)}
            placeholder={details.placeholder}
            value={draft.text}
          />
        </label>

        <p className="mt-3 text-xs leading-5 text-[var(--rs-muted)]">
          {details.helper}
        </p>

        <div className="mt-6 flex flex-col gap-3 sm:flex-row sm:justify-end">
          <Button disabled={isSubmitting} onClick={onCancel} type="button" variant="secondary">
            Geri dön
          </Button>
          <Button disabled={isSubmitting} onClick={onSubmit} type="button">
            {isSubmitting ? "İşleniyor" : details.submitLabel}
          </Button>
        </div>
      </DialogPanel>
    </DialogOverlay>
  );
}

function AppointmentInfoBlock({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface)] p-4">
      <p className="text-xs text-[var(--rs-muted)]">{label}</p>
      <p className="mt-2 font-mono text-sm font-medium text-[var(--rs-ink)]">
        {value}
      </p>
    </div>
  );
}

function getAppointmentStatus(appointment: BusinessAppointment) {
  return appointment.status ?? "Unknown";
}

function getServiceSummary(appointment: BusinessAppointment) {
  const lines = appointment.lines ?? [];
  const firstLine = lines.at(0);
  const firstService = firstLine?.serviceNameSnapshot ?? "Hizmet detayı yok";

  if (lines.length <= 1) {
    return firstService;
  }

  return `${firstService} + ${lines.length - 1} hizmet`;
}

function getDurationMinutes(appointment: BusinessAppointment) {
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

function formatAppointmentWindow(appointment: BusinessAppointment) {
  if (!appointment.startUtc) {
    return "Zaman bilgisi yok";
  }

  if (!appointment.branchTimeZoneId) {
    return `${appointment.startUtc} UTC`;
  }

  const start = formatBranchDateTime(
    appointment.startUtc,
    appointment.branchTimeZoneId
  );

  if (!appointment.endUtc) {
    return start;
  }

  const end = formatBranchTime(appointment.endUtc, appointment.branchTimeZoneId);

  return `${start} - ${end}`;
}

function formatBranchTime(valueUtc: string, branchTimeZoneId: string) {
  const value = new Date(valueUtc);

  if (Number.isNaN(value.getTime())) {
    return "Zaman bilgisi okunamıyor";
  }

  return new Intl.DateTimeFormat("tr-TR", {
    timeStyle: "short",
    timeZone: branchTimeZoneId
  }).format(value);
}

function formatTotalPrice(appointment: BusinessAppointment) {
  const lines = appointment.lines ?? [];
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
    return `${amount} ${currencyCode}`;
  }
}

function getCustomerHandle(appointment: BusinessAppointment) {
  return shortGuid(appointment.customer?.userAccountId);
}

function shortGuid(value?: string | null) {
  if (!value) {
    return "Bilgi yok";
  }

  return `${value.slice(0, 8)}...`;
}

function canComplete(appointment: BusinessAppointment) {
  if (getAppointmentStatus(appointment) !== "Confirmed" || !appointment.endUtc) {
    return false;
  }

  const end = new Date(appointment.endUtc).getTime();

  return !Number.isNaN(end) && end <= Date.now();
}

function canMarkNoShow(appointment: BusinessAppointment) {
  if (getAppointmentStatus(appointment) !== "Confirmed" || !appointment.startUtc) {
    return false;
  }

  const start = new Date(appointment.startUtc).getTime();

  return !Number.isNaN(start) && start <= Date.now();
}

function operationNeedsReason(kind: AppointmentOperation) {
  return (
    kind === "cancel" ||
    kind === "no-show" ||
    kind === "rebook" ||
    kind === "resource-block"
  );
}

function operationNeedsTimeRange(kind: AppointmentOperation) {
  return kind === "rebook" || kind === "resource-block";
}

function getOperationDetails(kind: AppointmentOperation) {
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
      helper: "Not opsiyoneldir. Randevu yalnızca bitiş saatinden sonra tamamlanabilir.",
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

function getOperationSuccessCopy(kind: AppointmentOperation) {
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

function getOperationErrorCopy(status: number, kind: AppointmentOperation) {
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

function formatDraftBranchPreview(draft: AppointmentOperationDraft) {
  const branchTimeZoneId = draft.appointment.branchTimeZoneId;
  const startUtc = parseBranchDateTimeLocalValue(
    draft.startUtcInput,
    branchTimeZoneId
  );
  const endUtc = parseBranchDateTimeLocalValue(
    draft.endUtcInput,
    branchTimeZoneId
  );

  if (!startUtc || !endUtc) {
    return "Geçerli şube zamanı gir.";
  }

  if (!branchTimeZoneId) {
    return `${startUtc} - ${endUtc}`;
  }

  return `${formatBranchDateTime(startUtc, branchTimeZoneId)} - ${formatBranchTime(
    endUtc,
    branchTimeZoneId
  )}`;
}
