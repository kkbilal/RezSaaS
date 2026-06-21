"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMemo, useRef, useState } from "react";
import type { BusinessAppointmentScheduleState } from "@/features/business/api/get-business-appointments";
import type {
  BusinessAppointmentInboxState,
  BusinessAppointmentRequest
} from "@/features/business/api/get-appointment-inbox";
import type {
  BusinessContextState,
  BusinessTenantContext
} from "@/features/business/api/get-business-context";
import { BusinessAppointmentSchedule } from "@/features/business/components/business-appointment-schedule";
import {
  getPendingApprovalConflictSignals,
  hasPendingApprovalConflict,
  shouldOptimisticallySupersede,
  type BusinessRequestConflictSignal
} from "@/features/business/lib/business-request-conflicts";
import { createTenantApiClient } from "@/shared/api/client";
import { routes, withTenant } from "@/shared/config/routes";
import { formatBranchDateTime } from "@/shared/lib/date-time";
import {
  clearIntentIdempotencyKey,
  getOrCreateIntentIdempotencyKey,
  type IdempotencyKeyCache
} from "@/shared/lib/idempotency";
import { Button } from "@/shared/ui/button";
import { Card, CardDescription, CardHeader, CardTitle } from "@/shared/ui/card";
import {
  DialogFormPanel,
  DialogOverlay,
  DialogPanel
} from "@/shared/ui/dialog";
import { StatusBadge } from "@/shared/ui/status-badge";

type AppointmentRequestStatus =
  | "PendingApproval"
  | "Approved"
  | "Declined"
  | "Expired"
  | "Superseded"
  | "CancelledByCustomer";

type AppointmentFilter = "all" | AppointmentRequestStatus;
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

type BusinessPanelProps = {
  appointmentSchedule: BusinessAppointmentScheduleState;
  context: BusinessContextState;
  inbox: BusinessAppointmentInboxState;
  sessionEmail: string;
};

function contextStatusCopy(context: BusinessContextState) {
  if (context.kind === "ready" && context.tenants.length > 0) {
    return "Yetkili işletme hesabı";
  }

  if (context.kind === "ready") {
    return "İşletme yetkisi görünmüyor";
  }

  if (context.kind === "unauthenticated") {
    return "Giriş bekleniyor";
  }

  return context.reason;
}

