"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { Suspense, useMemo, useState, type ReactNode } from "react";
import { toast } from "sonner";
import {
  canCompleteAppointment,
  canMarkAppointmentNoShow,
  getAppointmentStatus,
  getOperationDetails,
  operationIsDestructive,
  operationNeedsTimeRange,
  prepareAppointmentOperation,
  runAppointmentOperation,
  type AppointmentOperationKind
} from "@/features/business/api/business-appointment-operations";
import type { BusinessAppointment } from "@/features/business/api/get-business-appointments";
import {
  AlertDialog,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle
} from "@/components/ui/alert-dialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle
} from "@/components/ui/card";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from "@/components/ui/dialog";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger
} from "@/components/ui/dropdown-menu";
import { Label } from "@/components/ui/label";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle
} from "@/components/ui/sheet";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow
} from "@/components/ui/table";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Textarea } from "@/components/ui/textarea";
import { useIsMobile } from "@/shared/hooks/use-mobile";
import {
  formatBranchDateLabel,
  formatBranchTimeLabel,
  parseBranchDateTimeLocalValue,
  toBranchDateTimeLocalValue
} from "@/shared/lib/date-time";
import { createWebIdempotencyKey } from "@/shared/lib/idempotency";

/**
 * /panel/randevular -- kesinlesmis randevularin LISTE gorunumu.
 *
 * Takvim (/panel/takvim) ayni veriyi zaman izgarasi olarak cizer; burasi mobilde takvimin
 * yerine gecen, operasyona odakli gorunumdur. Alti operasyon da (tamamla / iptal / gelmedi /
 * not / yeniden planla) ORTAK business-appointment-operations modulu uzerinden kosar --
 * cagri mantigi bu dosyada TEKRARLANMAZ.
 *
 * SAAT DILIMI: her satir kendi SUBESININ saat diliminde yazilir. Tarayici saatine cevirmek
 * sessiz bir yanlistir; sube baska sehirde olabilir.
 */

type StatusTab = "yaklasan" | "tamamlanan" | "kapanan";

type StatusTabDefinition = {
  description: string;
  label: string;
  matches: (status: string) => boolean;
  value: StatusTab;
};

// Varsayilan sekme AYRI bir sabit: dizinin ilk elemanina indeksle erismek
// (noUncheckedIndexedAccess altinda) gereksiz bir "undefined olabilir" dali acar.
const upcomingStatusTab: StatusTabDefinition = {
  description: "Onaylanmış, henüz kapanmamış randevular.",
  label: "Yaklaşan",
  matches: (status) => status === "Confirmed",
  value: "yaklasan"
};

const statusTabs: ReadonlyArray<StatusTabDefinition> = [
  upcomingStatusTab,
  {
    description: "Hizmeti verilmiş ve tamamlandı olarak işaretlenmiş randevular.",
    label: "Tamamlanan",
    matches: (status) => status === "Completed",
    value: "tamamlanan"
  },
  {
    description:
      "İptal edilen, müşterinin gelmediği veya yeni bir saate taşınan randevular.",
    label: "İptal/Gelmedi",
    matches: (status) =>
      status === "Cancelled" || status === "NoShow" || status === "Rebooked",
    value: "kapanan"
  }
];

/** Statu rozeti: renk TEK sinyal degildir, Turkce metin her zaman yazilir. */
const statusBadges: Record<
  string,
  { label: string; variant: "default" | "secondary" | "destructive" | "outline" }
> = {
  Cancelled: { label: "İptal edildi", variant: "destructive" },
  Completed: { label: "Tamamlandı", variant: "secondary" },
  Confirmed: { label: "Onaylı", variant: "default" },
  NoShow: { label: "Gelmedi", variant: "destructive" },
  Rebooked: { label: "Yeniden planlandı", variant: "outline" }
};

type OperationDraft = {
  appointment: BusinessAppointment;
  endLocalValue: string;
  idempotencyKey: string;
  kind: AppointmentOperationKind;
  startLocalValue: string;
  text: string;
};

type BusinessAppointmentListPageProps = {
  appointments: ReadonlyArray<BusinessAppointment>;
  tenantId: string | null;
};

export function BusinessAppointmentListPage(
  props: BusinessAppointmentListPageProps
) {
  // useSearchParams Suspense sinirinda okunur (PanelShell ile ayni desen).
  return (
    <Suspense>
      <BusinessAppointmentListPageInner {...props} />
    </Suspense>
  );
}

