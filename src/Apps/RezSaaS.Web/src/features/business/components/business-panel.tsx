"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import type {
  BusinessAppointmentInboxState,
  BusinessAppointmentRequest
} from "@/features/business/api/get-appointment-inbox";
import type {
  BusinessContextState,
  BusinessTenantContext
} from "@/features/business/api/get-business-context";
import { createTenantApiClient } from "@/shared/api/client";
import { formatBranchDateTime } from "@/shared/lib/date-time";
import { Button } from "@/shared/ui/button";
import { Card, CardDescription, CardHeader, CardTitle } from "@/shared/ui/card";
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

type BusinessPanelProps = {
  context: BusinessContextState;
  inbox: BusinessAppointmentInboxState;
  sessionEmail: string;
};

function contextStatusCopy(context: BusinessContextState) {
  if (context.kind === "ready" && context.tenants.length > 0) {
    return "Backend doğrulamalı aktif bağlam";
  }

  if (context.kind === "ready") {
    return "Oturum var, işletme üyeliği görünmüyor";
  }

  if (context.kind === "unauthenticated") {
    return "Oturum bekleniyor";
  }

  return context.reason;
}

export function BusinessPanel({
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
  const [toast, setToast] = useState<string | null>(null);
  const [actingRequestId, setActingRequestId] = useState<string | null>(null);

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
    if (!forceDecision && hasPendingConflict(request, requests)) {
      setConflictCandidate(request);
      return;
    }

    await decideRequest(request, "approve");
  }

  async function declineRequest(request: BusinessAppointmentRequest) {
    await decideRequest(request, "decline");
  }

  async function decideRequest(
    request: BusinessAppointmentRequest,
    decision: AppointmentDecision
  ) {
    const appointmentRequestId = request.id;

    if (!tenantId || !appointmentRequestId) {
      showToast("İşlem için doğrulanmış tenant ve talep kimliği gerekiyor.");
      return;
    }

    const idempotencyKey = createIdempotencyKey(decision);
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
      setConflictCandidate(null);
      showToast(
        decision === "approve"
          ? "Talep canlı API üzerinden onaylandı; liste yenileniyor."
          : "Talep canlı API üzerinden reddedildi."
      );
      router.refresh();
    } catch {
      showToast("Backend bağlantısı kurulamadı; işlem uygulanmadı.");
    } finally {
      setActingRequestId(null);
    }
  }

  function applyDecisionResult(
    request: BusinessAppointmentRequest,
    nextStatus: string
  ) {
    const appointmentRequestId = request.id;
    const conflictKey = getConflictKey(request);

    if (!appointmentRequestId) {
      return;
    }

    setStatusOverrides((currentOverrides) => {
      const nextOverrides = {
        ...currentOverrides,
        [appointmentRequestId]: nextStatus
      };

      if (nextStatus === "Approved" && conflictKey) {
        for (const currentRequest of requests) {
          if (
            currentRequest.id &&
            currentRequest.id !== appointmentRequestId &&
            getConflictKey(currentRequest) === conflictKey &&
            getRequestStatus(currentRequest) === "PendingApproval"
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
          tenantName={
            tenant?.tenantDisplayName ?? tenant?.tenantSlug ?? "RezSaaS Merkez"
          }
        />

        <div className="grid gap-6 xl:grid-cols-[25rem_1fr]">
          <aside className="space-y-6">
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
                <CardTitle>Canlı inbox okunamadı</CardTitle>
                <CardDescription className="mt-2 text-[var(--rs-warning)]">
                  {inbox.reason} Preview data kullanılmaz; backend kontratı veya
                  oturum düzelene kadar liste boş kalır.
                </CardDescription>
              </Card>
            ) : null}

            <div className="grid gap-4">
              {visibleRequests.length === 0 ? (
                <Card className="border-dashed bg-white/55 p-10 text-center shadow-none">
                  <CardTitle>Bu filtrede canlı talep yok</CardTitle>
                  <CardDescription className="mx-auto mt-2 max-w-md">
                    Liste artık `/api/business/appointment-requests` typed
                    response kontratından beslenir. Aramayı veya durum filtresini
                    değiştirebilirsin.
                  </CardDescription>
                </Card>
              ) : (
                visibleRequests.map((request, index) => (
                  <AppointmentRequestCard
                    index={index}
                    isSubmitting={actingRequestId === request.id}
                    key={request.id ?? `${request.branchId}-${request.requestedStartUtc}`}
                    onApprove={() => void approveRequest(request)}
                    onDecline={() => void declineRequest(request)}
                    request={request}
                  />
                ))
              )}
            </div>
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
  tenantName
}: {
  context: BusinessContextState;
  inbox: BusinessAppointmentInboxState;
  pendingCount: number;
  sessionEmail: string;
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
              Cookie auth ve backend tenant context
            </span>
            <span className="rounded-full border border-[var(--rs-border)] bg-white px-4 py-2 text-sm text-[var(--rs-muted)]">
              {sessionEmail}
            </span>
          </div>

          <div className="space-y-3">
            <p className="text-sm font-medium uppercase tracking-[0.24em] text-[var(--rs-muted)]">
              {tenantName}
            </p>
            <h1 className="max-w-4xl text-5xl font-semibold tracking-[-0.07em] text-[var(--rs-ink)] sm:text-7xl">
              Rezervasyon kararlarını canlı veriye bağlayan operasyon paneli.
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
              {inbox.kind === "ready" ? "Canlı API" : "API bekleniyor"}
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
              PendingApproval TTL üst sınırı
            </p>
          </div>
        </div>
      </div>
    </header>
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
          Kullanıcıya serbest tenant GUID seçtirilmez; header merkezi API client
          tarafından doğrulanmış context ile eklenir.
        </CardDescription>
      </CardHeader>

      <div className="mt-6 space-y-4">
        <div className="rounded-[1.5rem] bg-[var(--rs-surface-muted)] p-4">
          <p className="text-xs uppercase tracking-[0.2em] text-[var(--rs-muted)]">
            Aktif tenant
          </p>
          <p className="mt-3 text-lg font-semibold tracking-[-0.03em] text-[var(--rs-ink)]">
            {tenant?.tenantDisplayName ??
              tenant?.tenantSlug ??
              "Backend context bekleniyor"}
          </p>
          <p className="mt-1 break-all font-mono text-xs text-[var(--rs-muted)]">
            {tenant?.tenantId ?? "tenant id UI'da serbest seçim alanı değildir"}
          </p>
        </div>

        <div className="grid grid-cols-2 gap-3 text-sm">
          <div className="rounded-2xl border border-[var(--rs-border)] bg-white p-4">
            <p className="text-xs text-[var(--rs-muted)]">Rol</p>
            <p className="mt-2 font-medium text-[var(--rs-ink)]">
              {tenant?.role ?? "Bilinmiyor"}
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
            {contextStatusCopy(context)}. Panel gerçek tenant verisi gelmeden
            operasyon aksiyonu açmaz.
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
          label="PendingApproval slot bloklamaz"
          text="İşletme aynı slot için gelen taleplerden birini seçebilir."
        />
        <RuleItem
          label="Masked PII"
          text="E-posta ve telefon ham halde panel response veya log yüzeyine taşınmaz."
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
        Onay aksiyonu idempotency key ile çalışır; backend transaction içinde
        çakışmayı tekrar kontrol eder ve uygun olmayan talepleri Superseded
        durumuna taşır.
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
            Canlı API response tipleriyle beslenen işletme inbox ekranı.
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
  index,
  isSubmitting,
  onApprove,
  onDecline,
  request
}: {
  index: number;
  isSubmitting: boolean;
  onApprove: () => void;
  onDecline: () => void;
  request: BusinessAppointmentRequest;
}) {
  const status = getRequestStatus(request);
  const isPending = status === "PendingApproval";

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

          {isPending ? (
            <ConflictHint request={request} />
          ) : null}
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
            </>
          ) : (
            <Button className="lg:w-full" disabled type="button" variant="secondary">
              Aksiyon kapalı
            </Button>
          )}
        </div>
      </div>
    </article>
  );
}

