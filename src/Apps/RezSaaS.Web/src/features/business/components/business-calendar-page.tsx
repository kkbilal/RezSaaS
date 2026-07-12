"use client";

import { useMemo, useState } from "react";
import type { BusinessAppointment } from "@/features/business/api/get-business-appointments";
import type { BusinessTenantContext } from "@/features/business/api/get-business-context";
import { routes, withTenant } from "@/shared/config/routes";
import {
  formatBranchDateLabel,
  getBranchTimeParts
} from "@/shared/lib/date-time";
import {
  CalendarGrid,
  type CalendarEvent,
  type CalendarEventTone,
  type CalendarView
} from "@/shared/ui/calendar-grid";
import { Card, CardDescription, CardHeader, CardTitle } from "@/shared/ui/card";
import { EmptyState } from "@/shared/ui/empty-state";
import { Tabs } from "@/shared/ui/tabs";

type BusinessCalendarPageProps = {
  appointments: ReadonlyArray<BusinessAppointment>;
  branchTimeZoneId: string;
  tenant: BusinessTenantContext | null;
};

const dayOffsetMs = 24 * 60 * 60 * 1000;

export function BusinessCalendarPage({
  appointments,
  branchTimeZoneId,
  tenant
}: BusinessCalendarPageProps) {
  const [view, setView] = useState<CalendarView>("week");
  const [selectedDateUtc, setSelectedDateUtc] = useState<string>(() =>
    new Date().toISOString()
  );

  const tenantId = tenant?.tenantId ?? null;

  const events = useMemo(
    () => appointments.map(mapAppointmentToEvent).filter(hasEventFields),
    [appointments]
  );

  const workingHours = useMemo(() => deriveWorkingHours(events, branchTimeZoneId), [
    events,
    branchTimeZoneId
  ]);

  function shiftSelectedDate(days: number) {
    setSelectedDateUtc((current) => {
      const base = new Date(current).getTime();
      return new Date(base + days * dayOffsetMs).toISOString();
    });
  }

  return (
    <div className="space-y-6">
        <section className="fade-up space-y-4">
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <p className="w-fit rounded-full bg-[var(--rs-accent-soft)] px-4 py-2 text-xs font-medium uppercase tracking-[0.18em] text-[var(--rs-accent-strong)]">
                Kesinleşmiş randevu takvimi
              </p>
              <h1 className="mt-3 text-3xl font-semibold tracking-[-0.06em] text-[var(--rs-ink)] sm:text-4xl">
                {formatBranchDateLabel(selectedDateUtc, branchTimeZoneId)}
              </h1>
              <p className="mt-2 text-sm text-[var(--rs-muted-strong)]">
                Saatler şube zaman dilimine göre ({branchTimeZoneId}) gösterilir.
                İç kaynak ataması müşteriye gösterilmez.
              </p>
            </div>

            <div className="flex flex-wrap items-center gap-2">
              <button
                type="button"
                onClick={() => shiftSelectedDate(view === "day" ? -1 : -7)}
                className="inline-flex h-10 items-center justify-center rounded-full border border-[var(--rs-border)] bg-[var(--rs-surface)] px-4 text-sm font-medium text-[var(--rs-ink)] shadow-[var(--rs-shadow-soft)] transition hover:-translate-y-0.5"
                aria-label="Önceki"
              >
                ‹ {view === "day" ? "Önceki gün" : "Önceki hafta"}
              </button>
              <button
                type="button"
                onClick={() => setSelectedDateUtc(new Date().toISOString())}
                className="inline-flex h-10 items-center justify-center rounded-full border border-[var(--rs-border)] bg-[var(--rs-surface)] px-4 text-sm font-medium text-[var(--rs-ink)] shadow-[var(--rs-shadow-soft)] transition hover:-translate-y-0.5"
              >
                Bugün
              </button>
              <button
                type="button"
                onClick={() => shiftSelectedDate(view === "day" ? 1 : 7)}
                className="inline-flex h-10 items-center justify-center rounded-full border border-[var(--rs-border)] bg-[var(--rs-surface)] px-4 text-sm font-medium text-[var(--rs-ink)] shadow-[var(--rs-shadow-soft)] transition hover:-translate-y-0.5"
                aria-label="Sonraki"
              >
                {view === "day" ? "Sonraki gün" : "Sonraki hafta"} ›
              </button>
              <Tabs
                items={[
                  { label: "Gün", value: "day" },
                  { label: "Hafta", value: "week" }
                ]}
                onChange={setView}
                size="sm"
                value={view}
              />
            </div>
          </div>

          {tenantId ? (
            <CalendarGrid
              branchTimeZoneId={branchTimeZoneId}
              emptyHint="Bu aralıkta kesinleşmiş randevu yok."
              events={events}
              onEventClick={(event) => {
                const target = withTenant(routes.business.appointments, tenantId);
                window.location.href = `${target}?takvim=${encodeURIComponent(event.id)}`;
              }}
              selectedDateUtc={selectedDateUtc}
              view={view}
              workingHourEnd={workingHours.end}
              workingHourStart={workingHours.start}
            />
          ) : (
            <Card className="p-6">
              <EmptyState
                description="Takvim için yetkili işletme ve şube bilgisi doğrulanmalı."
                title="İşletme seçilmedi"
              />
            </Card>
          )}
        </section>

        <Card className="p-5">
          <CardHeader>
            <CardTitle>Çalışma penceresi</CardTitle>
            <CardDescription>
              Takvim otomatik olarak randevuların yayıldığı saat aralığını baz
              alır; varsayılan 08:00–18:00 penceresidir.
            </CardDescription>
          </CardHeader>
          <p className="mt-3 text-sm text-[var(--rs-muted-strong)]">
            Görüntülenen pencere: {String(workingHours.start).padStart(2, "0")}:00 –{" "}
            {String(workingHours.end).padStart(2, "0")}:00 ({branchTimeZoneId})
          </p>
        </Card>
    </div>
  );
}

