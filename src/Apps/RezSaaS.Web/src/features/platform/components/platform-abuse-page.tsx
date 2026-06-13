import Link from "next/link";
import type { ReactNode } from "react";
import type {
  PlatformAbuseAppeal,
  PlatformAbuseEvent,
  PlatformAbuseOverview,
  PlatformAbuseReport,
  PlatformClosureCase,
  PlatformReconciliation
} from "@/features/platform/api/get-platform-abuse-overview";
import { routes } from "@/shared/config/routes";
import { Button } from "@/shared/ui/button";
import { Card, CardDescription, CardHeader, CardTitle } from "@/shared/ui/card";
import { StatusBadge } from "@/shared/ui/status-badge";

type PlatformAbusePageProps = {
  overview: PlatformAbuseOverview;
  sessionEmail: string;
  stepUpExpiresAtUtc?: string | null;
};

export function PlatformAbusePage({
  overview,
  sessionEmail,
  stepUpExpiresAtUtc
}: PlatformAbusePageProps) {
  const reconciliation = overview.reconciliation;
  const criticalCount =
    (reconciliation?.failedNotificationCount ?? 0) +
    (reconciliation?.staleProcessingNotificationCount ?? 0) +
    (reconciliation?.callbackPendingNotificationCount ?? 0) +
    (reconciliation?.notificationOverdueClosureCount ?? 0) +
    (reconciliation?.executionStalledClosureCount ?? 0);

  return (
    <main className="studio-grid min-h-screen px-4 py-6 sm:px-8">
      <div className="mx-auto max-w-7xl space-y-8">
        <PlatformHeader
          sessionEmail={sessionEmail}
          stepUpExpiresAtUtc={stepUpExpiresAtUtc}
        />

        <section className="fade-up rounded-[2.5rem] border border-[var(--rs-border)] bg-white/76 p-6 shadow-[var(--rs-shadow-card)] backdrop-blur-xl sm:p-8">
          <div className="flex flex-col gap-8 lg:flex-row lg:items-end lg:justify-between">
            <div className="max-w-4xl space-y-5">
              <p className="w-fit rounded-full bg-[var(--rs-accent-soft)] px-4 py-2 text-sm font-medium text-[var(--rs-accent-strong)]">
                Platform Control-plane
              </p>
              <h1 className="text-5xl font-semibold tracking-[-0.07em] text-[var(--rs-ink)] sm:text-7xl">
                Abuse ve itiraz sinyallerini step-up kapısı arkasında izle.
              </h1>
              <p className="max-w-2xl text-lg leading-8 text-[var(--rs-muted-strong)]">
                Bu ekran salt-okunur ilk platform dilimidir. Strike, sanction,
                closure review veya execution gibi yüksek riskli aksiyonlar ayrı
                reason ve confirmation akışı tamamlanmadan açılmaz.
              </p>
            </div>

            <div className="grid min-w-80 grid-cols-2 gap-3">
              <MetricCard label="Bekleyen rapor" value={overview.reports.length} />
              <MetricCard label="Bekleyen itiraz" value={overview.appeals.length} />
              <MetricCard label="Closure case" value={overview.closureCases.length} />
              <MetricCard label="Operasyon sinyali" value={criticalCount} />
            </div>
          </div>
        </section>

        <div className="grid gap-6 xl:grid-cols-[1fr_24rem]">
          <section className="space-y-6">
            <PlatformSection
              description="İşletme abuse bildirimleri tek başına yaptırım üretmez; PlatformAdmin incelemesi bekler."
              title="Bekleyen işletme abuse raporları"
            >
              <ReportList reports={overview.reports} />
            </PlatformSection>

            <PlatformSection
              description="Customer appeal kayıtları müşteri-safe response yüzeyinden bağımsız, platform inceleme kuyruğudur."
              title="Bekleyen müşteri itirazları"
            >
              <AppealList appeals={overview.appeals} />
            </PlatformSection>
          </section>

          <aside className="space-y-6">
            <ReconciliationCard reconciliation={reconciliation} />
            <ClosureCaseCard closureCases={overview.closureCases} />
            <EventCard events={overview.events} />
          </aside>
        </div>
      </div>
    </main>
  );
}

