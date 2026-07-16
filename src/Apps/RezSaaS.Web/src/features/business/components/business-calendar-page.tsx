"use client";

import { CalendarDays, ChevronLeft, ChevronRight } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import {
  describeAppointmentActions,
  getAppointmentStatus,
  type AppointmentOperationKind
} from "@/features/business/api/business-appointment-operations";
import type { BusinessAppointment } from "@/features/business/api/get-business-appointments";
import type { BusinessTenantContext } from "@/features/business/api/get-business-context";
import { OperationSurface } from "@/features/business/components/appointment-operation-surface";
import { useAppointmentOperations } from "@/features/business/hooks/use-appointment-operations";
import {
  formatWindow,
  getDurationMinutes,
  getServiceSummary
} from "@/features/business/lib/appointment-format";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle
} from "@/components/ui/card";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle
} from "@/components/ui/dialog";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle
} from "@/components/ui/sheet";
import { Skeleton } from "@/components/ui/skeleton";
import { createTenantApiClient } from "@/shared/api/client";
import { useIsMobile } from "@/shared/hooks/use-mobile";
import {
  formatBranchDateLabel,
  formatBranchTimeLabel,
  getBranchTimeParts,
  parseBranchDateTimeLocalValue
} from "@/shared/lib/date-time";

/**
 * /panel/takvim -- kesinlesmis randevularin GUN takvimi.
 *
 * MVP: TEK EKSEN (personel), GUN gorunumu. Iki eksenli / surukle-birak / premium takvim YOK.
 * Randevu operasyonlari (tamamla / iptal / gelmedi / not / yeniden planla) ORTAK
 * useAppointmentOperations + OperationSurface uzerinden kosar -- yeni operasyon YAZILMAZ.
 *
 * SAAT DILIMI: her randevu SUBE saat diliminde gosterilir; tarayici saatine SESSIZCE
 * cevrilmez (sube baska sehirde olabilir).
 *
 * VERI: gorunen gun icin GET /api/business/appointments?fromUtc&toUtc ile o gunun
 * araligi cekilir (acik UTC araligi -- parametresiz cagri gecmiste 500 veriyordu).
 * Ilk gun, sunucudan gelen on-yukleme ile aninda cizilir; gun degisince tazelenir.
 */

const DAY_MS = 24 * 60 * 60 * 1000;
const UNASSIGNED_STAFF_KEY = "__unassigned__";

type BusinessCalendarPageProps = {
  initialAppointments: ReadonlyArray<BusinessAppointment>;
  branchTimeZoneId: string;
  tenant: BusinessTenantContext | null;
};

type StatusPresentation = {
  label: string;
  badge: "default" | "secondary" | "destructive" | "outline";
  accent: string;
};

/** Statu -> gorunum. Renk TEK sinyal degildir; rozet METNI her zaman yazilir. */
const statusPresentation: Record<string, StatusPresentation> = {
  Confirmed: { label: "Onaylı", badge: "default", accent: "border-l-primary" },
  Completed: {
    label: "Tamamlandı",
    badge: "secondary",
    accent: "border-l-muted-foreground"
  },
  Cancelled: {
    label: "İptal edildi",
    badge: "destructive",
    accent: "border-l-destructive"
  },
  NoShow: { label: "Gelmedi", badge: "destructive", accent: "border-l-destructive" },
  Rebooked: {
    label: "Yeniden planlandı",
    badge: "outline",
    accent: "border-l-muted-foreground"
  }
};

function presentationFor(status: string): StatusPresentation {
  return (
    statusPresentation[status] ?? {
      label: status,
      badge: "outline",
      accent: "border-l-border"
    }
  );
}

