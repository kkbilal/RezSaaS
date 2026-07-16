"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { Suspense, useMemo } from "react";
import {
  describeAppointmentActions,
  getAppointmentStatus,
  type AppointmentOperationKind
} from "@/features/business/api/business-appointment-operations";
import type { BusinessAppointment } from "@/features/business/api/get-business-appointments";
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
  CardDescription,
  CardHeader,
  CardTitle
} from "@/components/ui/card";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger
} from "@/components/ui/dropdown-menu";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow
} from "@/components/ui/table";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { useIsMobile } from "@/shared/hooks/use-mobile";

/**
 * /panel/randevular -- kesinlesmis randevularin LISTE gorunumu.
 *
 * Takvim (/panel/takvim) ayni veriyi zaman izgarasi olarak cizer; burasi mobilde takvimin
 * yerine gecen, operasyona odakli gorunumdur. Alti operasyon da (tamamla / iptal / gelmedi /
 * not / yeniden planla) ORTAK useAppointmentOperations + OperationSurface uzerinden kosar --
 * cagri mantigi ve diyalog bu dosyada TEKRARLANMAZ.
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

  // Randevu operasyonlari (taslak/gonderim/statu override) ortak kancada.
  const ops = useAppointmentOperations(tenantId);

  // Sekme URL'de tutulur: tablet kullanicisi sekmeyi paylasabilir/geri tusunu kullanabilir.
  const tab = readStatusTab(searchParams?.get("durum"));
  const activeTab = tab.value;

  const rows = useMemo(
    () =>
      appointments.map((appointment) => {
        const override = appointment.appointmentId
          ? ops.statusOverrides[appointment.appointmentId]
          : undefined;

        return override ? { ...appointment, status: override } : appointment;
      }),
    [appointments, ops.statusOverrides]
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
                          ops.actingAppointmentId === appointment.appointmentId
                        }
                        onSelect={ops.openOperation}
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
                    isSubmitting={ops.actingAppointmentId === appointment.appointmentId}
                    onSelect={ops.openOperation}
                  />
                </CardContent>
              </Card>
            ))}
          </div>
        </>
      )}

      {ops.draft ? (
        <OperationSurface
          draft={ops.draft}
          isMobile={isMobile}
          isSubmitting={ops.actingAppointmentId === ops.draft.appointment.appointmentId}
          onClose={ops.closeDraft}
          onDraftChange={ops.updateDraft}
          onSubmit={() => void ops.submitOperation()}
        />
      ) : null}
    </div>
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
  // Kapali aksiyon SILINMEZ, DISABLED birakilir ve NEDENI ETIKETE YAZILIR (describeAppointmentActions).
  // Tooltip kullanilamaz: dokunmatik cihazda tooltip yoktur.
  const actions = describeAppointmentActions(appointment, isSubmitting);

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button className={`min-h-11 ${className ?? ""}`} variant="outline">
          İşlem
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-64">
        {actions.map((action) => (
          <DropdownMenuItem
            className="min-h-11"
            disabled={action.disabled}
            key={action.kind}
            onSelect={() => onSelect(appointment, action.kind)}
            variant={action.destructive ? "destructive" : "default"}
          >
            {action.label}
          </DropdownMenuItem>
        ))}
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