export function BusinessPanel({
  appointmentSchedule,
  context,
  inbox,
  sessionEmail
}: BusinessPanelProps) {
  const router = useRouter();
  const tenant = getPanelTenant(context, inbox);
  const tenantId = tenant?.tenantId ?? null;
  const inboxRequests = useMemo(
    () => (inbox.kind === "ready" ? inbox.requests : []),
    [inbox]
  );
  const [statusOverrides, setStatusOverrides] = useState<Record<string, string>>(
    {}
  );
  const [activeFilter, setActiveFilter] = useState<AppointmentFilter>("all");
  const [search, setSearch] = useState("");
  const [conflictCandidate, setConflictCandidate] =
    useState<BusinessAppointmentRequest | null>(null);
  const [reportDraft, setReportDraft] = useState<AbuseReportDraft | null>(null);
  const [toast, setToast] = useState<string | null>(null);
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

  const visibleRequests = useMemo(() => {
    return requests.filter((request) => {
      const requestStatus = getRequestStatus(request);
      const matchesFilter =
        activeFilter === "all" ? true : requestStatus === activeFilter;
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

      return matchesFilter && haystack.includes(search.toLocaleLowerCase("tr-TR"));
    });
  }, [activeFilter, requests, search]);

  function showToast(message: string) {
    setToast(message);
    window.setTimeout(() => setToast(null), 3200);
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
      showToast("Rapor için yetkili işletme ve talep bilgisi doğrulanmalı.");
      return;
    }

    if (reportedRequestIds.has(request.id)) {
      showToast("Bu talep için rapor zaten gönderildi.");
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
      showToast("Rapor için yetkili işletme ve talep bilgisi doğrulanmalı.");
      return;
    }

    if (note.length > abuseReportNoteMaxLength) {
      showToast("Rapor notu 300 karakteri aşamaz.");
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
        showToast(getAbuseReportErrorCopy(result.response.status));
        return;
      }

      setReportedRequestIds((current) => {
        const next = new Set(current);
        next.add(appointmentRequestId);
        return next;
      });
      setReportDraft(null);
      showToast(
        result.response.status === 201
          ? "Abuse raporu platform inceleme kuyruğuna alındı."
          : "Bu talep için mevcut abuse raporu tekrarlandı."
      );
      router.refresh();
    } catch {
      showToast("Abuse raporu şu anda gönderilemedi. Lütfen tekrar dene.");
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
      showToast("İşlem için yetkili işletme ve talep bilgisi doğrulanmalı.");
      return;
    }

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
        showToast(getDecisionErrorCopy(result.response.status));
        router.refresh();
        return;
      }

      const nextStatus =
        result.data?.status ??
        (decision === "approve" ? "Approved" : "Declined");
      applyDecisionResult(request, nextStatus);
      clearIntentIdempotencyKey(decisionIdempotencyKeys.current, intentId);
      setConflictCandidate(null);
      showToast(
        decision === "approve"
          ? "Talep onaylandı; liste güncelleniyor."
          : "Talep reddedildi."
      );
      router.refresh();
    } catch {
      showToast("İşlem şu anda tamamlanamadı. Lütfen tekrar dene.");
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

  return (
    <main className="studio-grid min-h-screen px-4 py-5 sm:px-6 lg:px-8">
      <div className="mx-auto max-w-[1440px] space-y-6">
        <PanelHeader
          context={context}
          inbox={inbox}
          pendingCount={pendingCount}
          sessionEmail={sessionEmail}
          tenantId={tenant?.tenantId}
          tenantName={
            tenant?.tenantDisplayName ?? tenant?.tenantSlug ?? "RezSaaS Merkez"
          }
        />

        <div className="grid gap-6 xl:grid-cols-[25rem_1fr]">
          <aside className="space-y-6">
            <BusinessTenantSwitcher
              activeTenantId={tenant?.tenantId}
              context={context}
            />
            <TenantContextCard context={context} tenant={tenant} />
            <OperatingRulesCard />
            <DarkDecisionCard />
          </aside>

          <section className="space-y-6">
            <InboxToolbar
              activeFilter={activeFilter}
              onFilterChange={setActiveFilter}
              onSearchChange={setSearch}
              search={search}
            />

            {inbox.kind === "unavailable" ? (
              <Card className="border-[var(--rs-warning-border)] bg-[var(--rs-warning-soft)] p-5 shadow-none">
                <CardTitle>Talepler yüklenemedi</CardTitle>
                <CardDescription className="mt-2 text-[var(--rs-warning)]">
                  {inbox.reason} Lütfen kısa süre sonra tekrar dene.
                </CardDescription>
              </Card>
            ) : null}

            <div className="grid gap-4">
              {visibleRequests.length === 0 ? (
                <Card className="border-dashed bg-white/55 p-10 text-center shadow-none">
                  <CardTitle>Bu filtrede talep yok</CardTitle>
                  <CardDescription className="mx-auto mt-2 max-w-md">
                    Aramayı veya durum filtresini değiştirerek diğer talepleri
                    inceleyebilirsin.
                  </CardDescription>
                </Card>
              ) : (
                visibleRequests.map((request, index) => (
                  <AppointmentRequestCard
                    conflictSignals={getPendingApprovalConflictSignals(
                      request,
                      requests
                    )}
                    index={index}
                    isReported={
                      request.id ? reportedRequestIds.has(request.id) : false
                    }
                    isReporting={reportingRequestId === request.id}
                    isSubmitting={actingRequestId === request.id}
                    key={request.id ?? `${request.branchId}-${request.requestedStartUtc}`}
                    onApprove={() => void approveRequest(request)}
                    onDecline={() => void declineRequest(request)}
                    onReport={() => openAbuseReport(request)}
                    request={request}
                  />
                ))
              )}
            </div>

            <BusinessAppointmentSchedule
              onToast={showToast}
              schedule={appointmentSchedule}
              tenantId={tenantId}
            />
          </section>
        </div>
      </div>

      {conflictCandidate ? (
        <ConflictDialog
          isSubmitting={actingRequestId === conflictCandidate.id}
          onCancel={() => setConflictCandidate(null)}
          onConfirm={() => void approveRequest(conflictCandidate, true)}
          request={conflictCandidate}
        />
      ) : null}

      {reportDraft ? (
        <AbuseReportDialog
          draft={reportDraft}
          isSubmitting={reportingRequestId === reportDraft.request.id}
          onCancel={() => setReportDraft(null)}
          onDraftChange={setReportDraft}
          onSubmit={() => void submitAbuseReport()}
        />
      ) : null}

      {toast ? (
        <div className="fixed bottom-5 left-1/2 z-50 w-[calc(100%-2rem)] max-w-xl -translate-x-1/2 rounded-full border border-[var(--rs-border)] bg-white px-5 py-3 text-sm text-[var(--rs-ink)] shadow-[var(--rs-shadow-card)]">
          {toast}
        </div>
      ) : null}
    </main>
  );
}

