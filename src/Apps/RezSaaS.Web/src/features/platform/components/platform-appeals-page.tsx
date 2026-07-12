"use client";

import Link from "next/link";
import { useState, type ReactNode } from "react";
import type {
  PlatformAppeal,
  PlatformAppealsFilters,
  PlatformAppealsOverview,
  PlatformClosureCase
} from "@/features/platform/api/get-platform-appeals-overview";
import {
  PlatformAppealAcceptDialog,
  PlatformAppealRejectDialog
} from "@/features/platform/components/platform-appeal-review-dialog";
import { PlatformClosureProposalDialog } from "@/features/platform/components/platform-closure-proposal-dialog";
import {
  PlatformClosureApproveDialog,
  PlatformClosureRejectDialog,
  PlatformClosureExecuteDialog
} from "@/features/platform/components/platform-closure-review-dialog";
import { routes } from "@/shared/config/routes";
import { Button } from "@/shared/ui/button";
import { Card, CardDescription, CardHeader, CardTitle } from "@/shared/ui/card";
import { StatusBadge } from "@/shared/ui/status-badge";

type PlatformAppealsPageProps = {
  overview: PlatformAppealsOverview;
};

const appealStatusFilters = [
  { label: "Hepsi", value: undefined },
  { label: "PendingReview", value: "PendingReview" },
  { label: "Accepted", value: "Accepted" },
  { label: "Rejected", value: "Rejected" }
];

const closureStatusFilters = [
  { label: "Hepsi", value: undefined },
  { label: "PendingApproval", value: "PendingApproval" },
  { label: "Approved", value: "Approved" },
  { label: "Executing", value: "Executing" },
  { label: "Executed", value: "Executed" },
  { label: "CancelledByAppeal", value: "CancelledByAppeal" },
  { label: "Rejected", value: "Rejected" }
];