function ConflictHint({ request }: { request: BusinessAppointmentRequest }) {
  if (!getConflictKey(request)) {
    return null;
  }

  return (
    <p className="rounded-2xl border border-[var(--rs-warning-border)] bg-[var(--rs-warning-soft)] px-4 py-3 text-sm leading-6 text-[var(--rs-warning)]">
      Aynı şube saatinde başka PendingApproval talepler olabilir. UI sadece karar
      öncesi uyarır; kesin çakışma kontrolü backend onay transaction içinde yapılır.
    </p>
  );
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
    <div className="fixed inset-0 z-40 grid place-items-center bg-[rgb(5_26_36_/_0.42)] p-4 backdrop-blur-sm">
      <section className="fade-up w-full max-w-2xl rounded-[2rem] border border-[var(--rs-border)] bg-[var(--rs-surface)] p-6 shadow-[var(--rs-shadow-card)]">
        <div className="space-y-4">
          <StatusBadge status="PendingApproval" />
          <h2 className="text-3xl font-semibold tracking-[-0.06em] text-[var(--rs-ink)]">
            Eşzamanlı talep çakışması
          </h2>
          <p className="text-sm leading-7 text-[var(--rs-muted)]">
            {shortGuid(request.id)} talebini onaylamak, aynı şube saatinde
            bekleyen diğer talepleri Superseded durumuna taşıyabilir. Backend
            onay anında staff/resource çakışmasını transaction içinde tekrar
            kontrol eder.
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
      </section>
    </div>
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

function hasPendingConflict(
  request: BusinessAppointmentRequest,
  allRequests: BusinessAppointmentRequest[]
) {
  const conflictKey = getConflictKey(request);

  if (!conflictKey) {
    return false;
  }

  return allRequests.some(
    (candidate) =>
      candidate.id !== request.id &&
      getRequestStatus(candidate) === "PendingApproval" &&
      getConflictKey(candidate) === conflictKey
  );
}

function getConflictKey(request: BusinessAppointmentRequest) {
  if (!request.branchId || !request.requestedStartUtc || !request.requestedEndUtc) {
    return null;
  }

  return [
    request.branchId,
    request.requestedStartUtc,
    request.requestedEndUtc,
    request.staffMemberId ?? "staff-auto",
    request.resourceId ?? "resource-auto"
  ].join(":");
}

function createIdempotencyKey(decision: AppointmentDecision) {
  const randomPart =
    globalThis.crypto?.randomUUID?.() ??
    `${Date.now().toString(36)}-${Math.random().toString(36).slice(2)}`;

  return `web-${decision}-${randomPart}`;
}

function getDecisionErrorCopy(status: number) {
  if (status === 401) {
    return "Oturum doğrulanamadı; tekrar giriş yapmak gerekebilir.";
  }

  if (status === 403) {
    return "Bu tenant veya şube için karar yetkin yok.";
  }

  if (status === 404) {
    return "Talep bulunamadı veya tenant dışı kaynak gizlendi.";
  }

  if (status === 409) {
    return "Backend çakışma veya idempotency ihlali nedeniyle işlemi reddetti.";
  }

  return `Backend ${status} döndü; işlem uygulanmadı.`;
}