function BusinessAppointmentListPageInner({
  appointments,
  tenantId
}: BusinessAppointmentListPageProps) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const isMobile = useIsMobile();

  const [actingAppointmentId, setActingAppointmentId] = useState<string | null>(
    null
  );
  const [draft, setDraft] = useState<OperationDraft | null>(null);
  // Backend yanitini bekleyip sayfa yenilenene kadar satirin bayat gorunmemesi icin.
  const [statusOverrides, setStatusOverrides] = useState<Record<string, string>>(
    {}
  );

  // Sekme URL'de tutulur: tablet kullanicisi sekmeyi paylasabilir/geri tusunu kullanabilir.
  const tab = readStatusTab(searchParams?.get("durum"));
  const activeTab = tab.value;

  const rows = useMemo(
    () =>
      appointments.map((appointment) => {
        const override = appointment.appointmentId
          ? statusOverrides[appointment.appointmentId]
          : undefined;

        return override ? { ...appointment, status: override } : appointment;
      }),
    [appointments, statusOverrides]
  );

  const countByTab = useMemo(() => {
    const counts: Record<StatusTab, number> = {
      kapanan: 0,
      tamamlanan: 0,
      yaklasan: 0
    };

    for (const appointment of rows) {
      const status = getAppointmentStatus(appointment);
      const tab = statusTabs.find((candidate) => candidate.matches(status));

      if (tab) {
        counts[tab.value] += 1;
      }
    }

    return counts;
  }, [rows]);

  const visibleRows = useMemo(
    () => rows.filter((row) => tab.matches(getAppointmentStatus(row))),
    [rows, tab]
  );

  function changeTab(next: string) {
    const params = new URLSearchParams(searchParams?.toString() ?? "");
    params.set("durum", next);
    router.replace(`?${params.toString()}`, { scroll: false });
  }

  function openOperation(
    appointment: BusinessAppointment,
    kind: AppointmentOperationKind
  ) {
    if (!tenantId || !appointment.appointmentId) {
      toast.error("İşlem için yetkili işletme ve randevu bilgisi doğrulanmalı.");
      return;
    }

    setDraft({
      appointment,
      // Yeniden planlama kutulari SUBE saatiyle doldurulur, tarayici saatiyle degil.
      endLocalValue: toBranchDateTimeLocalValue(
        appointment.endUtc,
        appointment.branchTimeZoneId
      ),
      idempotencyKey: createWebIdempotencyKey(`appointment-${kind}`),
      kind,
      startLocalValue: toBranchDateTimeLocalValue(
        appointment.startUtc,
        appointment.branchTimeZoneId
      ),
      text: kind === "note" ? appointment.businessNote ?? "" : ""
    });
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
      router.refresh();
    } finally {
      setActingAppointmentId(null);
    }
  }

  return (
    <div className="space-y-6">
      <header className="space-y-2">
        <h1 className="text-2xl font-semibold tracking-tight sm:text-3xl">
          Randevular
        </h1>
        <p className="text-sm text-muted-foreground">
          Kesinleşmiş randevuların listesi. Saatler her randevunun kendi{" "}
          <strong className="font-medium text-foreground">şube saatine</strong> göre
          yazılır — cihazının saatine çevrilmez.
        </p>
      </header>

      <Tabs onValueChange={changeTab} value={activeTab}>
        <TabsList className="h-auto w-full flex-wrap justify-start sm:w-fit">
          {statusTabs.map((candidate) => (
            <TabsTrigger
              // min-h-11 = 44px dokunma hedefi (birincil cihaz: resepsiyon tableti).
              className="min-h-11 flex-1 px-4 sm:flex-none"
              key={candidate.value}
              value={candidate.value}
            >
              {candidate.label} ({countByTab[candidate.value]})
            </TabsTrigger>
          ))}
        </TabsList>
      </Tabs>

      <p className="text-sm text-muted-foreground">{tab.description}</p>

      {visibleRows.length === 0 ? (
        <Card>
          <CardHeader>
            <CardTitle>Bu sekmede randevu yok</CardTitle>
            <CardDescription>
              Başka bir durum sekmesine geçerek diğer randevuları görebilirsin.
            </CardDescription>
          </CardHeader>
        </Card>
      ) : (
        <>
          {/* Masaustu/tablet: tablo. Mobilde tabloyu yatay kaydirtmak yerine karta duseriz. */}
          <Card className="hidden py-0 md:block">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Tarih ve saat</TableHead>
                  <TableHead>Müşteri</TableHead>
                  <TableHead>Hizmet</TableHead>
                  <TableHead>Personel</TableHead>
                  <TableHead>Durum</TableHead>
                  <TableHead className="text-right">İşlem</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {visibleRows.map((appointment, index) => (
                  <TableRow key={rowKey(appointment, index)}>
                    <TableCell className="align-top">
                      <AppointmentWhen appointment={appointment} />
                    </TableCell>
                    <TableCell className="align-top">
                      <AppointmentCustomer appointment={appointment} />
                    </TableCell>
                    <TableCell className="align-top">
                      <AppointmentService appointment={appointment} />
                    </TableCell>
                    <TableCell className="align-top text-sm">
                      {appointment.staffMemberDisplayName ?? "Personel atanmamış"}
                    </TableCell>
                    <TableCell className="align-top">
                      <AppointmentStatusBadge appointment={appointment} />
                    </TableCell>
                    <TableCell className="align-top text-right">
                      <AppointmentActions
                        appointment={appointment}
                        isSubmitting={
                          actingAppointmentId === appointment.appointmentId
                        }
                        onSelect={openOperation}
                      />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Card>

          {/* <768px: kart listesi. */}
          <div className="grid gap-3 md:hidden">
            {visibleRows.map((appointment, index) => (
              <Card key={rowKey(appointment, index)}>
                <CardHeader>
                  <div className="flex items-start justify-between gap-3">
                    <AppointmentWhen appointment={appointment} />
                    <AppointmentStatusBadge appointment={appointment} />
                  </div>
                </CardHeader>
                <CardContent className="space-y-3">
                  <AppointmentService appointment={appointment} />
                  <div className="text-sm">
                    <p className="text-muted-foreground">Personel</p>
                    <p>{appointment.staffMemberDisplayName ?? "Personel atanmamış"}</p>
                  </div>
                  <div className="text-sm">
                    <p className="text-muted-foreground">Müşteri</p>
                    <AppointmentCustomer appointment={appointment} />
                  </div>
                  {appointment.businessNote ? (
                    <p className="rounded-md bg-muted px-3 py-2 text-sm">
                      <span className="font-medium">Not: </span>
                      {appointment.businessNote}
                    </p>
                  ) : null}
                  <AppointmentActions
                    appointment={appointment}
                    className="w-full"
                    isSubmitting={actingAppointmentId === appointment.appointmentId}
                    onSelect={openOperation}
                  />
                </CardContent>
              </Card>
            ))}
          </div>
        </>
      )}

      {draft ? (
        <OperationSurface
          draft={draft}
          isMobile={isMobile}
          isSubmitting={actingAppointmentId === draft.appointment.appointmentId}
          onClose={() => setDraft(null)}
          onDraftChange={(patch) =>
            setDraft((current) => (current ? { ...current, ...patch } : current))
          }
          onSubmit={() => void submitOperation()}
        />
      ) : null}
    </div>
  );
}

/**
 * Operasyon yuzeyi.
 *
 * - YIKICI (iptal / gelmedi): AlertDialog -- geri alinamaz, onay ister.
 * - Digerleri: masaustunde Dialog, mobilde alttan Sheet (basparmakla ulasilabilir).
 */
function OperationSurface({
  draft,
  isMobile,
  isSubmitting,
  onClose,
  onDraftChange,
  onSubmit
}: {
  draft: OperationDraft;
  isMobile: boolean;
  isSubmitting: boolean;
  onClose: () => void;
  onDraftChange: (patch: Partial<OperationDraft>) => void;
  onSubmit: () => void;
}) {
  const details = getOperationDetails(draft.kind);
  const description = `${getServiceSummary(draft.appointment)} · ${formatWindow(
    draft.appointment
  )}`;

  const body = (
    <div className="space-y-4">
      {operationNeedsTimeRange(draft.kind) ? (
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="appointment-start">Başlangıç (şube saati)</Label>
            <input
              className="flex min-h-11 w-full rounded-md border bg-background px-3 py-2 text-sm shadow-xs outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50"
              id="appointment-start"
              onChange={(event) =>
                onDraftChange({ startLocalValue: event.target.value })
              }
              type="datetime-local"
              value={draft.startLocalValue}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="appointment-end">Bitiş (şube saati)</Label>
            <input
              className="flex min-h-11 w-full rounded-md border bg-background px-3 py-2 text-sm shadow-xs outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50"
              id="appointment-end"
              onChange={(event) =>
                onDraftChange({ endLocalValue: event.target.value })
              }
              type="datetime-local"
              value={draft.endLocalValue}
            />
          </div>
          {/* Girilen saatin hangi sube saatine denk geldigi GORUNUR yazilir. */}
          <p className="rounded-md bg-muted px-3 py-2 text-xs sm:col-span-2">
            Şube saati önizleme:{" "}
            <span className="font-medium">{formatDraftPreview(draft)}</span>
          </p>
        </div>
      ) : null}

      <div className="space-y-2">
        <Label htmlFor="appointment-text">{details.textareaLabel}</Label>
        <Textarea
          id="appointment-text"
          maxLength={details.maxLength}
          onChange={(event) => onDraftChange({ text: event.target.value })}
          placeholder={details.placeholder}
          rows={4}
          value={draft.text}
        />
        <p className="text-xs text-muted-foreground">{details.helper}</p>
      </div>
    </div>
  );

  if (operationIsDestructive(draft.kind)) {
    return (
      <AlertDialog onOpenChange={(open) => (open ? undefined : onClose())} open>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{details.title}</AlertDialogTitle>
            <AlertDialogDescription>
              {description} — bu işlem geri alınamaz.
            </AlertDialogDescription>
          </AlertDialogHeader>
          {body}
          <AlertDialogFooter>
            <AlertDialogCancel className="min-h-11" disabled={isSubmitting}>
              Vazgeç
            </AlertDialogCancel>
            <Button
              className="min-h-11"
              disabled={isSubmitting}
              onClick={onSubmit}
              variant="destructive"
            >
              {isSubmitting ? "İşleniyor" : details.submitLabel}
            </Button>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    );
  }

  const footer = (
    <>
      <Button
        className="min-h-11"
        disabled={isSubmitting}
        onClick={onClose}
        variant="outline"
      >
        Vazgeç
      </Button>
      <Button className="min-h-11" disabled={isSubmitting} onClick={onSubmit}>
        {isSubmitting ? "İşleniyor" : details.submitLabel}
      </Button>
    </>
  );

  if (isMobile) {
    return (
      <Sheet onOpenChange={(open) => (open ? undefined : onClose())} open>
        <SheetContent className="max-h-[90vh] overflow-y-auto" side="bottom">
          <SheetHeader>
            <SheetTitle>{details.title}</SheetTitle>
            <SheetDescription>{description}</SheetDescription>
          </SheetHeader>
          <div className="px-4">{body}</div>
          <SheetFooter>{footer}</SheetFooter>
        </SheetContent>
      </Sheet>
    );
  }

  return (
    <Dialog onOpenChange={(open) => (open ? undefined : onClose())} open>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{details.title}</DialogTitle>
          <DialogDescription>{description}</DialogDescription>
        </DialogHeader>
        {body}
        <DialogFooter>{footer}</DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function AppointmentActions({
  appointment,
  className,
  isSubmitting,
  onSelect
}: {
  appointment: BusinessAppointment;
  className?: string;
  isSubmitting: boolean;
  onSelect: (
    appointment: BusinessAppointment,
    kind: AppointmentOperationKind
  ) => void;
}) {
  const isConfirmed = getAppointmentStatus(appointment) === "Confirmed";
  const completeIsOpen = canCompleteAppointment(appointment);
  const noShowIsOpen = canMarkAppointmentNoShow(appointment);

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button className={`min-h-11 ${className ?? ""}`} variant="outline">
          İşlem
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-64">
        {isConfirmed ? (
          <>
            {/*
             * Kapali aksiyon SILINMEZ, DISABLED birakilir ve NEDENI ETIKETE YAZILIR.
             * Tooltip kullanilamaz: dokunmatik cihazda tooltip yoktur.
             */}
            <DropdownMenuItem
              className="min-h-11"
              disabled={isSubmitting || !completeIsOpen}
              onSelect={() => onSelect(appointment, "complete")}
            >
              {completeIsOpen
                ? "Tamamla"
                : "Tamamla — bitiş saatinden sonra açılır"}
            </DropdownMenuItem>
            <DropdownMenuItem
              className="min-h-11"
              disabled={isSubmitting || !noShowIsOpen}
              onSelect={() => onSelect(appointment, "no-show")}
            >
              {noShowIsOpen
                ? "Gelmedi olarak işaretle"
                : "Gelmedi — başlangıç saatinden sonra açılır"}
            </DropdownMenuItem>
            <DropdownMenuItem
              className="min-h-11"
              disabled={isSubmitting}
              onSelect={() => onSelect(appointment, "rebook")}
            >
              Yeniden planla
            </DropdownMenuItem>
            <DropdownMenuItem
              className="min-h-11"
              disabled={isSubmitting}
              onSelect={() => onSelect(appointment, "cancel")}
              variant="destructive"
            >
              İptal et
            </DropdownMenuItem>
          </>
        ) : null}
        {/* Not, kapanmis randevularda da yazilabilir (operasyon gecmisi). */}
        <DropdownMenuItem
          className="min-h-11"
          disabled={isSubmitting}
          onSelect={() => onSelect(appointment, "note")}
        >
          {appointment.businessNote ? "Notu düzenle" : "Not ekle"}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

function AppointmentWhen({ appointment }: { appointment: BusinessAppointment }) {
  return (
    <div className="text-sm">
      <p className="font-medium">{formatWindow(appointment)}</p>
      {/* Sube adi GORUNUR: hangi sube hangi saat diliminde, kullanici bilmeli. */}
      <p className="text-muted-foreground">
        {appointment.branchDisplayName ?? "Şube adı yok"}
        {appointment.branchTimeZoneId ? ` · ${appointment.branchTimeZoneId}` : ""}
      </p>
    </div>
  );
}

/** Musteri bilgisi backend'de ZATEN maskeli doner; "tam bilgiyi goster" ucu YOKTUR. */
function AppointmentCustomer({
  appointment
}: {
  appointment: BusinessAppointment;
}) {
  const customer = appointment.customer;

  return (
    <div className="text-sm">
      <p>{customer?.maskedPhone ?? "Telefon yok"}</p>
      <p className="text-muted-foreground">
        {customer?.maskedEmail ?? "E-posta yok"}
      </p>
    </div>
  );
}

function AppointmentService({
  appointment
}: {
  appointment: BusinessAppointment;
}) {
  const lines = appointment.lines ?? [];

  return (
    <div className="text-sm">
      <p className="font-medium">{getServiceSummary(appointment)}</p>
      <p className="text-muted-foreground">
        {getDurationMinutes(appointment)} dk
        {lines.length > 1 ? ` · ${lines.length} hizmet` : ""}
      </p>
    </div>
  );
}

function AppointmentStatusBadge({
  appointment
}: {
  appointment: BusinessAppointment;
}) {
  const status = getAppointmentStatus(appointment);
  const badge = statusBadges[status];

  // Bilinmeyen statu de METIN olarak yazilir; sessizce bos rozet cizilmez.
  return (
    <Badge variant={badge?.variant ?? "outline"}>{badge?.label ?? status}</Badge>
  );
}

/** Bilinmeyen/eksik ?durum= degeri sessizce "Yaklasan" sekmesine duser. */
function readStatusTab(value: string | null | undefined): StatusTabDefinition {
  return (
    statusTabs.find((candidate) => candidate.value === value) ?? upcomingStatusTab
  );
}

function rowKey(appointment: BusinessAppointment, index: number): string {
  return appointment.appointmentId ?? `${appointment.branchId ?? "sube"}-${index}`;
}

function getServiceSummary(appointment: BusinessAppointment): string {
  const lines = appointment.lines ?? [];
  const firstLine = lines.at(0);
  const firstService = firstLine?.serviceNameSnapshot ?? "Hizmet detayı yok";

  if (lines.length <= 1) {
    return firstService;
  }

  return `${firstService} + ${lines.length - 1} hizmet`;
}

function getDurationMinutes(appointment: BusinessAppointment): number {
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
function formatWindow(appointment: BusinessAppointment): string {
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

function formatDraftPreview(draft: OperationDraft): string {
  const branchTimeZoneId = draft.appointment.branchTimeZoneId;
  const startUtc = parseBranchDateTimeLocalValue(
    draft.startLocalValue,
    branchTimeZoneId
  );
  const endUtc = parseBranchDateTimeLocalValue(
    draft.endLocalValue,
    branchTimeZoneId
  );

  if (!startUtc || !endUtc) {
    return "Geçerli şube zamanı gir.";
  }

  if (!branchTimeZoneId) {
    return `${startUtc} - ${endUtc} (UTC)`;
  }

  return `${formatBranchDateLabel(
    startUtc,
    branchTimeZoneId
  )} · ${formatBranchTimeLabel(startUtc, branchTimeZoneId)} - ${formatBranchTimeLabel(
    endUtc,
    branchTimeZoneId
  )}`;
}
