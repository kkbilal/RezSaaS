"use client";

import Link from "next/link";
import { useState, type ReactNode } from "react";
import type {
  PlatformAbuseAppeal,
  PlatformAbuseEvent,
  PlatformAbuseOverview,
  PlatformAbuseReport,
  PlatformClosureCase,
  PlatformReconciliation
} from "@/features/platform/api/get-platform-abuse-overview";
import {
  PlatformReportConfirmDialog,
  PlatformReportDismissDialog
} from "@/features/platform/components/platform-report-review-dialog";
import { Button } from "@/shared/ui/button";
import { Card, CardDescription, CardHeader, CardTitle } from "@/shared/ui/card";
import { StatusBadge } from "@/shared/ui/status-badge";

type PlatformAbusePageProps = {
  overview: PlatformAbuseOverview;
};

export function PlatformAbusePage({
  overview
}: PlatformAbusePageProps) {
  const [confirmReportId, setConfirmReportId] = useState<string | null>(null);
  const [dismissReportId, setDismissReportId] = useState<string | null>(null);
  const reconciliation = overview.reconciliation;
  const criticalCount =
    (reconciliation?.failedNotificationCount ?? 0) +
    (reconciliation?.staleProcessingNotificationCount ?? 0) +
    (reconciliation?.callbackPendingNotificationCount ?? 0) +
    (reconciliation?.notificationOverdueClosureCount ?? 0) +
    (reconciliation?.executionStalledClosureCount ?? 0);

  return (
    <div className="space-y-6">
      <div className="mx-auto max-w-7xl space-y-8">
        <section className="fade-up rounded-[2.5rem] border border-[var(--rs-border)] bg-[var(--rs-glass)] p-6 shadow-[var(--rs-shadow-card)] backdrop-blur-xl sm:p-8">
          <div className="flex flex-col gap-8 lg:flex-row lg:items-end lg:justify-between">
            <div className="max-w-4xl space-y-5">
              <p className="w-fit rounded-full bg-[var(--rs-accent-soft)] px-4 py-2 text-sm font-medium text-[var(--rs-accent-strong)]">
                Platform Control-plane
              </p>
              <h1 className="text-5xl font-semibold tracking-[-0.07em] text-[var(--rs-ink)] sm:text-7xl">
                Abuse ve itiraz sinyallerini step-up kapısı arkasında izle.
              </h1>
              <p className="max-w-2xl text-lg leading-8 text-[var(--rs-muted-strong)]">
                Abuse raporları onaylanabilir veya reddedilebilir. Strike, sanction
                ve closure review mutasyonları kullanıcı detay sayfasında açılır.
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
              <ReportList
                onConfirm={(reportId) => setConfirmReportId(reportId)}
                onDismiss={(reportId) => setDismissReportId(reportId)}
                reports={overview.reports}
              />
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

      {confirmReportId ? (
        <PlatformReportConfirmDialog
          onDismiss={() => setConfirmReportId(null)}
          reportId={confirmReportId}
        />
      ) : null}

      {dismissReportId ? (
        <PlatformReportDismissDialog
          onDismiss={() => setDismissReportId(null)}
          reportId={dismissReportId}
        />
      ) : null}
    </div>
  );
}

function MetricCard({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-[1.5rem] bg-[var(--rs-accent)] p-4 text-white shadow-[var(--rs-shadow-card)]">
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

function ReportList({
  onConfirm,
  onDismiss,
  reports
}: {
  onConfirm: (reportId: string) => void;
  onDismiss: (reportId: string) => void;
  reports: PlatformAbuseReport[];
}) {
  if (reports.length === 0) {
    return <EmptyState title="Bekleyen işletme abuse raporu yok." />;
  }

  return (
    <div className="grid gap-3">
      {reports.map((report) => (
        <article
          className="rounded-[1.5rem] border border-[var(--rs-border)] bg-[var(--rs-glass)] p-4"
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
          <div className="mt-4 flex flex-wrap items-center gap-3">
            <span className="text-xs text-[var(--rs-muted)]">
              Kullanıcı:{" "}
              <Link
                className="font-mono underline underline-offset-2 hover:text-[var(--rs-ink)]"
                href={`/platform/abuse/kullanici/${report.reportedUserAccountId}`}
              >
                {shortGuid(report.reportedUserAccountId)}
              </Link>
            </span>
            <Button
              onClick={() => onConfirm(report.reportId!)}
              variant="danger"
            >
              Strike oluştur
            </Button>
            <Button
              onClick={() => onDismiss(report.reportId!)}
              variant="secondary"
            >
              Reddet
            </Button>
          </div>
        </article>
      ))}
    </div>
  );
}

function AppealList({ appeals }: { appeals: PlatformAbuseAppeal[] }) {
  if (appeals.length === 0) {
    return <EmptyState title="Bekleyen müşteri itirazı yok." />;
  }

  return (
    <div className="grid gap-3">
      {appeals.map((appeal) => (
        <article
          className="rounded-[1.5rem] border border-[var(--rs-border)] bg-[var(--rs-glass)] p-4"
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
            Kullanıcı:{" "}
            <Link
              className="font-mono underline underline-offset-2 hover:text-[var(--rs-ink)]"
              href={`/platform/abuse/kullanici/${appeal.userAccountId}`}
            >
              {shortGuid(appeal.userAccountId)}
            </Link>{" "}
            · Oluşturma: {formatUtcDateTime(appeal.createdAtUtc)}
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
            className="flex items-center justify-between rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface)] px-4 py-3 text-sm"
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
          <EmptyState title="Closure case kaydı yok." />
        ) : (
          closureCases.map((closureCase) => (
            <div
              className="rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface)] p-4 text-sm"
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
          <EmptyState title="Abuse event kaydı yok." />
        ) : (
          events.slice(0, 8).map((event) => (
            <div
              className="rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface)] p-4 text-sm"
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
                User:{" "}
                <Link
                  className="font-mono underline underline-offset-2 hover:text-[var(--rs-ink)]"
                  href={`/platform/abuse/kullanici/${event.userAccountId}`}
                >
                  {shortGuid(event.userAccountId)}
                </Link>{" "}
                · {formatUtcDateTime(event.occurredAtUtc)}
              </p>
            </div>
          ))
        )}
      </div>
    </Card>
  );
}

function EmptyState({ title }: { title: string }) {
  return (
    <p className="rounded-2xl border border-dashed border-[var(--rs-border)] bg-[var(--rs-glass)] p-4 text-sm text-[var(--rs-muted)]">
      {title}
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
