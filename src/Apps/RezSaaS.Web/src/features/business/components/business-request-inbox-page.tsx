"use client";

import { useRouter } from "next/navigation";
import { useMemo, useRef, useState, useSyncExternalStore } from "react";
import { toast } from "sonner";
import type {
  BusinessAppointmentInboxState,
  BusinessAppointmentRequest
} from "@/features/business/api/get-appointment-inbox";
import {
  hasPendingApprovalConflict,
  shouldOptimisticallySupersede
} from "@/features/business/lib/business-request-conflicts";
import {
  getRequestTtlStatus,
  isRequestUrgent,
  type RequestTtlStatus
} from "@/features/business/lib/request-ttl";
import { createTenantApiClient } from "@/shared/api/client";
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
  DialogHeader,
  DialogTitle
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue
} from "@/components/ui/select";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle
} from "@/components/ui/sheet";
import { Textarea } from "@/components/ui/textarea";
import { useIsMobile } from "@/shared/hooks/use-mobile";
import { cn } from "@/shared/lib/cn";
import { formatBranchDateTime } from "@/shared/lib/date-time";
import {
  clearIntentIdempotencyKey,
  getOrCreateIntentIdempotencyKey,
  type IdempotencyKeyCache
} from "@/shared/lib/idempotency";

type AppointmentRequestStatus =
  | "PendingApproval"
  | "Approved"
  | "Declined"
  | "Expired"
  | "Superseded"
  | "CancelledByCustomer";

type AppointmentFilter = "all" | "urgent" | AppointmentRequestStatus;
type AppointmentDecision = "approve" | "decline";
type AbuseReportReasonCode =
  | "SlotSpam"
  | "RepeatedCancellation"
  | "NoShowPattern"
  | "SuspectedAutomation"
  | "AbusiveBehavior"
  | "Other";

type AbuseReportDraft = {
  note: string;
  reasonCode: AbuseReportReasonCode;
  request: BusinessAppointmentRequest;
};

/** Bos olamaz: her grup en az kendi talebini icerir. Tuple, group[0] icin guard gerektirmez. */
type RequestGroup = [BusinessAppointmentRequest, ...BusinessAppointmentRequest[]];

const clockTickMs = 5 * 1000;

/**
 * Duvar saati REACT DISI bir kaynaktir; state degil, ABONELIK olarak okunur.
 *
 * Iki kisit var:
 *  - Snapshot 5 sn'lik kovaya yuvarlanir. Her cagrida ham Date.now() donseydi React
 *    "getSnapshot should be cached" ile sonsuz render'a girerdi.
 *  - Sunucu snapshot'i NULL. Sunucuda "kalan sure" hesaplanip HTML'e yazilirsa, istemci
 *    hydration'da baska bir sure hesaplar ve uyusmazlik olur. Geri sayim mount sonrasi baslar.
 */
function useNowUtc(): string | null {
  return useSyncExternalStore(
    subscribeToClock,
    getClockSnapshot,
    getServerClockSnapshot
  );
}

function subscribeToClock(onStoreChange: () => void) {
  const intervalId = window.setInterval(onStoreChange, clockTickMs);
  return () => window.clearInterval(intervalId);
}

function getClockSnapshot() {
  return new Date(
    Math.floor(Date.now() / clockTickMs) * clockTickMs
  ).toISOString();
}

function getServerClockSnapshot(): string | null {
  return null;
}

const abuseReportNoteMaxLength = 300;

const abuseReportReasonOptions: Array<{
  description: string;
  label: string;
  value: AbuseReportReasonCode;
}> = [
  {
    description: "Aynı işletme veya saat aralığına kısa sürede yoğun talep.",
    label: "Slot spam",
    value: "SlotSpam"
  },
  {
    description: "Tekrarlayan iptal davranışı operasyonu bozuyor.",
    label: "Tekrarlayan iptal",
    value: "RepeatedCancellation"
  },
  {
    description: "Gelmemeyi alışkanlık haline getiren müşteri sinyali.",
    label: "No-show örüntüsü",
    value: "NoShowPattern"
  },
  {
    description: "Otomasyon, bot veya gerçek dışı kullanım şüphesi.",
    label: "Otomasyon şüphesi",
    value: "SuspectedAutomation"
  },
  {
    description: "İşletme veya personele yönelik uygunsuz davranış.",
    label: "Uygunsuz davranış",
    value: "AbusiveBehavior"
  },
  {
    description: "Listede olmayan ama platform incelemesi gerektiren durum.",
    label: "Diğer",
    value: "Other"
  }
];

/** Durum -> salon sahibinin dilinde etiket. Renk TEK sinyal olmaz; metin her zaman yazilir. */
const statusCopy: Record<string, string> = {
  Approved: "Onaylandı",
  CancelledByCustomer: "Müşteri iptal etti",
  Declined: "Reddedildi",
  Expired: "Süresi doldu",
  PendingApproval: "Onay bekliyor",
  Superseded: "Başka talep seçildi"
};

const statusStyles: Record<string, string> = {
  Approved:
    "border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950 dark:text-emerald-300",
  CancelledByCustomer:
    "border-border bg-muted text-muted-foreground",
  Declined:
    "border-rose-200 bg-rose-50 text-rose-700 dark:border-rose-900 dark:bg-rose-950 dark:text-rose-300",
  Expired: "border-border bg-muted text-muted-foreground",
  PendingApproval:
    "border-amber-200 bg-amber-50 text-amber-800 dark:border-amber-900 dark:bg-amber-950 dark:text-amber-300",
  Superseded: "border-border bg-muted text-muted-foreground"
};