function PlatformHeader({
  sessionEmail,
  stepUpExpiresAtUtc
}: {
  sessionEmail: string;
  stepUpExpiresAtUtc?: string | null;
}) {
  return (
    <header className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <Link
        className="text-lg font-semibold tracking-[-0.04em] text-[var(--rs-ink)]"
        href={routes.public.home}
      >
        RezSaaS
      </Link>
      <div className="flex flex-wrap items-center gap-3">
        <span className="rounded-full border border-[var(--rs-border)] bg-white px-4 py-2 text-sm text-[var(--rs-muted)]">
          {sessionEmail}
        </span>
        <span className="rounded-full border border-[var(--rs-border)] bg-white px-4 py-2 text-sm text-[var(--rs-muted)]">
          Step-up: {formatUtcDateTime(stepUpExpiresAtUtc)}
        </span>
        <Button asChild variant="secondary">
          <Link href={routes.platform.tenants}>Tenantlar</Link>
        </Button>
        <Button asChild variant="secondary">
          <Link href={routes.platform.appeals}>İtirazlar</Link>
        </Button>
      </div>
    </header>
  );
}

function MetricCard({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-[1.5rem] bg-[var(--rs-ink)] p-4 text-white shadow-[var(--rs-shadow-card)]">
      <p className="text-[0.65rem] uppercase tracking-[0.18em] text-white/50">
        {label}
      </p>
      <p className="mt-5 text-4xl font-semibold tracking-[-0.07em]">{value}</p>
    </div>
  );
}

function PlatformSection({
  children,
  description,
  title
}: {
  children: ReactNode;
  description: string;
  title: string;
}) {
  return (
    <Card className="p-5">
      <CardHeader>
        <CardTitle>{title}</CardTitle>
        <CardDescription>{description}</CardDescription>
      </CardHeader>
      <div className="mt-5">{children}</div>
    </Card>
  );
}

function ReportList({ reports }: { reports: PlatformAbuseReport[] }) {
  if (reports.length === 0) {
    return <EmptyState text="Bekleyen işletme abuse raporu yok." />;
  }

  return (
    <div className="grid gap-3">
      {reports.map((report) => (
        <article
          className="rounded-[1.5rem] border border-[var(--rs-border)] bg-white/75 p-4"
          key={report.reportId}
        >
          <div className="flex flex-wrap items-center justify-between gap-3">
            <StatusBadge status={report.status ?? "PendingReview"} />
            <span className="font-mono text-xs text-[var(--rs-muted)]">
              {shortGuid(report.reportId)}
            </span>
          </div>
          <h2 className="mt-4 text-xl font-semibold tracking-[-0.04em] text-[var(--rs-ink)]">
            {report.reasonCode ?? "Reason yok"}
          </h2>
          <p className="mt-2 text-sm leading-6 text-[var(--rs-muted)]">
            {report.note || "Operasyon notu girilmemiş."}
          </p>
          <div className="mt-4 grid gap-2 text-xs text-[var(--rs-muted)] sm:grid-cols-3">
            <span>Tenant: {shortGuid(report.tenantId)}</span>
            <span>Talep: {shortGuid(report.appointmentRequestId)}</span>
            <span>Oluşturma: {formatUtcDateTime(report.createdAtUtc)}</span>
          </div>
        </article>
      ))}
    </div>
  );
}

function AppealList({ appeals }: { appeals: PlatformAbuseAppeal[] }) {
  if (appeals.length === 0) {
    return <EmptyState text="Bekleyen müşteri itirazı yok." />;
  }

  return (
    <div className="grid gap-3">
      {appeals.map((appeal) => (
        <article
          className="rounded-[1.5rem] border border-[var(--rs-border)] bg-white/75 p-4"
          key={appeal.appealId}
        >
          <div className="flex flex-wrap items-center justify-between gap-3">
            <StatusBadge status={appeal.status ?? "PendingReview"} />
            <span className="font-mono text-xs text-[var(--rs-muted)]">
              {shortGuid(appeal.appealId)}
            </span>
          </div>
          <h2 className="mt-4 text-xl font-semibold tracking-[-0.04em] text-[var(--rs-ink)]">
            {getTargetTypeCopy(appeal.targetType)} · {shortGuid(appeal.targetId)}
          </h2>
          <p className="mt-2 text-sm leading-6 text-[var(--rs-muted)]">
            {appeal.statement || "Açıklama yok."}
          </p>
          <p className="mt-4 text-xs text-[var(--rs-muted)]">
            Kullanıcı: {shortGuid(appeal.userAccountId)} · Oluşturma:{" "}
            {formatUtcDateTime(appeal.createdAtUtc)}
          </p>
        </article>
      ))}
    </div>
  );
}