function PanelHeader({
  context,
  inbox,
  pendingCount,
  sessionEmail,
  tenantId,
  tenantName
}: {
  context: BusinessContextState;
  inbox: BusinessAppointmentInboxState;
  pendingCount: number;
  sessionEmail: string;
  tenantId?: string | null;
  tenantName: string;
}) {
  return (
    <header className="fade-up rounded-[2.5rem] border border-[var(--rs-border)] bg-white/72 p-5 shadow-[var(--rs-shadow-card)] backdrop-blur-xl sm:p-8">
      <div className="flex flex-col gap-8 lg:flex-row lg:items-end lg:justify-between">
        <div className="max-w-3xl space-y-5">
          <div className="flex flex-wrap items-center gap-3">
            <span className="rounded-full bg-[var(--rs-accent-soft)] px-4 py-2 text-sm font-medium text-[var(--rs-accent-strong)]">
              {contextStatusCopy(context)}
            </span>
            <span className="rounded-full border border-[var(--rs-border)] bg-white px-4 py-2 text-sm text-[var(--rs-muted)]">
              Güvenli işletme seçimi
            </span>
            <span className="rounded-full border border-[var(--rs-border)] bg-white px-4 py-2 text-sm text-[var(--rs-muted)]">
              {sessionEmail}
            </span>
            <Button asChild variant="secondary">
              <Link href={withTenant(routes.business.settings, tenantId)}>
                Ayar snapshot
              </Link>
            </Button>
          </div>

          <div className="space-y-3">
            <p className="text-sm font-medium uppercase tracking-[0.24em] text-[var(--rs-muted)]">
              {tenantName}
            </p>
            <h1 className="max-w-4xl text-5xl font-semibold tracking-[-0.07em] text-[var(--rs-ink)] sm:text-7xl">
              Rezervasyon kararlarını netleştiren operasyon paneli.
            </h1>
          </div>
        </div>

        <div className="grid min-w-72 grid-cols-2 gap-3">
          <div className="rounded-[1.75rem] bg-[var(--rs-ink)] p-5 text-white shadow-[var(--rs-shadow-card)]">
            <p className="text-xs uppercase tracking-[0.2em] text-white/55">
              Bekleyen
            </p>
            <p className="mt-6 text-4xl font-semibold tracking-[-0.06em]">
              {pendingCount}
            </p>
            <p className="mt-1 text-xs text-white/60">
              {inbox.kind === "ready" ? "Güncel liste" : "Liste yükleniyor"}
            </p>
          </div>
          <div className="rounded-[1.75rem] border border-[var(--rs-border)] bg-white p-5 shadow-[var(--rs-shadow-soft)]">
            <p className="text-xs uppercase tracking-[0.2em] text-[var(--rs-muted)]">
              SLA
            </p>
            <p className="mt-6 text-4xl font-semibold tracking-[-0.06em]">
              24s
            </p>
            <p className="mt-1 text-xs text-[var(--rs-muted)]">
              Yanıt üst sınırı
            </p>
          </div>
        </div>
      </div>
    </header>
  );
}