type BusinessRequestInboxPageProps = {
  inbox: BusinessAppointmentInboxState;
  tenantId: string | null;
};

export function BusinessRequestInboxPage({
  inbox,
  tenantId
}: BusinessRequestInboxPageProps) {
  const router = useRouter();
  const inboxRequests = useMemo(
    () => (inbox.kind === "ready" ? inbox.requests : []),
    [inbox]
  );

  const nowUtc = useNowUtc();
  const [statusOverrides, setStatusOverrides] = useState<Record<string, string>>(
    {}
  );
  const [activeFilter, setActiveFilter] = useState<AppointmentFilter>("all");
  const [search, setSearch] = useState("");
  const [conflictCandidate, setConflictCandidate] =
    useState<BusinessAppointmentRequest | null>(null);
  const [reportDraft, setReportDraft] = useState<AbuseReportDraft | null>(null);
  const [actingRequestId, setActingRequestId] = useState<string | null>(null);
  const [reportingRequestId, setReportingRequestId] = useState<string | null>(
    null
  );
  const [reportedRequestIds, setReportedRequestIds] = useState<Set<string>>(
    () => new Set()
  );
  const decisionIdempotencyKeys = useRef<IdempotencyKeyCache>({});

  const requests = useMemo(
    () =>
      inboxRequests.map((request) => {
        const override =
          request.id === undefined ? undefined : statusOverrides[request.id];

        return override
          ? {
              ...request,
              status: override
            }
          : request;
      }),
    [inboxRequests, statusOverrides]
  );

  const pendingCount = requests.filter(
    (request) => getRequestStatus(request) === "PendingApproval"
  ).length;

  const urgentCount = useMemo(
    () =>
      requests.filter(
        (request) =>
          getRequestStatus(request) === "PendingApproval" &&
          isRequestUrgent(request.expiresAtUtc, nowUtc ?? "")
      ).length,
    [requests, nowUtc]
  );

  const visibleRequests = useMemo(() => {
    const needle = search.trim().toLocaleLowerCase("tr-TR");

    return requests.filter((request) => {
      const requestStatus = getRequestStatus(request);
      const matchesFilter =
        activeFilter === "all"
          ? true
          : activeFilter === "urgent"
            ? requestStatus === "PendingApproval" &&
              isRequestUrgent(request.expiresAtUtc, nowUtc ?? "")
            : requestStatus === activeFilter;
      const haystack = [
        request.id,
        request.branchDisplayName,
        getCustomerHandle(request),
        getServiceSummary(request),
        request.staffMemberDisplayName,
        request.resourceDisplayName
      ]
        .join(" ")
        .toLocaleLowerCase("tr-TR");

      return matchesFilter && haystack.includes(needle);
    });
  }, [activeFilter, nowUtc, requests, search]);

  /*
   * Cakisan talepler YAN YANA cizilir. Sebep: PendingApproval bir talep KOLTUGU BLOKLAMAZ --
   * ayni saate birden fazla talep dusebilir ve isletme BIRINI secer. Listeyi duz bir akis
   * olarak cizersek isletme rakip talepleri hic gormeden onaylar; sonra "digerleri neden
   * dustu?" sorusu cikar. Gruplama sadece GORSEL -- karar mantigi degismez.
   */
  const groups = useMemo(
    () => groupConflictingRequests(visibleRequests),
    [visibleRequests]
  );

  /** Bu talep onaylanirsa DUSECEK olan rakipler. Onay diyaloğu bu listeyi aynen gosterir. */
  function getSupersededRivals(request: BusinessAppointmentRequest) {
    return requests.filter(
      (candidate) =>
        candidate.id !== undefined &&
        candidate.id !== request.id &&
        shouldOptimisticallySupersede(request, candidate)
    );
  }

  async function approveRequest(
    request: BusinessAppointmentRequest,
    forceDecision: boolean = false
  ) {
    if (!forceDecision && hasPendingApprovalConflict(request, requests)) {
      setConflictCandidate(request);
      return;
    }

    await decideRequest(request, "approve");
  }

  async function declineRequest(request: BusinessAppointmentRequest) {
    await decideRequest(request, "decline");
  }

  function openAbuseReport(request: BusinessAppointmentRequest) {
    if (!tenantId || !request.id) {
      toast.error("Rapor için yetkili işletme ve talep bilgisi doğrulanmalı.");
      return;
    }

    if (reportedRequestIds.has(request.id)) {
      toast.message("Bu talep için rapor zaten gönderildi.");
      return;
    }

    setReportDraft({
      note: "",
      reasonCode: "SlotSpam",
      request
    });
  }

  async function submitAbuseReport() {
    if (!reportDraft) {
      return;
    }

    const appointmentRequestId = reportDraft.request.id;
    const note = reportDraft.note.trim();

    if (!tenantId || !appointmentRequestId) {
      toast.error("Rapor için yetkili işletme ve talep bilgisi doğrulanmalı.");
      return;
    }

    if (note.length > abuseReportNoteMaxLength) {
      toast.error("Rapor notu 300 karakteri aşamaz.");
      return;
    }

    const client = createTenantApiClient(tenantId);
    setReportingRequestId(appointmentRequestId);

    try {
      const result = await client.POST(
        "/api/business/appointment-requests/{appointmentRequestId}/abuse-reports",
        {
          body: {
            note: note.length > 0 ? note : null,
            reasonCode: reportDraft.reasonCode
          },
          params: {
            path: {
              appointmentRequestId
            }
          }
        }
      );

      if (!result.response.ok || !result.data) {
        toast.error(getAbuseReportErrorCopy(result.response.status));
        return;
      }

      setReportedRequestIds((current) => {
        const next = new Set(current);
        next.add(appointmentRequestId);
        return next;
      });
      setReportDraft(null);
      toast.success(
        result.response.status === 201
          ? "Suistimal bildirimi platform inceleme kuyruğuna alındı."
          : "Bu talep için mevcut bildirim tekrarlandı."
      );
      router.refresh();
    } catch {
      toast.error("Bildirim şu anda gönderilemedi. Lütfen tekrar dene.");
    } finally {
      setReportingRequestId(null);
    }
  }

  async function decideRequest(
    request: BusinessAppointmentRequest,
    decision: AppointmentDecision
  ) {
    const appointmentRequestId = request.id;

    if (!tenantId || !appointmentRequestId) {
      toast.error("İşlem için yetkili işletme ve talep bilgisi doğrulanmalı.");
      return;
    }

    /*
     * Idempotency-Key intent basina URETILIR ve sonuc alinana kadar KORUNUR.
     * Tablette cift dokunma / aginin kopup geri gelmesi ayni talebi iki kez
     * onaylatmamali: backend ayni anahtari gorunce ayni sonucu doner.
     */
    const intentId = `${decision}:${appointmentRequestId}`;
    const idempotencyKey = getOrCreateIntentIdempotencyKey(
      decisionIdempotencyKeys.current,
      intentId,
      `business-${decision}`
    );
    const client = createTenantApiClient(tenantId);
    setActingRequestId(appointmentRequestId);

    try {
      const result =
        decision === "approve"
          ? await client.POST(
              "/api/business/appointment-requests/{appointmentRequestId}/approve",
              {
                params: {
                  header: {
                    "Idempotency-Key": idempotencyKey
                  },
                  path: {
                    appointmentRequestId
                  }
                }
              }
            )
          : await client.POST(
              "/api/business/appointment-requests/{appointmentRequestId}/decline",
              {
                params: {
                  header: {
                    "Idempotency-Key": idempotencyKey
                  },
                  path: {
                    appointmentRequestId
                  }
                }
              }
            );

      if (!result.response.ok) {
        toast.error(getDecisionErrorCopy(result.response.status));
        router.refresh();
        return;
      }

      const nextStatus =
        result.data?.status ??
        (decision === "approve" ? "Approved" : "Declined");
      applyDecisionResult(request, nextStatus);
      clearIntentIdempotencyKey(decisionIdempotencyKeys.current, intentId);
      setConflictCandidate(null);
      toast.success(
        decision === "approve"
          ? "Talep onaylandı; randevu oluşturuldu."
          : "Talep reddedildi."
      );
      router.refresh();
    } catch {
      toast.error("İşlem şu anda tamamlanamadı. Lütfen tekrar dene.");
    } finally {
      setActingRequestId(null);
    }
  }

  function applyDecisionResult(
    request: BusinessAppointmentRequest,
    nextStatus: string
  ) {
    const appointmentRequestId = request.id;

    if (!appointmentRequestId) {
      return;
    }

    setStatusOverrides((currentOverrides) => {
      const nextOverrides = {
        ...currentOverrides,
        [appointmentRequestId]: nextStatus
      };

      /*
       * Onaylanan talebin rakipleri backend'de Superseded'a duser. Sunucudan taze liste
       * gelene kadar ekranda hala "Onay bekliyor" gorunmeleri isletmeyi yaniltir --
       * ayni koltugu ikinci kez onaylamaya calisir ve 409 yer. Bu yuzden iyimser olarak
       * burada da dusuruyoruz; router.refresh() gercek durumu hemen ardindan getirir.
       */
      if (nextStatus === "Approved") {
        for (const currentRequest of requests) {
          if (
            currentRequest.id &&
            currentRequest.id !== appointmentRequestId &&
            shouldOptimisticallySupersede(request, currentRequest)
          ) {
            nextOverrides[currentRequest.id] = "Superseded";
          }
        }
      }

      return nextOverrides;
    });
  }

  const conflictRivals = conflictCandidate
    ? getSupersededRivals(conflictCandidate)
    : [];

  return (
    <div className="space-y-6">
      <header className="space-y-2">
        <h1 className="text-3xl font-semibold tracking-tight text-foreground sm:text-4xl">
          Talep kutusu
        </h1>
        <p className="max-w-3xl text-sm leading-6 text-muted-foreground">
          Müşterilerin gönderdiği randevu talepleri burada birikir. Onayladığın
          talep randevuya dönüşür; reddettiğin talep müşteriye kapanır.
        </p>
      </header>

      <SlotMechanicCard pendingCount={pendingCount} />

      <InboxToolbar
        activeFilter={activeFilter}
        onFilterChange={setActiveFilter}
        onSearchChange={setSearch}
        pendingCount={pendingCount}
        search={search}
        urgentCount={urgentCount}
      />

      {inbox.kind === "unavailable" ? (
        <Card className="border-amber-200 bg-amber-50 dark:border-amber-900 dark:bg-amber-950">
          <CardHeader>
            <CardTitle className="text-amber-900 dark:text-amber-200">
              Talepler yüklenemedi
            </CardTitle>
            <CardDescription className="text-amber-800 dark:text-amber-300">
              {inbox.reason} Lütfen kısa süre sonra tekrar dene.
            </CardDescription>
          </CardHeader>
        </Card>
      ) : null}

      {groups.length === 0 ? (
        <EmptyInbox hasFilter={activeFilter !== "all" || search.length > 0} />
      ) : (
        <div className="space-y-4">
          {groups.map((group) => {
            const [leadRequest] = group;
            const isConflictGroup = group.length > 1;

            if (!isConflictGroup) {
              const request = leadRequest;

              return (
                <RequestCard
                  isReported={
                    request.id ? reportedRequestIds.has(request.id) : false
                  }
                  isReporting={reportingRequestId === request.id}
                  isSubmitting={actingRequestId === request.id}
                  key={getRequestKey(request)}
                  nowUtc={nowUtc}
                  onApprove={() => void approveRequest(request)}
                  onDecline={() => void declineRequest(request)}
                  onReport={() => openAbuseReport(request)}
                  request={request}
                  rivalCount={getSupersededRivals(request).length}
                />
              );
            }

            return (
              <ConflictGroup count={group.length} key={getRequestKey(leadRequest)}>
                {group.map((request) => (
                  <RequestCard
                    isInConflictGroup
                    isReported={
                      request.id ? reportedRequestIds.has(request.id) : false
                    }
                    isReporting={reportingRequestId === request.id}
                    isSubmitting={actingRequestId === request.id}
                    key={getRequestKey(request)}
                    nowUtc={nowUtc}
                    onApprove={() => void approveRequest(request)}
                    onDecline={() => void declineRequest(request)}
                    onReport={() => openAbuseReport(request)}
                    request={request}
                    rivalCount={getSupersededRivals(request).length}
                  />
                ))}
              </ConflictGroup>
            );
          })}
        </div>
      )}

      <ConflictConfirmModal
        isSubmitting={
          conflictCandidate !== null && actingRequestId === conflictCandidate.id
        }
        onCancel={() => setConflictCandidate(null)}
        onConfirm={() => {
          if (conflictCandidate) {
            void approveRequest(conflictCandidate, true);
          }
        }}
        request={conflictCandidate}
        rivals={conflictRivals}
      />

      <AbuseReportModal
        draft={reportDraft}
        isSubmitting={
          reportDraft !== null && reportingRequestId === reportDraft.request.id
        }
        onCancel={() => setReportDraft(null)}
        onDraftChange={setReportDraft}
        onSubmit={() => void submitAbuseReport()}
      />
    </div>
  );
}

