import Link from "next/link";
import type {
  PlatformAppeal,
  PlatformAppealsFilters,
  PlatformAppealsOverview,
  PlatformClosureCase
} from "@/features/platform/api/get-platform-appeals-overview";
import { routes } from "@/shared/config/routes";
import { Button } from "@/shared/ui/button";
import { Card, CardDescription, CardHeader, CardTitle } from "@/shared/ui/card";
import { StatusBadge } from "@/shared/ui/status-badge";

type PlatformAppealsPageProps = {
  overview: PlatformAppealsOverview;
  sessionEmail: string;
  stepUpExpiresAtUtc?: string | null;
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
  overview,
  sessionEmail,
  stepUpExpiresAtUtc
}: PlatformAppealsPageProps) {
  const pendingAppealCount = overview.appeals.filter(
    (appeal) => appeal.status === "PendingReview"
  ).length;
  const activeClosureCount = overview.closureCases.filter((closureCase) =>
    ["PendingApproval", "Approved", "Executing"].includes(
      closureCase.status ?? ""
    )
  ).length;

  return (
    <main className="studio-grid min-h-screen px-4 py-6 sm:px-8">
      <div className="mx-auto max-w-7xl space-y-8">
        <PlatformAppealsHeader
          sessionEmail={sessionEmail}
          stepUpExpiresAtUtc={stepUpExpiresAtUtc}
        />

        <section className="fade-up rounded-[2.5rem] border border-[var(--rs-border)] bg-white/76 p-6 shadow-[var(--rs-shadow-card)] backdrop-blur-xl sm:p-8">
          <div className="flex flex-col gap-8 lg:flex-row lg:items-end lg:justify-between">
            <div className="max-w-4xl space-y-5">
              <p className="w-fit rounded-full bg-[var(--rs-accent-soft)] px-4 py-2 text-sm font-medium text-[var(--rs-accent-strong)]">
                Platform appeal desk
              </p>
              <h1 className="text-5xl font-semibold tracking-[-0.07em] text-[var(--rs-ink)] sm:text-7xl">
                İtiraz ve kalıcı kapatma vakalarını karar açmadan incele.
              </h1>
              <p className="max-w-2xl text-lg leading-8 text-[var(--rs-muted-strong)]">
                Bu dilim review ve execute mutationlarını özellikle açmaz.
                Amaç, PlatformAdmin&apos;in itiraz beyanını, customer notice ve
                internal reason ayrımını, ikinci admin ve itiraz penceresi
                durumlarını gerçek API ile görmesidir.
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
            />
          </section>
        </div>
      </div>
    </main>
  );
}

function PlatformAppealsHeader({
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
          <Link href={routes.platform.abuse}>Abuse</Link>
        </Button>
        <Button asChild variant="secondary">
          <Link href={routes.platform.tenants}>Tenantlar</Link>
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
            className="min-h-11 rounded-full border border-[var(--rs-border)] bg-white px-5 font-mono text-sm text-[var(--rs-ink)] shadow-[var(--rs-shadow-soft)] outline-none transition focus:border-[var(--rs-border-strong)] focus:ring-4 focus:ring-[rgb(5_26_36_/_0.08)]"
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
                ? "rounded-full bg-[var(--rs-ink)] px-4 py-2 text-xs font-medium text-white"
                : "rounded-full border border-[var(--rs-border)] bg-white px-4 py-2 text-xs font-medium text-[var(--rs-muted)] transition hover:border-[var(--rs-border-strong)] hover:text-[var(--rs-ink)]"
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
        <RuleLine text="Accept/reject, closure approve/reject ve execute mutationları bu dilimde kapalıdır." />
        <RuleLine text="Closure proposal için iki farklı step-up admin, teslim edilmiş notice ve itiraz penceresi gerekir." />
        <RuleLine text="Internal reason yalnız platform yüzeyinde görünür; müşteri yüzeyine CustomerNotice gider." />
      </div>
    </Card>
  );
}

function RuleLine({ text }: { text: string }) {
  return (
    <p className="rounded-2xl border border-[var(--rs-border)] bg-white/70 p-4 text-[var(--rs-muted)]">
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
          <EmptyState text="Bu filtreyle itiraz bulunamadı." />
        ) : (
          appeals.map((appeal) => (
            <Link
              className={
                selectedAppealId === appeal.appealId
                  ? "rounded-[1.5rem] border border-[var(--rs-border-strong)] bg-white p-4 shadow-[var(--rs-shadow-card)]"
                  : "rounded-[1.5rem] border border-[var(--rs-border)] bg-white/74 p-4 shadow-[var(--rs-shadow-soft)] transition hover:-translate-y-0.5 hover:border-[var(--rs-border-strong)]"
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
          Kalıcı hesap kapatma saga durumları read-only izlenir.
        </CardDescription>
      </CardHeader>
      <div className="mt-5 grid gap-3">
        {closureCases.length === 0 ? (
          <EmptyState text="Bu filtreyle closure case bulunamadı." />
        ) : (
          closureCases.map((closureCase) => (
            <Link
              className={
                selectedClosureCaseId === closureCase.closureCaseId
                  ? "rounded-[1.5rem] border border-[var(--rs-border-strong)] bg-white p-4 shadow-[var(--rs-shadow-card)]"
                  : "rounded-[1.5rem] border border-[var(--rs-border)] bg-white/74 p-4 shadow-[var(--rs-shadow-soft)] transition hover:-translate-y-0.5 hover:border-[var(--rs-border-strong)]"
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
                Kullanıcı · {shortGuid(closureCase.userAccountId)}
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
  closureCase
}: {
  appeal: PlatformAppeal | null;
  closureCase: PlatformClosureCase | null;
}) {
  if (!appeal && !closureCase) {
    return (
      <Card className="border-dashed bg-white/55 p-10 text-center shadow-none">
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
      {appeal ? <AppealDetailCard appeal={appeal} /> : null}
      {closureCase ? <ClosureDetailCard closureCase={closureCase} /> : null}
    </div>
  );
}

function AppealDetailCard({ appeal }: { appeal: PlatformAppeal }) {
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

      <p className="mt-5 rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface-muted)] p-4 text-sm leading-6 text-[var(--rs-muted)]">
        Kabul edilen itiraz strike/sanction revoke eder veya closure case&apos;i
        `CancelledByAppeal` durumuna taşır; bu ekranda karar butonu açılmadı.
      </p>
    </Card>
  );
}

function ClosureDetailCard({
  closureCase
}: {
  closureCase: PlatformClosureCase;
}) {
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
          <StatusBadge status={closureCase.status ?? "Unknown"} />
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
    <div className="rounded-2xl border border-[var(--rs-border)] bg-white p-4">
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
    <div className="mt-5 rounded-[1.5rem] border border-[var(--rs-border)] bg-white p-4">
      <p className="text-xs font-semibold uppercase tracking-[0.16em] text-[var(--rs-muted)]">
        {label}
      </p>
      <p className="mt-3 whitespace-pre-wrap text-sm leading-6 text-[var(--rs-muted-strong)]">
        {value || "Kayıt yok."}
      </p>
    </div>
  );
}

function EmptyState({ text }: { text: string }) {
  return (
    <p className="rounded-2xl border border-dashed border-[var(--rs-border)] bg-white/60 p-4 text-sm text-[var(--rs-muted)]">
      {text}
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