function BusinessTenantSwitcher({
  activeTenantId,
  context
}: {
  activeTenantId?: string | null;
  context: BusinessContextState;
}) {
  if (context.kind !== "ready" || context.tenants.length <= 1) {
    return null;
  }

  return (
    <Card className="fade-up p-6">
      <CardHeader>
        <CardTitle>İşletme seçimi</CardTitle>
        <CardDescription>
          Tenant header yalnızca bu doğrulanmış üyeliklerden üretilir.
        </CardDescription>
      </CardHeader>

      <div className="mt-5 space-y-2">
        {context.tenants.map((tenant) => {
          const isActive = tenant.tenantId === activeTenantId;

          return (
            <Link
              className={
                isActive
                  ? "block rounded-2xl border border-[var(--rs-border-strong)] bg-white px-4 py-3 text-sm font-medium text-[var(--rs-ink)] shadow-[var(--rs-shadow-soft)]"
                  : "block rounded-2xl border border-[var(--rs-border)] bg-white/62 px-4 py-3 text-sm text-[var(--rs-muted)] transition hover:border-[var(--rs-border-strong)] hover:text-[var(--rs-ink)]"
              }
              href={withTenant(routes.business.panel, tenant.tenantId)}
              key={tenant.tenantId ?? tenant.membershipId}
            >
              <span className="block">
                {tenant.tenantDisplayName ?? tenant.tenantSlug ?? "İşletme"}
              </span>
              <span className="mt-1 block text-xs opacity-70">
                {getRoleLabel(tenant.role)}
              </span>
            </Link>
          );
        })}
      </div>
    </Card>
  );
}

function TenantContextCard({
  context,
  tenant
}: {
  context: BusinessContextState;
  tenant: BusinessTenantContext | null;
}) {
  return (
    <Card className="fade-up p-6">
      <CardHeader>
        <CardTitle>İşletme bağlamı</CardTitle>
        <CardDescription>
          Panel yalnızca hesabına bağlı işletme ve şube yetkileriyle açılır.
        </CardDescription>
      </CardHeader>

      <div className="mt-6 space-y-4">
        <div className="rounded-[1.5rem] bg-[var(--rs-surface-muted)] p-4">
          <p className="text-xs uppercase tracking-[0.2em] text-[var(--rs-muted)]">
            Aktif işletme
          </p>
          <p className="mt-3 text-lg font-semibold tracking-[-0.03em] text-[var(--rs-ink)]">
            {tenant?.tenantDisplayName ??
              tenant?.tenantSlug ??
              "İşletme bilgisi bekleniyor"}
          </p>
        </div>

        <div className="grid grid-cols-2 gap-3 text-sm">
          <div className="rounded-2xl border border-[var(--rs-border)] bg-white p-4">
            <p className="text-xs text-[var(--rs-muted)]">Rol</p>
            <p className="mt-2 font-medium text-[var(--rs-ink)]">
              {getRoleLabel(tenant?.role)}
            </p>
          </div>
          <div className="rounded-2xl border border-[var(--rs-border)] bg-white p-4">
            <p className="text-xs text-[var(--rs-muted)]">Kapsam</p>
            <p className="mt-2 font-medium text-[var(--rs-ink)]">
              {tenant?.isTenantWide
                ? "Tüm işletme"
                : tenant?.branchId
                  ? "Şube"
                  : "Yok"}
            </p>
          </div>
        </div>

        {context.kind !== "ready" ? (
          <p className="rounded-2xl border border-[var(--rs-warning-border)] bg-[var(--rs-warning-soft)] p-4 text-sm leading-6 text-[var(--rs-warning)]">
            {contextStatusCopy(context)}. İşletme bilgisi doğrulanmadan operasyon
            aksiyonu açılmaz.
          </p>
        ) : null}
      </div>
    </Card>
  );
}

function OperatingRulesCard() {
  return (
    <Card className="fade-up p-6 [animation-delay:80ms]">
      <CardHeader>
        <CardTitle>Operasyon kuralları</CardTitle>
        <CardDescription>
          Panelde görünen her karar RezSaaS booking state machine ile uyumlu
          kalır.
        </CardDescription>
      </CardHeader>

      <div className="mt-6 space-y-3 text-sm leading-6">
        <RuleItem
          label="Onay bekleyen talep slot bloklamaz"
          text="İşletme aynı slot için gelen taleplerden birini seçebilir."
        />
        <RuleItem
          label="Maskeli müşteri bilgisi"
          text="E-posta ve telefon yalnızca güvenli, maskelenmiş biçimde gösterilir."
        />
        <RuleItem
          label="Şube timezone'u korunur"
          text="Zaman gösterimi browser timezone'una sessizce çevrilmez."
        />
        <RuleItem
          label="Resource business-internal kalır"
          text="Kaynak adı yalnızca işletme operasyon yüzeyinde gösterilir."
        />
      </div>
    </Card>
  );
}