/**
 * Urunun en cok yanlis anlasilan mekanigi. Tooltip'e GIZLENMEZ -- dokunmatik cihazda
 * tooltip yoktur; kalici, gorunur metin olarak yazilir.
 */
function SlotMechanicCard({ pendingCount }: { pendingCount: number }) {
  return (
    <Card className="border-dashed">
      <CardHeader>
        <CardTitle className="text-base">
          Onay bekleyen talep koltuğu tutmaz
        </CardTitle>
        <CardDescription className="leading-6">
          Aynı saate birden fazla talep düşebilir; bekleyen talep o saati kimseye
          kapatmaz. Birini onayladığında aynı personel veya aynı koltuk için
          bekleyen diğer talepler otomatik olarak{" "}
          <strong className="font-medium text-foreground">
            &quot;Başka talep seçildi&quot;
          </strong>{" "}
          durumuna düşer. Çakışan talepler aşağıda yan yana gösterilir; onaydan
          önce hangilerinin düşeceğini de sorarız.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <p className="text-sm text-muted-foreground">
          Yanıtlanmayan talep{" "}
          <strong className="font-medium text-foreground">24 saat</strong> sonra
          kendiliğinden düşer. Her talebin kalan süresi kartında yazar.
          {pendingCount > 0
            ? ` Şu anda ${pendingCount} talep senin kararını bekliyor.`
            : ""}
        </p>
      </CardContent>
    </Card>
  );
}