export function BusinessCalendarPage({
  initialAppointments,
  branchTimeZoneId,
  tenant
}: BusinessCalendarPageProps) {
  const isMobile = useIsMobile();
  const tenantId = tenant?.tenantId ?? null;
  const ops = useAppointmentOperations(tenantId);

  const [selectedDateUtc, setSelectedDateUtc] = useState<string>(() =>
    new Date().toISOString()
  );
  // Gun -> o gunun randevulari. Ilk paint'te bosluk olmamasi icin on-yukleme ile tohumlanir.
  const [dayCache, setDayCache] = useState<Record<string, BusinessAppointment[]>>(
    () => bucketByBranchDay(initialAppointments, branchTimeZoneId)
  );
  // Ilk fetch effect'ten once "randevu yok" yanip sonmemesi icin: tenant varsa yukleniyor say.
  const [loadingDay, setLoadingDay] = useState(() => Boolean(tenant?.tenantId));
  const [loadError, setLoadError] = useState<string | null>(null);
  // Detay yuzeyinde acik olan randevunun kimligi (operasyon secimi buradan yapilir).
  const [detailAppointmentId, setDetailAppointmentId] = useState<string | null>(
    null
  );

  const dayKey = getBranchDayKey(selectedDateUtc, branchTimeZoneId);
  const operationRevision = ops.operationRevision;

  // Gorunen gunun randevularini backend'den cek. Gun degisince VE her basarili
  // operasyondan sonra (operationRevision) tazelenir; yeniden planlama YENI kayit yaratir.
  useEffect(() => {
    if (!tenantId) {
      return;
    }

    let cancelled = false;
    const { fromUtc, toUtc } = branchDayRangeUtc(selectedDateUtc, branchTimeZoneId);

    setLoadingDay(true);
    setLoadError(null);

    createTenantApiClient(tenantId)
      .GET("/api/business/appointments", {
        params: { query: { fromUtc, toUtc, take: 200 } }
      })
      .then(({ data, response }) => {
        if (cancelled) {
          return;
        }

        if (!response.ok) {
          setLoadError("Bu günün randevuları alınamadı. Tekrar dene.");
          return;
        }

        setDayCache((current) => ({
          ...current,
          [dayKey]: data?.appointments ?? []
        }));
      })
      .catch(() => {
        if (!cancelled) {
          setLoadError("Bu günün randevuları yüklenemedi. Tekrar dene.");
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLoadingDay(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [dayKey, selectedDateUtc, branchTimeZoneId, tenantId, operationRevision]);

  // O gunun randevulari: statu override'lari uygulanir (backend yaniti gelene kadar
  // rozetin bayat gorunmemesi icin), baslangica gore siralanir.
  const dayAppointments = useMemo(() => {
    const source = dayCache[dayKey] ?? [];

    return source
      .map((appointment) => {
        const override = appointment.appointmentId
          ? ops.statusOverrides[appointment.appointmentId]
          : undefined;
        return override ? { ...appointment, status: override } : appointment;
      })
      .slice()
      .sort((left, right) =>
        (left.startUtc ?? "").localeCompare(right.startUtc ?? "")
      );
  }, [dayCache, dayKey, ops.statusOverrides]);

  // TEK EKSEN: personel. Her personel bir sutun; atanmamislar sona.
  const staffColumns = useMemo(
    () => groupByStaff(dayAppointments),
    [dayAppointments]
  );

  const detailAppointment = useMemo(
    () =>
      dayAppointments.find(
        (appointment) => appointment.appointmentId === detailAppointmentId
      ) ?? null,
    [dayAppointments, detailAppointmentId]
  );

  function shiftDay(days: number) {
    setSelectedDateUtc((current) =>
      new Date(new Date(current).getTime() + days * DAY_MS).toISOString()
    );
  }

  function selectOperation(
    appointment: BusinessAppointment,
    kind: AppointmentOperationKind
  ) {
    // Once detay yuzeyini kapat, sonra operasyon taslagini ac (ust uste binmesin).
    setDetailAppointmentId(null);
    ops.openOperation(appointment, kind);
  }

  const isEmpty = !loadingDay && dayAppointments.length === 0 && !loadError;

  return (
    <div className="space-y-6">
      <header className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div className="space-y-1">
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <CalendarDays aria-hidden className="size-4" />
            Gün takvimi
          </div>
          <h1 className="text-2xl font-semibold tracking-tight sm:text-3xl">
            {formatBranchDateLabel(selectedDateUtc, branchTimeZoneId)}
          </h1>
          <p className="text-sm text-muted-foreground">
            Randevular{" "}
            <strong className="font-medium text-foreground">şube saatine</strong> (
            {branchTimeZoneId}) göre gösterilir — cihazının saatine çevrilmez.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <Button
            aria-label="Önceki gün"
            className="min-h-11"
            onClick={() => shiftDay(-1)}
            variant="outline"
          >
            <ChevronLeft aria-hidden className="size-4" />
            Önceki gün
          </Button>
          <Button
            className="min-h-11"
            onClick={() => setSelectedDateUtc(new Date().toISOString())}
            variant="secondary"
          >
            Bugün
          </Button>
          <Button
            aria-label="Sonraki gün"
            className="min-h-11"
            onClick={() => shiftDay(1)}
            variant="outline"
          >
            Sonraki gün
            <ChevronRight aria-hidden className="size-4" />
          </Button>
        </div>
      </header>

      {loadError ? (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{loadError}</CardTitle>
          </CardHeader>
          <CardContent>
            <Button
              className="min-h-11"
              onClick={() => shiftDay(0)}
              variant="outline"
            >
              Yeniden dene
            </Button>
          </CardContent>
        </Card>
      ) : null}

      {loadingDay && dayAppointments.length === 0 ? (
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 3 }).map((_, index) => (
            <Skeleton className="h-28 w-full rounded-xl" key={index} />
          ))}
        </div>
      ) : null}

      {isEmpty ? (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">
              Bu günde kesinleşmiş randevu yok
            </CardTitle>
          </CardHeader>
          <CardContent className="text-sm text-muted-foreground">
            Başka bir güne geçmek için önceki/sonraki gün düğmelerini kullan.
          </CardContent>
        </Card>
      ) : null}

      {dayAppointments.length > 0 ? (
        <>
          {/* Masaustu/tablet: personel sutunlari (tek eksen). Cok personelde yatay kaydirir. */}
          <div className="hidden overflow-x-auto pb-2 md:block">
            <div className="flex gap-4">
              {staffColumns.map((column) => (
                <section
                  className="flex w-72 shrink-0 flex-col gap-3"
                  key={column.key}
                >
                  <div className="flex items-center justify-between gap-2 border-b pb-2">
                    <h2 className="truncate text-sm font-semibold">
                      {column.name}
                    </h2>
                    <Badge variant="outline">{column.appointments.length}</Badge>
                  </div>
                  {column.appointments.map((appointment, index) => (
                    <AppointmentBlock
                      appointment={appointment}
                      branchTimeZoneId={branchTimeZoneId}
                      key={blockKey(appointment, index)}
                      onOpen={() =>
                        setDetailAppointmentId(appointment.appointmentId ?? null)
                      }
                    />
                  ))}
                </section>
              ))}
            </div>
          </div>

          {/* <768px: tek gun, dikey liste (personel etiketi blok icinde). */}
          <div className="grid gap-3 md:hidden">
            {dayAppointments.map((appointment, index) => (
              <AppointmentBlock
                appointment={appointment}
                branchTimeZoneId={branchTimeZoneId}
                key={blockKey(appointment, index)}
                onOpen={() =>
                  setDetailAppointmentId(appointment.appointmentId ?? null)
                }
                showStaff
              />
            ))}
          </div>
        </>
      ) : null}

      {detailAppointment ? (
        <AppointmentDetail
          appointment={detailAppointment}
          isMobile={isMobile}
          isSubmitting={
            ops.actingAppointmentId === detailAppointment.appointmentId
          }
          onClose={() => setDetailAppointmentId(null)}
          onSelect={selectOperation}
        />
      ) : null}

      {ops.draft ? (
        <OperationSurface
          draft={ops.draft}
          isMobile={isMobile}
          isSubmitting={
            ops.actingAppointmentId === ops.draft.appointment.appointmentId
          }
          onClose={ops.closeDraft}
          onDraftChange={ops.updateDraft}
          onSubmit={() => void ops.submitOperation()}
        />
      ) : null}
    </div>
  );
}

/** Takvim blogu: dokunma hedefi min 44px, statu rozeti METIN tasir. */
function AppointmentBlock({
  appointment,
  branchTimeZoneId,
  onOpen,
  showStaff = false
}: {
  appointment: BusinessAppointment;
  branchTimeZoneId: string;
  onOpen: () => void;
  showStaff?: boolean;
}) {
  const presentation = presentationFor(getAppointmentStatus(appointment));
  const start = appointment.startUtc
    ? formatBranchTimeLabel(appointment.startUtc, branchTimeZoneId)
    : "--:--";
  const end = appointment.endUtc
    ? formatBranchTimeLabel(appointment.endUtc, branchTimeZoneId)
    : "--:--";

  return (
    <button
      className={`flex min-h-11 w-full flex-col gap-1 rounded-xl border border-l-4 bg-card p-3 text-left transition hover:bg-accent focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-ring/50 ${presentation.accent}`}
      onClick={onOpen}
      type="button"
    >
      <div className="flex items-center justify-between gap-2">
        <span className="text-sm font-semibold tabular-nums">
          {start} – {end}
        </span>
        <Badge variant={presentation.badge}>{presentation.label}</Badge>
      </div>
      <span className="truncate text-sm">{getServiceSummary(appointment)}</span>
      <span className="text-xs text-muted-foreground">
        {getDurationMinutes(appointment)} dk
        {showStaff
          ? ` · ${appointment.staffMemberDisplayName ?? "Personel atanmamış"}`
          : ""}
      </span>
    </button>
  );
}

/**
 * Randevu detayi + operasyon secimi.
 *
 * Masaustunde Dialog, mobilde alttan Sheet. Operasyon dugmeleri describeAppointmentActions
 * ile uretilir -- liste ekraniyla AYNI kurallar (kapali aksiyon disabled + nedeni etikette).
 */
function AppointmentDetail({
  appointment,
  isMobile,
  isSubmitting,
  onClose,
  onSelect
}: {
  appointment: BusinessAppointment;
  isMobile: boolean;
  isSubmitting: boolean;
  onClose: () => void;
  onSelect: (
    appointment: BusinessAppointment,
    kind: AppointmentOperationKind
  ) => void;
}) {
  const presentation = presentationFor(getAppointmentStatus(appointment));
  const actions = describeAppointmentActions(appointment, isSubmitting);
  const title = getServiceSummary(appointment);
  const description = formatWindow(appointment);

  const body = (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-2">
        <Badge variant={presentation.badge}>{presentation.label}</Badge>
        <span className="text-sm text-muted-foreground">
          {getDurationMinutes(appointment)} dk
        </span>
      </div>

      <dl className="grid gap-3 text-sm">
        <div>
          <dt className="text-muted-foreground">Personel</dt>
          <dd>{appointment.staffMemberDisplayName ?? "Personel atanmamış"}</dd>
        </div>
        <div>
          <dt className="text-muted-foreground">Müşteri</dt>
          {/* Musteri backend'de ZATEN maskeli doner; "tam bilgiyi goster" ucu YOKTUR. */}
          <dd>{appointment.customer?.maskedPhone ?? "Telefon yok"}</dd>
          <dd className="text-muted-foreground">
            {appointment.customer?.maskedEmail ?? "E-posta yok"}
          </dd>
        </div>
        {appointment.businessNote ? (
          <div>
            <dt className="text-muted-foreground">İşletme notu</dt>
            <dd className="rounded-md bg-muted px-3 py-2">
              {appointment.businessNote}
            </dd>
          </div>
        ) : null}
      </dl>

      <div className="grid gap-2">
        {actions.map((action) => (
          <Button
            className="min-h-11 w-full justify-start"
            disabled={action.disabled}
            key={action.kind}
            onClick={() => onSelect(appointment, action.kind)}
            variant={action.destructive ? "destructive" : "outline"}
          >
            {action.label}
          </Button>
        ))}
      </div>
    </div>
  );

  if (isMobile) {
    return (
      <Sheet onOpenChange={(open) => (open ? undefined : onClose())} open>
        <SheetContent className="max-h-[90vh] overflow-y-auto" side="bottom">
          <SheetHeader>
            <SheetTitle>{title}</SheetTitle>
            <SheetDescription>{description}</SheetDescription>
          </SheetHeader>
          <div className="px-4 pb-4">{body}</div>
        </SheetContent>
      </Sheet>
    );
  }

  return (
    <Dialog onOpenChange={(open) => (open ? undefined : onClose())} open>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>{description}</DialogDescription>
        </DialogHeader>
        {body}
      </DialogContent>
    </Dialog>
  );
}

type StaffColumn = {
  key: string;
  name: string;
  appointments: BusinessAppointment[];
};

/** Randevulari personele gore sutunlara ayirir; atanmamis personel EN SONA. */
function groupByStaff(
  appointments: ReadonlyArray<BusinessAppointment>
): StaffColumn[] {
  const columns = new Map<string, StaffColumn>();

  for (const appointment of appointments) {
    const key = appointment.staffMemberId ?? UNASSIGNED_STAFF_KEY;
    const existing = columns.get(key);

    if (existing) {
      existing.appointments.push(appointment);
      continue;
    }

    columns.set(key, {
      key,
      name: appointment.staffMemberDisplayName ?? "Personel atanmamış",
      appointments: [appointment]
    });
  }

  return Array.from(columns.values()).sort((left, right) => {
    if (left.key === UNASSIGNED_STAFF_KEY) {
      return 1;
    }
    if (right.key === UNASSIGNED_STAFF_KEY) {
      return -1;
    }
    return left.name.localeCompare(right.name, "tr");
  });
}

/** Randevulari SUBE-gun anahtarina gore gruplar (on-yukleme tohumu). */
function bucketByBranchDay(
  appointments: ReadonlyArray<BusinessAppointment>,
  branchTimeZoneId: string
): Record<string, BusinessAppointment[]> {
  const buckets: Record<string, BusinessAppointment[]> = {};

  for (const appointment of appointments) {
    if (!appointment.startUtc) {
      continue;
    }

    const key = getBranchDayKey(appointment.startUtc, branchTimeZoneId);
    (buckets[key] ??= []).push(appointment);
  }

  return buckets;
}

/** "YYYY-MM-DD" -- SUBE saat diliminde takvim gunu. */
function getBranchDayKey(valueUtc: string, branchTimeZoneId: string): string {
  const parts = getBranchTimeParts(valueUtc, branchTimeZoneId);

  if (!parts) {
    return valueUtc;
  }

  return `${parts.year}-${pad(parts.month)}-${pad(parts.day)}`;
}

/** Secili gunun SUBE saatindeki 00:00 ile ertesi gun 00:00 arasi UTC araligi. */
function branchDayRangeUtc(
  valueUtc: string,
  branchTimeZoneId: string
): { fromUtc: string; toUtc: string } {
  const parts = getBranchTimeParts(valueUtc, branchTimeZoneId);

  if (!parts) {
    // Saat dilimi cozulemezse en azindan gecerli bir aralik gonder (500'e dusme).
    const start = new Date(valueUtc);
    return {
      fromUtc: start.toISOString(),
      toUtc: new Date(start.getTime() + DAY_MS).toISOString()
    };
  }

  const from = branchMidnightUtc(parts.year, parts.month, parts.day, branchTimeZoneId);
  // Ertesi takvim gununu ay/yil sinirindan bagimsiz bul (UTC oglenden +24s).
  const nextNoon = new Date(Date.UTC(parts.year, parts.month - 1, parts.day, 12) + DAY_MS);
  const to = branchMidnightUtc(
    nextNoon.getUTCFullYear(),
    nextNoon.getUTCMonth() + 1,
    nextNoon.getUTCDate(),
    branchTimeZoneId
  );

  return { fromUtc: from, toUtc: to };
}

/** Verilen SUBE takvim gununun 00:00'inin UTC ISO karsiligi. */
function branchMidnightUtc(
  year: number,
  month: number,
  day: number,
  branchTimeZoneId: string
): string {
  const local = `${year}-${pad(month)}-${pad(day)}T00:00`;
  return (
    parseBranchDateTimeLocalValue(local, branchTimeZoneId) ??
    new Date(Date.UTC(year, month - 1, day)).toISOString()
  );
}

function blockKey(appointment: BusinessAppointment, index: number): string {
  return appointment.appointmentId ?? `${appointment.branchId ?? "sube"}-${index}`;
}

function pad(value: number): string {
  return value.toString().padStart(2, "0");
}