function mapAppointmentToEvent(appointment: BusinessAppointment): CalendarEvent {
  const status = appointment.status ?? "Confirmed";
  const title = getEventTitle(appointment);
  const subtitle =
    appointment.staffMemberDisplayName ?? "Personel atanacak";

  return {
    id: appointment.appointmentId ?? appointment.appointmentRequestId ?? "",
    startUtc: appointment.startUtc ?? "",
    endUtc: appointment.endUtc ?? "",
    title,
    subtitle,
    tone: mapStatusToTone(status)
  };
}

function getEventTitle(appointment: BusinessAppointment): string {
  const customer = appointment.customer;
  const handle =
    customer && typeof customer === "object"
      ? (customer as { handle?: string | null }).handle ?? null
      : null;

  if (handle) {
    return handle;
  }

  return "Randevu";
}

function mapStatusToTone(status: string): CalendarEventTone {
  if (status === "Completed") {
    return "neutral";
  }

  if (status === "NoShow") {
    return "warning";
  }

  if (
    status === "CancelledByCustomer" ||
    status === "Cancelled" ||
    status === "CancelledByAppeal"
  ) {
    return "danger";
  }

  return "accent";
}

function hasEventFields(event: CalendarEvent): boolean {
  return Boolean(event.id && event.startUtc && event.endUtc);
}

function deriveWorkingHours(
  events: ReadonlyArray<CalendarEvent>,
  branchTimeZoneId: string
): { start: number; end: number } {
  let minHour = 8;
  let maxHour = 18;

  for (const event of events) {
    const start = getBranchTimeParts(event.startUtc, branchTimeZoneId);
    const end = getBranchTimeParts(event.endUtc, branchTimeZoneId);

    if (!start || !end) {
      continue;
    }

    minHour = Math.min(minHour, start.hour);
    maxHour = Math.max(maxHour, end.minute > 0 ? end.hour + 1 : end.hour);
  }

  if (maxHour <= minHour) {
    maxHour = minHour + 1;
  }

  return { start: minHour, end: Math.min(maxHour, 22) };
}