function InboxToolbar({
  activeFilter,
  onFilterChange,
  onSearchChange,
  pendingCount,
  search,
  urgentCount
}: {
  activeFilter: AppointmentFilter;
  onFilterChange: (value: AppointmentFilter) => void;
  onSearchChange: (value: string) => void;
  pendingCount: number;
  search: string;
  urgentCount: number;
}) {
  const filters: Array<{ label: string; value: AppointmentFilter }> = [
    { label: "Hepsi", value: "all" },
    { label: `Onay bekleyen${pendingCount > 0 ? ` (${pendingCount})` : ""}`, value: "PendingApproval" },
    { label: "Onaylanan", value: "Approved" },
    { label: "Reddedilen", value: "Declined" },
    { label: "Süresi dolan", value: "Expired" },
    { label: "Başka talep seçilen", value: "Superseded" }
  ];

  return (
    <Card>
      <CardContent className="space-y-4">
        <Input
          aria-label="Talep, hizmet, personel veya koltuk ara"
          className="h-11 min-h-11"
          onChange={(event) => onSearchChange(event.target.value)}
          placeholder="Talep, hizmet, personel veya koltuk ara"
          type="search"
          value={search}
        />

        <div className="flex flex-wrap gap-2">
          {filters.map((filter) => (
            <Button
              aria-pressed={activeFilter === filter.value}
              className="min-h-11"
              key={filter.value}
              onClick={() => onFilterChange(filter.value)}
              size="sm"
              type="button"
              variant={activeFilter === filter.value ? "default" : "outline"}
            >
              {filter.label}
            </Button>
          ))}

          {urgentCount > 0 ? (
            <Button
              aria-pressed={activeFilter === "urgent"}
              className={cn(
                "min-h-11",
                activeFilter === "urgent"
                  ? undefined
                  : "border-rose-300 text-rose-700 dark:border-rose-800 dark:text-rose-300"
              )}
              onClick={() =>
                onFilterChange(activeFilter === "urgent" ? "all" : "urgent")
              }
              size="sm"
              type="button"
              variant={activeFilter === "urgent" ? "destructive" : "outline"}
            >
              Süresi bitmek üzere ({urgentCount})
            </Button>
          ) : null}
        </div>
      </CardContent>
    </Card>
  );
}