export function PlatformAppealsPage({
  overview
}: PlatformAppealsPageProps) {
  const [acceptAppealId, setAcceptAppealId] = useState<string | null>(null);
  const [rejectAppealId, setRejectAppealId] = useState<string | null>(null);
  const [proposeClosureForUser, setProposeClosureForUser] = useState<string | null>(null);
  const [approveClosureCase, setApproveClosureCase] = useState<PlatformClosureCase | null>(null);
  const [rejectClosureCase, setRejectClosureCase] = useState<PlatformClosureCase | null>(null);
  const [executeClosureCase, setExecuteClosureCase] = useState<PlatformClosureCase | null>(null);
  const pendingAppealCount = overview.appeals.filter(
    (appeal) => appeal.status === "PendingReview"
  ).length;
  const activeClosureCount = overview.closureCases.filter((closureCase) =>
    ["PendingApproval", "Approved", "Executing"].includes(
      closureCase.status ?? ""
    )
  ).length;

  return (
    <div className="space-y-6">
      <div className="mx-auto max-w-7xl space-y-8">
        <section className="fade-up rounded-[2.5rem] border border-[var(--rs-border)] bg-[var(--rs-glass)] p-6 shadow-[var(--rs-shadow-card)] backdrop-blur-xl sm:p-8">
          <div className="flex flex-col gap-8 lg:flex-row lg:items-end lg:justify-between">
            <div className="max-w-4xl space-y-5">
              <p className="w-fit rounded-full bg-[var(--rs-accent-soft)] px-4 py-2 text-sm font-medium text-[var(--rs-accent-strong)]">
                Platform appeal desk
              </p>
              <h1 className="text-5xl font-semibold tracking-[-0.07em] text-[var(--rs-ink)] sm:text-7xl">
                İtiraz ve kalıcı kapatma vakalarını karar açmadan incele.
              </h1>
              <p className="max-w-2xl text-lg leading-8 text-[var(--rs-muted-strong)]">
                İtiraz kabul/red, closure proposal, onay (ikinci admin), red ve
                execute mutasyonları açıldı. Reason zorunludur; InternalReason
                yalnız platform yüzeyinde görünür.
              </p>
            </div>

            <div className="grid min-w-80 grid-cols-2 gap-3">
              <MetricCard label="İtiraz" value={overview.appeals.length} />
              <MetricCard label="Bekleyen" value={pendingAppealCount} />
              <MetricCard label="Closure" value={overview.closureCases.length} />
              <MetricCard label="Aktif vaka" value={activeClosureCount} />
            </div>
          </div>
        </section>

        <div className="grid gap-6 xl:grid-cols-[24rem_1fr]">
          <aside className="space-y-6">
            <AppealFilters filters={overview.filters} />
            <SafetyCard />
          </aside>

          <section className="space-y-6">
            {overview.detailNotice ? (
              <Card className="border-[var(--rs-warning-border)] bg-[var(--rs-warning-soft)] p-5 shadow-none">
                <CardTitle>Detay uyarısı</CardTitle>
                <CardDescription className="mt-2">
                  {overview.detailNotice}
                </CardDescription>
              </Card>
            ) : null}

            <div className="grid gap-6 xl:grid-cols-2">
              <AppealList
                appeals={overview.appeals}
                filters={overview.filters}
                selectedAppealId={overview.selectedAppeal?.appealId}
              />
              <ClosureCaseList
                closureCases={overview.closureCases}
                filters={overview.filters}
                selectedClosureCaseId={
                  overview.selectedClosureCase?.closureCaseId
                }
              />
            </div>

            <DetailGrid
              appeal={overview.selectedAppeal}
              closureCase={overview.selectedClosureCase}
              onAcceptAppeal={(appealId) => setAcceptAppealId(appealId)}
              onRejectAppeal={(appealId) => setRejectAppealId(appealId)}
              onProposeClosure={(userAccountId) => setProposeClosureForUser(userAccountId)}
              onApproveClosure={(cc) => setApproveClosureCase(cc)}
              onRejectClosure={(cc) => setRejectClosureCase(cc)}
              onExecuteClosure={(cc) => setExecuteClosureCase(cc)}
            />
          </section>
        </div>
      </div>

      {acceptAppealId ? (
        <PlatformAppealAcceptDialog
          appealId={acceptAppealId}
          onDismiss={() => setAcceptAppealId(null)}
        />
      ) : null}

      {rejectAppealId ? (
        <PlatformAppealRejectDialog
          appealId={rejectAppealId}
          onDismiss={() => setRejectAppealId(null)}
        />
      ) : null}

      {proposeClosureForUser ? (
        <PlatformClosureProposalDialog
          onDismiss={() => setProposeClosureForUser(null)}
          userAccountId={proposeClosureForUser}
        />
      ) : null}

      {approveClosureCase ? (
        <PlatformClosureApproveDialog
          closureCase={approveClosureCase}
          onDismiss={() => setApproveClosureCase(null)}
        />
      ) : null}

      {rejectClosureCase ? (
        <PlatformClosureRejectDialog
          closureCase={rejectClosureCase}
          onDismiss={() => setRejectClosureCase(null)}
        />
      ) : null}

      {executeClosureCase ? (
        <PlatformClosureExecuteDialog
          closureCase={executeClosureCase}
          onDismiss={() => setExecuteClosureCase(null)}
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

function AppealFilters({ filters }: { filters: PlatformAppealsFilters }) {
  return (
    <Card className="p-6">
      <CardHeader>
        <CardTitle>İnceleme filtresi</CardTitle>
        <CardDescription>
          Backend `userAccountId`, `status` ve `take` sözleşmeleriyle çalışır.
        </CardDescription>
      </CardHeader>

      <form action={routes.platform.appeals} className="mt-6 space-y-4">
        {filters.appealStatus ? (
          <input name="appealStatus" type="hidden" value={filters.appealStatus} />
        ) : null}
        {filters.closureStatus ? (
          <input
            name="closureStatus"
            type="hidden"
            value={filters.closureStatus}
          />
        ) : null}
        <label className="grid gap-2 text-sm font-medium text-[var(--rs-ink)]">
          Kullanıcı hesabı
          <input
            className="min-h-11 rounded-full border border-[var(--rs-border)] bg-[var(--rs-surface)] px-5 font-mono text-sm text-[var(--rs-ink)] shadow-[var(--rs-shadow-soft)] outline-none transition focus:border-[var(--rs-accent)] focus:ring-4 focus:ring-[rgba(99_102_241_/_0.18)]"
            defaultValue={filters.userAccountId ?? ""}
            name="userAccountId"
            placeholder="UserAccountId"
            type="search"
          />
        </label>
        <Button className="w-full" type="submit">
          Filtrele
        </Button>
      </form>

      <FilterGroup
        activeValue={filters.appealStatus}
        filters={filters}
        items={appealStatusFilters}
        kind="appealStatus"
        label="İtiraz durumu"
      />
      <FilterGroup
        activeValue={filters.closureStatus}
        filters={filters}
        items={closureStatusFilters}
        kind="closureStatus"
        label="Closure durumu"
      />
    </Card>
  );
}

function FilterGroup({
  activeValue,
  filters,
  items,
  kind,
  label
}: {
  activeValue?: string;
  filters: PlatformAppealsFilters;
  items: { label: string; value?: string }[];
  kind: "appealStatus" | "closureStatus";
  label: string;
}) {
  return (
    <div className="mt-6 space-y-3">
      <p className="text-xs font-semibold uppercase tracking-[0.16em] text-[var(--rs-muted)]">
        {label}
      </p>
      <div className="flex flex-wrap gap-2">
        {items.map((item) => (
          <Link
            className={
              activeValue === item.value || (!activeValue && !item.value)
                ? "rounded-full bg-[var(--rs-accent)] px-4 py-2 text-xs font-medium text-white"
                : "rounded-full border border-[var(--rs-border)] bg-[var(--rs-surface)] px-4 py-2 text-xs font-medium text-[var(--rs-muted)] transition hover:border-[var(--rs-border-strong)] hover:text-[var(--rs-ink)]"
            }
            href={buildAppealsHref({
              ...filters,
              appealId: undefined,
              closureCaseId: undefined,
              [kind]: item.value
            })}
            key={item.label}
          >
            {item.label}
          </Link>
        ))}
      </div>
    </div>
  );
}

function SafetyCard() {
  return (
    <Card className="p-6">
      <CardHeader>
        <CardTitle>Karar kapısı</CardTitle>
        <CardDescription>
          Bu ekran platform-global çalışır ve tenant header göndermez.
        </CardDescription>
      </CardHeader>

      <div className="mt-6 space-y-3 text-sm leading-6">
        <RuleLine text="Accept/reject, closure approve/reject ve execute mutationları açıldı." />
        <RuleLine text="Closure proposal için iki farklı step-up admin, teslim edilmiş notice ve itiraz penceresi gerekir." />
        <RuleLine text="Internal reason yalnız platform yüzeyinde görünür; müşteri yüzeyine CustomerNotice gider." />
      </div>
    </Card>
  );
}

function RuleLine({ text }: { text: string }) {
  return (
    <p className="rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-glass)] p-4 text-[var(--rs-muted)]">
      {text}
    </p>
  );
}

function AppealList({
  appeals,
  filters,
  selectedAppealId
}: {
  appeals: PlatformAppeal[];
  filters: PlatformAppealsFilters;
  selectedAppealId?: string;
}) {
  return (
    <Card className="p-5">
      <CardHeader>
        <CardTitle>İtiraz kuyruğu</CardTitle>
        <CardDescription>
          Müşteri beyanı ve review sonucu aynı kayıt üzerinde izlenir.
        </CardDescription>
      </CardHeader>
      <div className="mt-5 grid gap-3">
        {appeals.length === 0 ? (
          <EmptyState title="Bu filtreyle itiraz bulunamadı." />
        ) : (
          appeals.map((appeal) => (
            <Link
              className={
                selectedAppealId === appeal.appealId
                  ? "rounded-[1.5rem] border border-[var(--rs-border-strong)] bg-[var(--rs-surface)] p-4 shadow-[var(--rs-shadow-card)]"
                  : "rounded-[1.5rem] border border-[var(--rs-border)] bg-[var(--rs-glass)] p-4 shadow-[var(--rs-shadow-soft)] transition hover:-translate-y-0.5 hover:border-[var(--rs-border-strong)]"
              }
              href={buildAppealsHref({
                ...filters,
                appealId: appeal.appealId,
                closureCaseId: undefined
              })}
              key={appeal.appealId}
            >
              <div className="flex flex-wrap items-center justify-between gap-3">
                <StatusBadge status={appeal.status ?? "Unknown"} />
                <span className="font-mono text-xs text-[var(--rs-muted)]">
                  {shortGuid(appeal.appealId)}
                </span>
              </div>
              <h2 className="mt-4 text-lg font-semibold tracking-[-0.04em] text-[var(--rs-ink)]">
                {getTargetTypeCopy(appeal.targetType)} ·{" "}
                {shortGuid(appeal.targetId)}
              </h2>
              <p className="mt-2 line-clamp-2 text-sm leading-6 text-[var(--rs-muted)]">
                {appeal.statement || "Müşteri beyanı yok."}
              </p>
              <p className="mt-3 text-xs text-[var(--rs-muted)]">
                Kullanıcı:{" "}
                <Link
                  className="font-mono underline underline-offset-2 hover:text-[var(--rs-ink)]"
                  href={`/platform/abuse/kullanici/${appeal.userAccountId}`}
                >
                  {shortGuid(appeal.userAccountId)}
                </Link>
              </p>
            </Link>
          ))
        )}
      </div>
    </Card>
  );
}

function ClosureCaseList({
  closureCases,
  filters,
  selectedClosureCaseId
}: {
  closureCases: PlatformClosureCase[];
  filters: PlatformAppealsFilters;
  selectedClosureCaseId?: string;
}) {
  return (
    <Card className="p-5">
      <CardHeader>
        <CardTitle>Closure cases</CardTitle>
        <CardDescription>
          Kalıcı hesap kapatma saga durumları ve mutasyonları.
        </CardDescription>
      </CardHeader>
      <div className="mt-5 grid gap-3">
        {closureCases.length === 0 ? (
          <EmptyState title="Bu filtreyle closure case bulunamadı." />
        ) : (
          closureCases.map((closureCase) => (
            <Link
              className={
                selectedClosureCaseId === closureCase.closureCaseId
                  ? "rounded-[1.5rem] border border-[var(--rs-border-strong)] bg-[var(--rs-surface)] p-4 shadow-[var(--rs-shadow-card)]"
                  : "rounded-[1.5rem] border border-[var(--rs-border)] bg-[var(--rs-glass)] p-4 shadow-[var(--rs-shadow-soft)] transition hover:-translate-y-0.5 hover:border-[var(--rs-border-strong)]"
              }
              href={buildAppealsHref({
                ...filters,
                appealId: undefined,
                closureCaseId: closureCase.closureCaseId
              })}
              key={closureCase.closureCaseId}
            >
              <div className="flex flex-wrap items-center justify-between gap-3">
                <StatusBadge status={closureCase.status ?? "Unknown"} />
                <span className="font-mono text-xs text-[var(--rs-muted)]">
                  {shortGuid(closureCase.closureCaseId)}
                </span>
              </div>
              <h2 className="mt-4 text-lg font-semibold tracking-[-0.04em] text-[var(--rs-ink)]">
                Kullanıcı ·{" "}
                <Link
                  className="underline underline-offset-2 hover:text-[var(--rs-ink-soft)]"
                  href={`/platform/abuse/kullanici/${closureCase.userAccountId}`}
                >
                  {shortGuid(closureCase.userAccountId)}
                </Link>
              </h2>
              <p className="mt-2 line-clamp-2 text-sm leading-6 text-[var(--rs-muted)]">
                {closureCase.internalReason || "Internal reason yok."}
              </p>
            </Link>
          ))
        )}
      </div>
    </Card>
  );
}

function DetailGrid({
  appeal,
  closureCase,
  onAcceptAppeal,
  onRejectAppeal,
  onProposeClosure,
  onApproveClosure,
  onRejectClosure,
  onExecuteClosure
}: {
  appeal: PlatformAppeal | null;
  closureCase: PlatformClosureCase | null;
  onAcceptAppeal: (appealId: string) => void;
  onRejectAppeal: (appealId: string) => void;
  onProposeClosure: (userAccountId: string) => void;
  onApproveClosure: (closureCase: PlatformClosureCase) => void;
  onRejectClosure: (closureCase: PlatformClosureCase) => void;
  onExecuteClosure: (closureCase: PlatformClosureCase) => void;
}) {
  if (!appeal && !closureCase) {
    return (
      <Card className="border-dashed bg-[var(--rs-glass)] p-10 text-center shadow-none">
        <CardTitle>Detay seçilmedi</CardTitle>
        <CardDescription className="mx-auto mt-2 max-w-lg">
          Liste üzerinden bir itiraz veya closure case seçildiğinde karar
          bağlamı burada açılır.
        </CardDescription>
      </Card>
    );
  }

  return (
    <div className="grid gap-6 xl:grid-cols-2">
      {appeal ? (
        <AppealDetailCard
          appeal={appeal}
          onAccept={() => appeal.appealId && onAcceptAppeal(appeal.appealId)}
          onReject={() => appeal.appealId && onRejectAppeal(appeal.appealId)}
        />
      ) : null}
      {closureCase ? (
        <ClosureDetailCard
          closureCase={closureCase}
          onProposeClosure={() =>
            closureCase.userAccountId &&
            onProposeClosure(closureCase.userAccountId)
          }
          onApprove={() => onApproveClosure(closureCase)}
          onReject={() => onRejectClosure(closureCase)}
          onExecute={() => onExecuteClosure(closureCase)}
        />
      ) : null}
    </div>
  );
}

function AppealDetailCard({
  appeal,
  onAccept,
  onReject
}: {
  appeal: PlatformAppeal;
  onAccept: () => void;
  onReject: () => void;
}) {
  const isPending = appeal.status === "PendingReview";

  return (
    <Card className="p-5">
      <CardHeader>
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <CardTitle>İtiraz detayı</CardTitle>
            <CardDescription className="mt-2">
              {getTargetTypeCopy(appeal.targetType)} ·{" "}
              {shortGuid(appeal.targetId)}
            </CardDescription>
          </div>
          <StatusBadge status={appeal.status ?? "Unknown"} />
        </div>
      </CardHeader>

      <div className="mt-6 grid gap-3 md:grid-cols-2">
        <InfoBox label="Appeal" value={shortGuid(appeal.appealId)} />
        <InfoBox label="User" value={shortGuid(appeal.userAccountId)} />
        <InfoBox label="Created" value={formatUtcDateTime(appeal.createdAtUtc)} />
        <InfoBox
          label="Reviewed"
          value={formatUtcDateTime(appeal.reviewedAtUtc)}
        />
      </div>

      <TextBlock label="Müşteri beyanı" value={appeal.statement} />
      <TextBlock label="Review reason" value={appeal.reviewReason} />

      {isPending ? (
        <div className="mt-5 flex flex-wrap gap-3">
          <Button onClick={onAccept} variant="primary">
            İtirazı kabul et
          </Button>
          <Button onClick={onReject} variant="secondary">
            İtirazı reddet
          </Button>
        </div>
      ) : (
        <p className="mt-5 rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface-muted)] p-4 text-sm leading-6 text-[var(--rs-muted)]">
          Kabul edilen itiraz strike/sanction revoke eder veya closure case&apos;i
          `CancelledByAppeal` durumuna taşır.
        </p>
      )}
    </Card>
  );
}

function ClosureDetailCard({
  closureCase,
  onProposeClosure,
  onApprove,
  onReject,
  onExecute
}: {
  closureCase: PlatformClosureCase;
  onProposeClosure: () => void;
  onApprove: () => void;
  onReject: () => void;
  onExecute: () => void;
}) {
  const status = closureCase.status;
  const canApprove = status === "PendingApproval";
  const canReject = status === "PendingApproval" || status === "Approved";
  const canExecute = status === "Approved";

  return (
    <Card className="p-5">
      <CardHeader>
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <CardTitle>Closure case detayı</CardTitle>
            <CardDescription className="mt-2">
              Kullanıcı · {shortGuid(closureCase.userAccountId)}
            </CardDescription>
          </div>
          <StatusBadge status={status ?? "Unknown"} />
        </div>
      </CardHeader>

      <div className="mt-6 grid gap-3 md:grid-cols-2">
        <InfoBox
          label="Closure"
          value={shortGuid(closureCase.closureCaseId)}
        />
        <InfoBox
          label="Proposed by"
          value={shortGuid(closureCase.proposedByUserAccountId)}
        />
        <InfoBox
          label="Notice delivered"
          value={formatUtcDateTime(closureCase.customerNoticeDeliveredAtUtc)}
        />
        <InfoBox
          label="Execution eligible"
          value={formatUtcDateTime(closureCase.eligibleForExecutionAtUtc)}
        />
        <InfoBox
          label="Decided"
          value={formatUtcDateTime(closureCase.decidedAtUtc)}
        />
        <InfoBox
          label="Executed"
          value={formatUtcDateTime(closureCase.executedAtUtc)}
        />
      </div>

      <TextBlock label="Internal reason" value={closureCase.internalReason} />
      <TextBlock label="Customer notice" value={closureCase.customerNotice} />
      <TextBlock label="Decision reason" value={closureCase.decisionReason} />

      <div className="mt-5 flex flex-wrap gap-3">
        {canApprove ? (
          <Button onClick={onApprove} variant="danger">
            Closure onayla
          </Button>
        ) : null}
        {canReject ? (
          <Button onClick={onReject} variant="secondary">
            Closure reddet
          </Button>
        ) : null}
        {canExecute ? (
          <Button onClick={onExecute} variant="danger">
            Execution başlat
          </Button>
        ) : null}
        <Button onClick={onProposeClosure} variant="secondary">
          Yeni closure öner
        </Button>
      </div>

      <p className="mt-5 rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface-muted)] p-4 text-sm leading-6 text-[var(--rs-muted)]">
        Execute için `Approved` vaka, teslim edilmiş notice, dolmuş itiraz
        penceresi, açık itiraz olmaması ve execution anında tekrar hesaplanan
        High risk gerekir.
      </p>
    </Card>
  );
}

function InfoBox({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface)] p-4">
      <p className="text-xs text-[var(--rs-muted)]">{label}</p>
      <p className="mt-2 break-all font-medium text-[var(--rs-ink)]">{value}</p>
    </div>
  );
}

function TextBlock({
  label,
  value
}: {
  label: string;
  value?: string | null;
}) {
  return (
    <div className="mt-5 rounded-[1.5rem] border border-[var(--rs-border)] bg-[var(--rs-surface)] p-4">
      <p className="text-xs font-semibold uppercase tracking-[0.16em] text-[var(--rs-muted)]">
        {label}
      </p>
      <p className="mt-3 whitespace-pre-wrap text-sm leading-6 text-[var(--rs-muted-strong)]">
        {value || "Kayıt yok."}
      </p>
    </div>
  );
}

function EmptyState({ title }: { title: string }) {
  return (
    <p className="rounded-2xl border border-dashed border-[var(--rs-border)] bg-[var(--rs-glass)] p-4 text-sm text-[var(--rs-muted)]">
      {title}
    </p>
  );
}

function buildAppealsHref(filters: PlatformAppealsFilters) {
  const params = new URLSearchParams();

  setParam(params, "userAccountId", filters.userAccountId);
  setParam(params, "appealStatus", filters.appealStatus);
  setParam(params, "closureStatus", filters.closureStatus);
  setParam(params, "appealId", filters.appealId);
  setParam(params, "closureCaseId", filters.closureCaseId);

  const query = params.toString();

  return query ? `${routes.platform.appeals}?${query}` : routes.platform.appeals;
}

function setParam(params: URLSearchParams, key: string, value?: string | null) {
  if (value) {
    params.set(key, value);
  }
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
    return "Yok";
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
    return "Yok";
  }

  return `${value.slice(0, 8)}...`;
}