function ReconciliationCard({
  reconciliation
}: {
  reconciliation: PlatformReconciliation | null;
}) {
  if (!reconciliation) {
    return (
      <Card className="p-6">
        <CardTitle>Operasyon sağlığı</CardTitle>
        <CardDescription className="mt-2">
          Reconciliation snapshot alınamadı.
        </CardDescription>
      </Card>
    );
  }

  const signals = [
    ["Failed notification", reconciliation.failedNotificationCount ?? 0],
    ["Stale processing", reconciliation.staleProcessingNotificationCount ?? 0],
    ["Callback pending", reconciliation.callbackPendingNotificationCount ?? 0],
    ["Closure overdue", reconciliation.notificationOverdueClosureCount ?? 0],
    ["Execution stalled", reconciliation.executionStalledClosureCount ?? 0]
  ] as const;

  return (
    <Card className="p-6">
      <CardHeader>
        <CardTitle>Operasyon sağlığı</CardTitle>
        <CardDescription>
          Snapshot: {formatUtcDateTime(reconciliation.evaluatedAtUtc)}
        </CardDescription>
      </CardHeader>
      <div className="mt-5 space-y-3">
        <StatusBadge status={reconciliation.status ?? "Unknown"} />
        {signals.map(([label, value]) => (
          <div
            className="flex items-center justify-between rounded-2xl border border-[var(--rs-border)] bg-white px-4 py-3 text-sm"
            key={label}
          >
            <span className="text-[var(--rs-muted)]">{label}</span>
            <span className="font-semibold text-[var(--rs-ink)]">{value}</span>
          </div>
        ))}
      </div>
    </Card>
  );
}

function ClosureCaseCard({
  closureCases
}: {
  closureCases: PlatformClosureCase[];
}) {
  return (
    <Card className="p-6">
      <CardTitle>Closure cases</CardTitle>
      <CardDescription className="mt-2">
        Execute/review aksiyonları bu ilk dilimde kapalıdır.
      </CardDescription>
      <div className="mt-5 space-y-3">
        {closureCases.length === 0 ? (
          <EmptyState text="Closure case kaydı yok." />
        ) : (
          closureCases.map((closureCase) => (
            <div
              className="rounded-2xl border border-[var(--rs-border)] bg-white p-4 text-sm"
              key={closureCase.closureCaseId}
            >
              <StatusBadge status={closureCase.status ?? "Unknown"} />
              <p className="mt-3 font-medium text-[var(--rs-ink)]">
                {shortGuid(closureCase.closureCaseId)}
              </p>
              <p className="mt-2 line-clamp-3 text-[var(--rs-muted)]">
                {closureCase.internalReason || "Internal reason yok."}
              </p>
            </div>
          ))
        )}
      </div>
    </Card>
  );
}

function EventCard({ events }: { events: PlatformAbuseEvent[] }) {
  return (
    <Card className="p-6">
      <CardTitle>Son abuse eventleri</CardTitle>
      <CardDescription className="mt-2">
        Raw details JSON bu overview ekranında gösterilmez.
      </CardDescription>
      <div className="mt-5 space-y-3">
        {events.length === 0 ? (
          <EmptyState text="Abuse event kaydı yok." />
        ) : (
          events.slice(0, 8).map((event) => (
            <div
              className="rounded-2xl border border-[var(--rs-border)] bg-white p-4 text-sm"
              key={event.eventId}
            >
              <div className="flex flex-wrap items-center justify-between gap-2">
                <StatusBadge status={event.severity ?? "Unknown"} />
                <span className="font-mono text-xs text-[var(--rs-muted)]">
                  {shortGuid(event.eventId)}
                </span>
              </div>
              <p className="mt-3 font-medium text-[var(--rs-ink)]">
                {event.eventType ?? "Event"}
              </p>
              <p className="mt-1 text-xs text-[var(--rs-muted)]">
                User: {shortGuid(event.userAccountId)} ·{" "}
                {formatUtcDateTime(event.occurredAtUtc)}
              </p>
            </div>
          ))
        )}
      </div>
    </Card>
  );
}

function EmptyState({ text }: { text: string }) {
  return (
    <p className="rounded-2xl border border-dashed border-[var(--rs-border)] bg-white/60 p-4 text-sm text-[var(--rs-muted)]">
      {text}
    </p>
  );
}

function getTargetTypeCopy(targetType?: string | null) {
  if (targetType === "UserStrike") {
    return "Uyarı";
  }

  if (targetType === "UserSanction") {
    return "Yaptırım";
  }

  if (targetType === "AccountClosureCase") {
    return "Hesap kapatma";
  }

  return targetType ?? "Kayıt";
}

function formatUtcDateTime(value?: string | null) {
  if (!value) {
    return "Zaman yok";
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return "Zaman okunamıyor";
  }

  return `${new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: "UTC"
  }).format(date)} UTC`;
}

function shortGuid(value?: string | null) {
  if (!value) {
    return "Bilgi yok";
  }

  return `${value.slice(0, 8)}...`;
}