function ConflictGroup({
  children,
  count
}: {
  children: React.ReactNode;
  count: number;
}) {
  return (
    <section className="rounded-xl border border-amber-300 bg-amber-50/60 p-3 dark:border-amber-900 dark:bg-amber-950/40 sm:p-4">
      <div className="mb-3 space-y-1">
        <p className="text-sm font-semibold text-amber-900 dark:text-amber-200">
          Aynı saat için {count} talep yarışıyor
        </p>
        <p className="text-sm leading-6 text-amber-800 dark:text-amber-300">
          Bunlardan yalnızca birini onaylayabilirsin. Onayladığın anda diğerleri
          &quot;Başka talep seçildi&quot; durumuna düşer.
        </p>
      </div>
      {/* Yan yana: isletme rakip talepleri KARSILASTIRARAK secsin. <768px'te alt alta. */}
      <div className="grid gap-3 md:grid-cols-2">{children}</div>
    </section>
  );
}

function RequestCard({
  isInConflictGroup = false,
  isReported,
  isReporting,
  isSubmitting,
  nowUtc,
  onApprove,
  onDecline,
  onReport,
  request,
  rivalCount
}: {
  isInConflictGroup?: boolean;
  isReported: boolean;
  isReporting: boolean;
  isSubmitting: boolean;
  nowUtc: string | null;
  onApprove: () => void;
  onDecline: () => void;
  onReport: () => void;
  request: BusinessAppointmentRequest;
  rivalCount: number;
}) {
  const status = getRequestStatus(request);
  const isPending = status === "PendingApproval";
  const ttlStatus =
    isPending && nowUtc ? getRequestTtlStatus(request.expiresAtUtc, nowUtc) : null;
  const reportLabel = isReported
    ? "Bildirildi"
    : isReporting
      ? "Gönderiliyor"
      : "Suistimal bildir";

  return (
    <Card className={cn("gap-4", isInConflictGroup && "bg-card")}>
      <CardHeader className="gap-3">
        <div className="flex flex-wrap items-center gap-2">
          <RequestStatusBadge status={status} />
          {ttlStatus ? <TtlBadge status={ttlStatus} /> : null}
        </div>

        <CardTitle className="text-lg leading-tight">
          {getServiceSummary(request)}
        </CardTitle>
        <CardDescription className="leading-6">
          {formatRequestStart(request)} · {getDurationMinutes(request)} dk
        </CardDescription>
      </CardHeader>

      <CardContent className="space-y-4">
        <dl className="grid gap-x-4 gap-y-2 text-sm sm:grid-cols-2">
          <DetailRow label="Personel" value={request.staffMemberDisplayName ?? "Belirtilmemiş"} />
          <DetailRow label="Koltuk" value={request.resourceDisplayName ?? "Belirtilmemiş"} />
          <DetailRow label="Şube" value={request.branchDisplayName ?? "Belirtilmemiş"} />
          <DetailRow label="Müşteri" value={getCustomerHandle(request)} />
          <DetailRow label="Telefon" value={request.customer?.maskedPhone ?? "Paylaşılmadı"} />
          <DetailRow label="E-posta" value={request.customer?.maskedEmail ?? "Paylaşılmadı"} />
        </dl>

        <p className="text-xs leading-5 text-muted-foreground">
          Müşteri iletişim bilgisi yalnızca maskeli gösterilir; tam bilgi randevu
          onaylandıktan sonra da panelde açılmaz.
        </p>

        {isPending && rivalCount > 0 ? (
          <p className="rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-sm leading-6 text-amber-900 dark:border-amber-900 dark:bg-amber-950 dark:text-amber-200">
            Bu talebi onaylarsan aynı saatte bekleyen{" "}
            <strong className="font-semibold">{rivalCount} talep</strong> düşecek.
          </p>
        ) : null}

        {ttlStatus?.level === "expired" ? (
          <p className="rounded-md border border-border bg-muted px-3 py-2 text-sm leading-6 text-muted-foreground">
            Bu talebin süresi doldu. Onaylamayı denersen sistem kabul etmeyebilir;
            listeyi yenile.
          </p>
        ) : null}
      </CardContent>

      <div className="flex flex-col gap-2 px-6 sm:flex-row">
        {isPending ? (
          <>
            <Button
              className="min-h-11 flex-1"
              disabled={isSubmitting}
              onClick={onApprove}
              type="button"
            >
              {isSubmitting ? "İşleniyor" : "Onayla"}
            </Button>
            <Button
              className="min-h-11 flex-1"
              disabled={isSubmitting}
              onClick={onDecline}
              type="button"
              variant="outline"
            >
              Reddet
            </Button>
          </>
        ) : (
          <p className="flex-1 text-sm text-muted-foreground">
            Bu talep sonuçlandı; karar verilemez.
          </p>
        )}

        <Button
          className="min-h-11"
          disabled={isSubmitting || isReporting || isReported}
          onClick={onReport}
          type="button"
          variant="ghost"
        >
          {reportLabel}
        </Button>
      </div>
    </Card>
  );
}

function DetailRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-baseline justify-between gap-3 border-b border-border/60 py-1 sm:border-none sm:py-0">
      <dt className="shrink-0 text-muted-foreground">{label}</dt>
      <dd className="truncate text-right font-medium text-foreground sm:text-left">
        {value}
      </dd>
    </div>
  );
}

function RequestStatusBadge({ status }: { status: string }) {
  return (
    <Badge
      className={cn("border", statusStyles[status] ?? "border-border bg-muted text-muted-foreground")}
      variant="outline"
    >
      {statusCopy[status] ?? status}
    </Badge>
  );
}

/** Geri sayim DEKORATIF DEGIL: 24 saat dolunca backend talebi gercekten Expire ediyor. */
function TtlBadge({ status }: { status: RequestTtlStatus }) {
  const toneByLevel: Record<RequestTtlStatus["level"], string> = {
    critical:
      "border-rose-300 bg-rose-50 text-rose-800 dark:border-rose-800 dark:bg-rose-950 dark:text-rose-200",
    expired: "border-border bg-muted text-muted-foreground",
    normal: "border-border bg-muted text-muted-foreground",
    warning:
      "border-amber-300 bg-amber-50 text-amber-900 dark:border-amber-800 dark:bg-amber-950 dark:text-amber-200"
  };

  return (
    <Badge className={cn("border", toneByLevel[status.level])} variant="outline">
      {status.level === "expired" ? status.label : `Otomatik düşmesine ${status.label}`}
    </Badge>
  );
}

function EmptyInbox({ hasFilter }: { hasFilter: boolean }) {
  return (
    <Card className="border-dashed">
      <CardHeader className="items-center text-center">
        <CardTitle className="text-base">
          {hasFilter ? "Bu filtrede talep yok" : "Bekleyen talep yok"}
        </CardTitle>
        <CardDescription className="mx-auto max-w-md leading-6">
          {hasFilter
            ? "Aramayı veya durum filtresini değiştirerek diğer talepleri inceleyebilirsin."
            : "Müşterilerden yeni randevu talebi geldiğinde burada görünür ve kararını bekler."}
        </CardDescription>
      </CardHeader>
    </Card>
  );
}

/**
 * Masaustunde Dialog, dokunmatikte alttan Sheet. Panelin birincil cihazi resepsiyon
 * tableti; alttan acilan yuzey basparmak menzilinde kalir.
 */
function ResponsiveModal({
  children,
  description,
  onOpenChange,
  open,
  title
}: {
  children: React.ReactNode;
  description: string;
  onOpenChange: (open: boolean) => void;
  open: boolean;
  title: string;
}) {
  const isMobile = useIsMobile();

  if (isMobile) {
    return (
      <Sheet onOpenChange={onOpenChange} open={open}>
        <SheetContent
          className="max-h-[92vh] overflow-y-auto"
          side="bottom"
        >
          <SheetHeader>
            <SheetTitle>{title}</SheetTitle>
            <SheetDescription>{description}</SheetDescription>
          </SheetHeader>
          <div className="px-4 pb-6">{children}</div>
        </SheetContent>
      </Sheet>
    );
  }

  return (
    <Dialog onOpenChange={onOpenChange} open={open}>
      <DialogContent className="max-h-[85vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>{description}</DialogDescription>
        </DialogHeader>
        {children}
      </DialogContent>
    </Dialog>
  );
}