function RuleItem({ label, text }: { label: string; text: string }) {
  return (
    <div className="rounded-2xl border border-[var(--rs-border)] bg-white/70 p-4">
      <p className="font-medium text-[var(--rs-ink)]">{label}</p>
      <p className="mt-1 text-[var(--rs-muted)]">{text}</p>
    </div>
  );
}

function DarkDecisionCard() {
  return (
    <section className="fade-up overflow-hidden rounded-[2rem] bg-[var(--rs-ink)] p-6 text-white shadow-[var(--rs-shadow-card)] [animation-delay:120ms]">
      <p className="text-xs uppercase tracking-[0.24em] text-white/45">
        karar odası
      </p>
      <h2 className="mt-8 text-3xl font-semibold tracking-[-0.06em]">
        Çakışmayı gizleme, işletmeye anlaşılır karar olarak sun.
      </h2>
      <p className="mt-5 text-sm leading-6 text-white/68">
        Onay sırasında çakışma yeniden kontrol edilir; seçilen talep netleşirken
        karşılanamayacak talepler işletmeye anlaşılır durumlarla gösterilir.
      </p>
    </section>
  );
}

function InboxToolbar({
  activeFilter,
  onFilterChange,
  onSearchChange,
  search
}: {
  activeFilter: AppointmentFilter;
  onFilterChange: (value: AppointmentFilter) => void;
  onSearchChange: (value: string) => void;
  search: string;
}) {
  const filters: Array<{ label: string; value: AppointmentFilter }> = [
    { label: "Hepsi", value: "all" },
    { label: "Onay bekleyen", value: "PendingApproval" },
    { label: "Onaylanan", value: "Approved" },
    { label: "Reddedilen", value: "Declined" }
  ];

  return (
    <Card className="fade-up p-4 [animation-delay:160ms]">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
        <div>
          <h2 className="text-2xl font-semibold tracking-[-0.05em] text-[var(--rs-ink)]">
            Gelen rezervasyon istekleri
          </h2>
          <p className="mt-1 text-sm text-[var(--rs-muted)]">
            Onay bekleyen ve sonuçlanmış talepleri hizmet, personel veya müşteriyle ara.
          </p>
        </div>

        <div className="flex flex-col gap-3 sm:flex-row">
          <input
            className="min-h-11 rounded-full border border-[var(--rs-border)] bg-white px-5 text-sm text-[var(--rs-ink)] shadow-[var(--rs-shadow-soft)] outline-none transition focus:border-[var(--rs-border-strong)] focus:ring-4 focus:ring-[rgb(5_26_36_/_0.08)]"
            onChange={(event) => onSearchChange(event.target.value)}
            placeholder="Talep, hizmet veya personel ara"
            type="search"
            value={search}
          />
          <div className="flex rounded-full border border-[var(--rs-border)] bg-[var(--rs-surface-muted)] p-1">
            {filters.map((filter) => (
              <button
                className={
                  activeFilter === filter.value
                    ? "rounded-full bg-white px-4 py-2 text-xs font-medium text-[var(--rs-ink)] shadow-[var(--rs-shadow-soft)]"
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

function AppointmentRequestCard({
  conflictSignals,
  index,
  isReported,
  isReporting,
  isSubmitting,
  onApprove,
  onDecline,
  onReport,
  request
}: {
  conflictSignals: BusinessRequestConflictSignal[];
  index: number;
  isReported: boolean;
  isReporting: boolean;
  isSubmitting: boolean;
  onApprove: () => void;
  onDecline: () => void;
  onReport: () => void;
  request: BusinessAppointmentRequest;
}) {
  const status = getRequestStatus(request);
  const isPending = status === "PendingApproval";
  const reportLabel = isReported
    ? "Raporlandı"
    : isReporting
      ? "Gönderiliyor"
      : "Spam/abuse bildir";

  return (
    <article
      className="fade-up group rounded-[2rem] border border-[var(--rs-border)] bg-white/78 p-5 shadow-[var(--rs-shadow-soft)] backdrop-blur-xl transition duration-300 hover:-translate-y-0.5 hover:shadow-[var(--rs-shadow-card)]"
      style={{ animationDelay: `${180 + index * 45}ms` }}
    >
      <div className="grid gap-5 lg:grid-cols-[1fr_auto] lg:items-start">
        <div className="space-y-5">
          <div className="flex flex-wrap items-center gap-3">
            <span className="rounded-full bg-[var(--rs-neutral-soft)] px-3 py-1 font-mono text-xs text-[var(--rs-muted)]">
              {shortGuid(request.id)}
            </span>
            <StatusBadge status={status} />
            <span className="text-xs text-[var(--rs-muted)]">
              {request.branchDisplayName ?? "Şube adı yok"}
            </span>
          </div>

          <div>
            <h3 className="text-2xl font-semibold tracking-[-0.05em] text-[var(--rs-ink)]">
              {getServiceSummary(request)}
            </h3>
            <p className="mt-1 text-sm text-[var(--rs-muted)]">
              Personel:{" "}
              <span className="font-medium text-[var(--rs-muted-strong)]">
                {request.staffMemberDisplayName ?? "Personel adı yok"}
              </span>
            </p>
          </div>

          <div className="grid gap-3 md:grid-cols-3">
            <InfoBlock label="Müşteri hesabı" value={getCustomerHandle(request)} />
            <InfoBlock
              label="Telefon"
              value={request.customer?.maskedPhone ?? "Telefon yok"}
            />
            <InfoBlock
              label="E-posta"
              value={request.customer?.maskedEmail ?? "E-posta yok"}
            />
          </div>

          <div className="rounded-[1.5rem] border border-[var(--rs-border)] bg-[var(--rs-surface-muted)] p-4">
            <div className="flex flex-col gap-3 text-sm md:flex-row md:items-center md:justify-between">
              <div>
                <p className="text-xs uppercase tracking-[0.2em] text-[var(--rs-muted)]">
                  Şube saati
                </p>
                <p className="mt-1 font-medium text-[var(--rs-ink)]">
                  {formatRequestStart(request)}
                </p>
              </div>
              <div className="text-left md:text-right">
                <p className="text-xs uppercase tracking-[0.2em] text-[var(--rs-muted)]">
                  İç kaynak
                </p>
                <p className="mt-1 font-medium text-[var(--rs-accent-strong)]">
                  {request.resourceDisplayName ?? "Kaynak adı yok"}
                </p>
              </div>
            </div>
            <p className="mt-3 text-xs text-[var(--rs-muted)]">
              Toplam süre: {getDurationMinutes(request)} dk
            </p>
          </div>

          {isPending ? <ConflictHint signals={conflictSignals} /> : null}
        </div>

        <div className="flex gap-3 lg:min-w-44 lg:flex-col">
          {isPending ? (
            <>
              <Button
                className="flex-1 lg:w-full"
                disabled={isSubmitting}
                onClick={onApprove}
                type="button"
              >
                {isSubmitting ? "İşleniyor" : "Onayla"}
              </Button>
              <Button
                className="flex-1 lg:w-full"
                disabled={isSubmitting}
                onClick={onDecline}
                type="button"
                variant="secondary"
              >
                Reddet
              </Button>
              <Button
                className="flex-1 lg:w-full"
                disabled={isSubmitting || isReporting || isReported}
                onClick={onReport}
                type="button"
                variant="ghost"
              >
                {reportLabel}
              </Button>
            </>
          ) : (
            <>
              <Button className="lg:w-full" disabled type="button" variant="secondary">
                Karar kapalı
              </Button>
              <Button
                className="lg:w-full"
                disabled={isReporting || isReported}
                onClick={onReport}
                type="button"
                variant="ghost"
              >
                {reportLabel}
              </Button>
            </>
          )}
        </div>
      </div>
    </article>
  );
}

function ConflictHint({ signals }: { signals: BusinessRequestConflictSignal[] }) {
  if (signals.length === 0) {
    return null;
  }

  const signalCopy = formatConflictSignals(signals);

  return (
    <p className="rounded-2xl border border-[var(--rs-warning-border)] bg-[var(--rs-warning-soft)] px-4 py-3 text-sm leading-6 text-[var(--rs-warning)]">
      Bu talep {signalCopy} çakışabilecek başka onay bekleyen taleplerle aynı
      zaman aralığına denk geliyor. Kesin kontrol onay anında yeniden yapılır.
    </p>
  );
}

function formatConflictSignals(signals: BusinessRequestConflictSignal[]) {
  const hasStaffConflict = signals.includes("staff");
  const hasResourceConflict = signals.includes("resource");

  if (hasStaffConflict && hasResourceConflict) {
    return "aynı personel ve aynı iç kaynakla";
  }

  if (hasStaffConflict) {
    return "aynı personelle";
  }

  return "aynı iç kaynakla";
}

function InfoBlock({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-[var(--rs-border)] bg-white p-4">
      <p className="text-xs text-[var(--rs-muted)]">{label}</p>
      <p className="mt-2 font-mono text-sm font-medium text-[var(--rs-ink)]">
        {value}
      </p>
    </div>
  );
}

function ConflictDialog({
  isSubmitting,
  onCancel,
  onConfirm,
  request
}: {
  isSubmitting: boolean;
  onCancel: () => void;
  onConfirm: () => void;
  request: BusinessAppointmentRequest;
}) {
  return (
    <DialogOverlay onEscapeKeyDown={onCancel}>
      <DialogPanel
        descriptionId="business-conflict-dialog-description"
        titleId="business-conflict-dialog-title"
      >
        <div className="space-y-4">
          <StatusBadge status="PendingApproval" />
          <h2
            className="text-3xl font-semibold tracking-[-0.06em] text-[var(--rs-ink)]"
            id="business-conflict-dialog-title"
          >
            Eşzamanlı talep çakışması
          </h2>
          <p
            className="text-sm leading-7 text-[var(--rs-muted)]"
            id="business-conflict-dialog-description"
          >
            {shortGuid(request.id)} talebini onaylamak, aynı şube saatinde
            bekleyen diğer talepleri karşılanamaz duruma taşıyabilir. Onay anında
            personel ve kaynak çakışması yeniden kontrol edilir.
          </p>
        </div>

        <div className="mt-6 rounded-[1.5rem] bg-[var(--rs-surface-muted)] p-4 text-sm">
          <p className="font-medium text-[var(--rs-ink)]">
            {getServiceSummary(request)}
          </p>
          <p className="mt-1 text-[var(--rs-muted)]">
            {request.branchDisplayName ?? "Şube adı yok"} ·{" "}
            {formatRequestStart(request)}
          </p>
        </div>

        <div className="mt-6 flex flex-col gap-3 sm:flex-row sm:justify-end">
          <Button disabled={isSubmitting} onClick={onCancel} type="button" variant="secondary">
            Geri dön
          </Button>
          <Button disabled={isSubmitting} onClick={onConfirm} type="button">
            {isSubmitting ? "İşleniyor" : "Çakışmayı kabul et ve onayla"}
          </Button>
        </div>
      </DialogPanel>
    </DialogOverlay>
  );
}

function AbuseReportDialog({
  draft,
  isSubmitting,
  onCancel,
  onDraftChange,
  onSubmit
}: {
  draft: AbuseReportDraft;
  isSubmitting: boolean;
  onCancel: () => void;
  onDraftChange: (draft: AbuseReportDraft) => void;
  onSubmit: () => void;
}) {
  const selectedReason = abuseReportReasonOptions.find(
    (option) => option.value === draft.reasonCode
  );

  return (
    <DialogOverlay onEscapeKeyDown={onCancel}>
      <DialogFormPanel
        descriptionId="business-abuse-report-dialog-description"
        onSubmit={(event) => {
          event.preventDefault();
          onSubmit();
        }}
        titleId="business-abuse-report-dialog-title"
      >
        <div className="space-y-4">
          <StatusBadge status={getRequestStatus(draft.request)} />
          <h2
            className="text-3xl font-semibold tracking-[-0.06em] text-[var(--rs-ink)]"
            id="business-abuse-report-dialog-title"
          >
            Abuse raporu oluştur
          </h2>
          <p
            className="text-sm leading-7 text-[var(--rs-muted)]"
            id="business-abuse-report-dialog-description"
          >
            Bu işlem müşteriye otomatik yaptırım uygulamaz; talep platform
            inceleme kuyruğuna alınır. Rapor yalnızca bu appointment request
            kapsamında oluşturulur.
          </p>
        </div>

        <div className="mt-6 rounded-[1.5rem] bg-[var(--rs-surface-muted)] p-4 text-sm">
          <p className="font-medium text-[var(--rs-ink)]">
            {getServiceSummary(draft.request)}
          </p>
          <p className="mt-1 text-[var(--rs-muted)]">
            {draft.request.branchDisplayName ?? "Şube adı yok"} ·{" "}
            {formatRequestStart(draft.request)}
          </p>
        </div>

        <div className="mt-6 grid gap-4">
          <label className="grid gap-2 text-sm font-medium text-[var(--rs-ink)]">
            Rapor nedeni
            <select
              className="min-h-12 rounded-2xl border border-[var(--rs-border)] bg-white px-4 text-sm text-[var(--rs-ink)] outline-none transition focus:border-[var(--rs-border-strong)] focus:ring-4 focus:ring-[rgb(5_26_36_/_0.08)]"
              onChange={(event) =>
                onDraftChange({
                  ...draft,
                  reasonCode: event.target.value as AbuseReportReasonCode
                })
              }
              value={draft.reasonCode}
            >
              {abuseReportReasonOptions.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>

          {selectedReason ? (
            <p className="rounded-2xl border border-[var(--rs-border)] bg-white/70 px-4 py-3 text-sm leading-6 text-[var(--rs-muted)]">
              {selectedReason.description}
            </p>
          ) : null}

          <label className="grid gap-2 text-sm font-medium text-[var(--rs-ink)]">
            Operasyon notu
            <textarea
              className="min-h-32 rounded-2xl border border-[var(--rs-border)] bg-white px-4 py-3 text-sm leading-6 text-[var(--rs-ink)] outline-none transition focus:border-[var(--rs-border-strong)] focus:ring-4 focus:ring-[rgb(5_26_36_/_0.08)]"
              maxLength={abuseReportNoteMaxLength}
              onChange={(event) =>
                onDraftChange({
                  ...draft,
                  note: event.target.value
                })
              }
              placeholder="Kısa, somut ve PII/secret içermeyen not yaz."
              value={draft.note}
            />
          </label>

          <div className="flex flex-col gap-2 text-xs text-[var(--rs-muted)] sm:flex-row sm:items-center sm:justify-between">
            <span>
              Telefon, e-posta, token, ödeme bilgisi veya gizli iç not yazma.
            </span>
            <span>
              {draft.note.length}/{abuseReportNoteMaxLength}
            </span>
          </div>
        </div>

        <div className="mt-6 flex flex-col gap-3 sm:flex-row sm:justify-end">
          <Button disabled={isSubmitting} onClick={onCancel} type="button" variant="secondary">
            Vazgeç
          </Button>
          <Button disabled={isSubmitting} type="submit" variant="danger">
            {isSubmitting ? "Gönderiliyor" : "İncelemeye gönder"}
          </Button>
        </div>
      </DialogFormPanel>
    </DialogOverlay>
  );
}

function getPanelTenant(
  context: BusinessContextState,
  inbox: BusinessAppointmentInboxState
): BusinessTenantContext | null {
  if (inbox.kind === "ready") {
    return inbox.tenant;
  }

  return context.kind === "ready" ? context.tenants[0] ?? null : null;
}

function getRoleLabel(role?: string | null) {
  if (role === "BusinessOwner") {
    return "İşletme sahibi";
  }

  if (role === "BranchManager") {
    return "Şube yöneticisi";
  }

  if (role === "Staff") {
    return "Personel";
  }

  return role ?? "Bilinmiyor";
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
    return "Rapor nedeni veya not formatı geçerli değil.";
  }

  if (status === 401) {
    return "Oturum doğrulanamadı; tekrar giriş yapmak gerekebilir.";
  }

  if (status === 403) {
    return "Bu işletme veya şube için abuse raporu oluşturma yetkin yok.";
  }

  if (status === 404) {
    return "Talep bulunamadı veya bu hesapla raporlanamıyor.";
  }

  if (status === 429) {
    return "Günlük rapor sınırına ulaşıldı. Platform spam koruması devrede.";
  }

  return "Abuse raporu oluşturulamadı. Lütfen kısa süre sonra tekrar dene.";
}