function ConflictConfirmModal({
  isSubmitting,
  onCancel,
  onConfirm,
  request,
  rivals
}: {
  isSubmitting: boolean;
  onCancel: () => void;
  onConfirm: () => void;
  request: BusinessAppointmentRequest | null;
  rivals: BusinessAppointmentRequest[];
}) {
  return (
    <ResponsiveModal
      description="Bu saat için birden fazla talep bekliyor. Onayladığında sadece seçtiğin talep randevuya dönüşür."
      onOpenChange={(open) => {
        if (!open) {
          onCancel();
        }
      }}
      open={request !== null}
      title="Onaylarsan diğer talepler düşecek"
    >
      {request ? (
        <div className="space-y-4">
          <div className="rounded-md border border-border bg-muted/60 p-3">
            <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
              Onaylayacağın talep
            </p>
            <p className="mt-1 font-medium text-foreground">
              {getServiceSummary(request)}
            </p>
            <p className="text-sm text-muted-foreground">
              {formatRequestStart(request)} ·{" "}
              {request.staffMemberDisplayName ?? "Personel belirtilmemiş"} ·{" "}
              {request.resourceDisplayName ?? "Koltuk belirtilmemiş"}
            </p>
          </div>

          <div>
            <p className="text-sm font-medium text-foreground">
              Düşecek talepler ({rivals.length})
            </p>
            <ul className="mt-2 space-y-2">
              {rivals.map((rival) => (
                <li
                  className="rounded-md border border-amber-200 bg-amber-50 p-3 text-sm dark:border-amber-900 dark:bg-amber-950"
                  key={getRequestKey(rival)}
                >
                  <p className="font-medium text-amber-900 dark:text-amber-200">
                    {getServiceSummary(rival)}
                  </p>
                  <p className="text-amber-800 dark:text-amber-300">
                    {formatRequestStart(rival)} · Müşteri:{" "}
                    {getCustomerHandle(rival)}
                  </p>
                </li>
              ))}
            </ul>
            <p className="mt-2 text-xs leading-5 text-muted-foreground">
              Kesin çakışma kontrolü onay anında sunucuda yeniden yapılır; sonuç
              burada gördüğünden farklı çıkarsa liste yenilenir.
            </p>
          </div>

          <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
            <Button
              className="min-h-11"
              disabled={isSubmitting}
              onClick={onCancel}
              type="button"
              variant="outline"
            >
              Vazgeç
            </Button>
            <Button
              className="min-h-11"
              disabled={isSubmitting}
              onClick={onConfirm}
              type="button"
            >
              {isSubmitting
                ? "İşleniyor"
                : `Onayla, ${rivals.length} talep düşsün`}
            </Button>
          </div>
        </div>
      ) : null}
    </ResponsiveModal>
  );
}

function AbuseReportModal({
  draft,
  isSubmitting,
  onCancel,
  onDraftChange,
  onSubmit
}: {
  draft: AbuseReportDraft | null;
  isSubmitting: boolean;
  onCancel: () => void;
  onDraftChange: (draft: AbuseReportDraft) => void;
  onSubmit: () => void;
}) {
  const selectedReason = draft
    ? abuseReportReasonOptions.find(
        (option) => option.value === draft.reasonCode
      )
    : undefined;

  return (
    <ResponsiveModal
      description="Bu bildirim müşteriye otomatik yaptırım uygulamaz; talep platform inceleme kuyruğuna alınır."
      onOpenChange={(open) => {
        if (!open) {
          onCancel();
        }
      }}
      open={draft !== null}
      title="Suistimal bildir"
    >
      {draft ? (
        <form
          className="space-y-4"
          onSubmit={(event) => {
            event.preventDefault();
            onSubmit();
          }}
        >
          <div className="rounded-md border border-border bg-muted/60 p-3">
            <p className="font-medium text-foreground">
              {getServiceSummary(draft.request)}
            </p>
            <p className="text-sm text-muted-foreground">
              {draft.request.branchDisplayName ?? "Şube belirtilmemiş"} ·{" "}
              {formatRequestStart(draft.request)}
            </p>
          </div>

          <div className="space-y-2">
            <label
              className="text-sm font-medium text-foreground"
              htmlFor="abuse-reason"
            >
              Bildirim nedeni
            </label>
            <Select
              onValueChange={(value) =>
                onDraftChange({
                  ...draft,
                  reasonCode: value as AbuseReportReasonCode
                })
              }
              value={draft.reasonCode}
            >
              <SelectTrigger
                className="h-11 min-h-11 w-full data-[size=default]:h-11"
                id="abuse-reason"
              >
                <SelectValue placeholder="Neden seç" />
              </SelectTrigger>
              <SelectContent>
                {abuseReportReasonOptions.map((option) => (
                  <SelectItem key={option.value} value={option.value}>
                    {option.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            {/* Aciklama tooltip DEGIL kalici metin: dokunmatik cihazda tooltip yoktur. */}
            {selectedReason ? (
              <p className="text-sm leading-6 text-muted-foreground">
                {selectedReason.description}
              </p>
            ) : null}
          </div>

          <div className="space-y-2">
            <label
              className="text-sm font-medium text-foreground"
              htmlFor="abuse-note"
            >
              Not (isteğe bağlı)
            </label>
            <Textarea
              className="min-h-28"
              id="abuse-note"
              maxLength={abuseReportNoteMaxLength}
              onChange={(event) =>
                onDraftChange({
                  ...draft,
                  note: event.target.value
                })
              }
              placeholder="Kısa ve somut yaz. Telefon, e-posta, ödeme bilgisi yazma."
              value={draft.note}
            />
            <div className="flex justify-between text-xs text-muted-foreground">
              <span>Kişisel veri veya gizli not paylaşma.</span>
              <span>
                {draft.note.length}/{abuseReportNoteMaxLength}
              </span>
            </div>
          </div>

          <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
            <Button
              className="min-h-11"
              disabled={isSubmitting}
              onClick={onCancel}
              type="button"
              variant="outline"
            >
              Vazgeç
            </Button>
            <Button
              className="min-h-11"
              disabled={isSubmitting}
              type="submit"
              variant="destructive"
            >
              {isSubmitting ? "Gönderiliyor" : "İncelemeye gönder"}
            </Button>
          </div>
        </form>
      ) : null}
    </ResponsiveModal>
  );
}

/**
 * Cakisan (ayni personel/koltuk + kesisen zaman) onay bekleyen talepleri tek gruba toplar.
 * Ikili cakisma karari business-request-conflicts.ts'e AITTIR -- burada yalnizca
 * gruplama (union-find) yapilir, cakisma kurali TEKRAR YAZILMAZ.
 */
function groupConflictingRequests(
  requests: BusinessAppointmentRequest[]
): RequestGroup[] {
  const parent = requests.map((_, index) => index);

  function find(index: number): number {
    let root = index;
    let nextParent = parent[root];

    while (nextParent !== undefined && nextParent !== root) {
      root = nextParent;
      nextParent = parent[root];
    }

    return root;
  }

  function union(left: number, right: number) {
    const leftRoot = find(left);
    const rightRoot = find(right);

    if (leftRoot !== rightRoot) {
      parent[rightRoot] = leftRoot;
    }
  }

  for (const [leftIndex, leftRequest] of requests.entries()) {
    for (const [rightIndex, rightRequest] of requests.entries()) {
      if (rightIndex <= leftIndex) {
        continue;
      }

      // hasPendingApprovalConflict ikisinin de PendingApproval olmasini zaten sart kosuyor.
      if (hasPendingApprovalConflict(leftRequest, [rightRequest])) {
        union(leftIndex, rightIndex);
      }
    }
  }

  const groupsByRoot = new Map<number, RequestGroup>();

  // Girdi sirasi korunur: gruplar ilk uyesinin sirasinda cizilir.
  for (const [index, request] of requests.entries()) {
    const root = find(index);
    const group = groupsByRoot.get(root);

    if (group) {
      group.push(request);
    } else {
      groupsByRoot.set(root, [request]);
    }
  }

  return [...groupsByRoot.values()];
}

function getRequestKey(request: BusinessAppointmentRequest) {
  return (
    request.id ?? `${request.branchId}-${request.requestedStartUtc}`
  );
}

function getRequestStatus(request: BusinessAppointmentRequest) {
  return request.status ?? "Unknown";
}

function getServiceSummary(request: BusinessAppointmentRequest) {
  const lines = request.lines ?? [];
  const firstLine = lines.at(0);
  const firstService = firstLine?.serviceNameSnapshot ?? "Hizmet detayı yok";

  if (lines.length <= 1) {
    return firstService;
  }

  return `${firstService} + ${lines.length - 1} hizmet`;
}

function getDurationMinutes(request: BusinessAppointmentRequest) {
  const lineDuration = (request.lines ?? []).reduce(
    (totalMinutes, line) => totalMinutes + (line.durationMinutes ?? 0),
    0
  );

  if (lineDuration > 0) {
    return lineDuration;
  }

  if (!request.requestedStartUtc || !request.requestedEndUtc) {
    return 0;
  }

  const start = new Date(request.requestedStartUtc).getTime();
  const end = new Date(request.requestedEndUtc).getTime();

  if (Number.isNaN(start) || Number.isNaN(end) || end <= start) {
    return 0;
  }

  return Math.round((end - start) / 60000);
}

function formatRequestStart(request: BusinessAppointmentRequest) {
  if (!request.requestedStartUtc) {
    return "Zaman bilgisi yok";
  }

  // Sube saati browser timezone'una SESSIZCE cevrilmez: salon kendi saatiyle calisir.
  if (!request.branchTimeZoneId) {
    return `${request.requestedStartUtc} UTC`;
  }

  return formatBranchDateTime(request.requestedStartUtc, request.branchTimeZoneId);
}

function getCustomerHandle(request: BusinessAppointmentRequest) {
  return shortGuid(request.customerUserAccountId ?? request.customer?.userAccountId);
}

function shortGuid(value?: string | null) {
  if (!value) {
    return "Bilgi yok";
  }

  return `${value.slice(0, 8)}…`;
}

function getDecisionErrorCopy(status: number) {
  if (status === 401) {
    return "Oturum doğrulanamadı; tekrar giriş yapmak gerekebilir.";
  }

  if (status === 403) {
    return "Bu işletme veya şube için karar yetkin yok.";
  }

  if (status === 404) {
    return "Talep bulunamadı veya bu hesapla görüntülenemiyor.";
  }

  if (status === 409) {
    return "Bu talep artık aynı şekilde sonuçlandırılamıyor. Liste yenileniyor.";
  }

  return "İşlem uygulanamadı. Lütfen listeyi yenileyip tekrar dene.";
}

function getAbuseReportErrorCopy(status: number) {
  if (status === 400) {
    return "Bildirim nedeni veya not formatı geçerli değil.";
  }

  if (status === 401) {
    return "Oturum doğrulanamadı; tekrar giriş yapmak gerekebilir.";
  }

  if (status === 403) {
    return "Bu işletme veya şube için suistimal bildirimi oluşturma yetkin yok.";
  }

  if (status === 404) {
    return "Talep bulunamadı veya bu hesapla bildirilemiyor.";
  }

  if (status === 429) {
    return "Günlük bildirim sınırına ulaşıldı. Platform spam koruması devrede.";
  }

  return "Bildirim oluşturulamadı. Lütfen kısa süre sonra tekrar dene.";
}
