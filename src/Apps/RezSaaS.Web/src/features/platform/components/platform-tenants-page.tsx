"use client";

import Link from "next/link";
import { useState } from "react";
import type {
  PlatformTenantDetail,
  PlatformTenantFilters,
  PlatformTenantListItem,
  PlatformTenantMembership,
  PlatformTenantsOverview
} from "@/features/platform/api/get-platform-tenants-overview";
import { PlatformTenantLifecycleActions } from "@/features/platform/components/platform-tenant-lifecycle-actions";
import { PlatformTenantProvisionDialog } from "@/features/platform/components/platform-tenant-provision-dialog";
import { PlatformMembershipAddDialog } from "@/features/platform/components/platform-membership-add-dialog";
import {
  PlatformMembershipSuspendDialog,
  PlatformMembershipRevokeDialog
} from "@/features/platform/components/platform-membership-action-dialog";
import { routes } from "@/shared/config/routes";
import { Button } from "@/shared/ui/button";
import { Card, CardDescription, CardHeader, CardTitle } from "@/shared/ui/card";
import { StatusBadge } from "@/shared/ui/status-badge";

type PlatformTenantsPageProps = {
  overview: PlatformTenantsOverview;
};

const statusFilters = [
  { label: "Hepsi", value: undefined },
  { label: "Active", value: "Active" },
  { label: "Suspended", value: "Suspended" },
  { label: "Closed", value: "Closed" }
];

export function PlatformTenantsPage({
  overview
}: PlatformTenantsPageProps) {
  const [showProvisionDialog, setShowProvisionDialog] = useState(false);
  const [showMembershipAdd, setShowMembershipAdd] = useState(false);
  const [suspendTarget, setSuspendTarget] = useState<{
    membership: PlatformTenantMembership;
    tenantId: string;
  } | null>(null);
  const [revokeTarget, setRevokeTarget] = useState<{
    membership: PlatformTenantMembership;
    tenantId: string;
  } | null>(null);
  const tenants = overview.tenants;
  const selectedTenant = overview.selectedTenant;
  const activeCount = tenants.filter(
    (tenant) => tenant.status === "Active"
  ).length;
  const suspendedCount = tenants.filter(
    (tenant) => tenant.status === "Suspended"
  ).length;
  const closedCount = tenants.filter(
    (tenant) => tenant.status === "Closed"
  ).length;

  return (
    <div className="space-y-6">
      <div className="mx-auto max-w-7xl space-y-8">
        <PlatformTenantHeader
          onProvision={() => setShowProvisionDialog(true)}
        />

        <section className="fade-up rounded-[2.5rem] border border-[var(--rs-border)] bg-[var(--rs-glass)] p-6 shadow-[var(--rs-shadow-card)] backdrop-blur-xl sm:p-8">
          <div className="flex flex-col gap-8 lg:flex-row lg:items-end lg:justify-between">
            <div className="max-w-4xl space-y-5">
              <p className="w-fit rounded-full bg-[var(--rs-accent-soft)] px-4 py-2 text-sm font-medium text-[var(--rs-accent-strong)]">
                Tenant control-plane
              </p>
              <h1 className="text-5xl font-semibold tracking-[-0.07em] text-[var(--rs-ink)] sm:text-7xl">
                Tenant yaşam döngüsünü kontrollü platform aksiyonuyla yönet.
              </h1>
              <p className="max-w-2xl text-lg leading-8 text-[var(--rs-muted-strong)]">
                Tenant lifecycle, provisioning ve membership mutationları reason
                ve confirmation kapısıyla açıldı.
              </p>
            </div>

            <div className="grid min-w-80 grid-cols-2 gap-3">
              <MetricCard label="Toplam" value={tenants.length} />
              <MetricCard label="Active" value={activeCount} />
              <MetricCard label="Suspended" value={suspendedCount} />
              <MetricCard label="Closed" value={closedCount} />
            </div>
          </div>
        </section>

        <div className="grid gap-6 xl:grid-cols-[24rem_1fr]">
          <aside className="space-y-6">
            <TenantFilters filters={overview.filters} />
            <SafetyCard />
          </aside>

          <section className="space-y-6">
            <TenantList
              filters={overview.filters}
              selectedTenantId={
                selectedTenant?.tenantId ?? overview.filters.tenantId
              }
              tenants={tenants}
            />
            {selectedTenant ? (
              <TenantDetailCard
                onAddMembership={() => setShowMembershipAdd(true)}
                onSuspendMembership={(membership) =>
                  setSuspendTarget({
                    membership,
                    tenantId: selectedTenant.tenantId!
                  })
                }
                onRevokeMembership={(membership) =>
                  setRevokeTarget({
                    membership,
                    tenantId: selectedTenant.tenantId!
                  })
                }
                tenant={selectedTenant}
              />
            ) : (
              <TenantDetailCard
                onAddMembership={() => {}}
                onSuspendMembership={() => {}}
                onRevokeMembership={() => {}}
                tenant={null}
              />
            )}
          </section>
        </div>
      </div>

      {showProvisionDialog ? (
        <PlatformTenantProvisionDialog
          onDismiss={() => setShowProvisionDialog(false)}
        />
      ) : null}

      {showMembershipAdd && selectedTenant?.tenantId ? (
        <PlatformMembershipAddDialog
          onDismiss={() => setShowMembershipAdd(false)}
          tenantId={selectedTenant.tenantId}
        />
      ) : null}

      {suspendTarget ? (
        <PlatformMembershipSuspendDialog
          membership={suspendTarget.membership}
          onDismiss={() => setSuspendTarget(null)}
          tenantId={suspendTarget.tenantId}
        />
      ) : null}

      {revokeTarget ? (
        <PlatformMembershipRevokeDialog
          membership={revokeTarget.membership}
          onDismiss={() => setRevokeTarget(null)}
          tenantId={revokeTarget.tenantId}
        />
      ) : null}
    </div>
  );
}

function PlatformTenantHeader({
  onProvision
}: {
  onProvision: () => void;
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
        <Button onClick={onProvision} variant="primary">
          Tenant oluştur
        </Button>
        <Button asChild variant="secondary">
          <Link href={routes.platform.abuse}>Abuse overview</Link>
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
    <div className="rounded-[1.5rem] bg-[var(--rs-accent)] p-4 text-white shadow-[var(--rs-shadow-card)]">
      <p className="text-[0.65rem] uppercase tracking-[0.18em] text-white/50">
        {label}
      </p>
      <p className="mt-5 text-4xl font-semibold tracking-[-0.07em]">{value}</p>
    </div>
  );
}

function TenantFilters({ filters }: { filters: PlatformTenantFilters }) {
  return (
    <Card className="p-6">
      <CardHeader>
        <CardTitle>Liste filtresi</CardTitle>
        <CardDescription>
          Backend `search`, `status` ve `take` sözleşmesiyle çalışır.
        </CardDescription>
      </CardHeader>

      <form action={routes.platform.tenants} className="mt-6 space-y-4">
        {filters.status ? (
          <input name="status" type="hidden" value={filters.status} />
        ) : null}
        <label className="grid gap-2 text-sm font-medium text-[var(--rs-ink)]">
          Arama
          <input
            className="min-h-11 rounded-full border border-[var(--rs-border)] bg-[var(--rs-surface)] px-5 text-sm text-[var(--rs-ink)] shadow-[var(--rs-shadow-soft)] outline-none transition focus:border-[var(--rs-accent)] focus:ring-4 focus:ring-[rgba(99_102_241_/_0.18)]"
            defaultValue={filters.search ?? ""}
            name="search"
            placeholder="Slug veya işletme adı"
            type="search"
          />
        </label>
        <Button className="w-full" type="submit">
          Filtrele
        </Button>
      </form>

      <div className="mt-5 flex flex-wrap gap-2">
        {statusFilters.map((filter) => (
          <Link
            className={
              filters.status === filter.value ||
              (!filters.status && filter.value === undefined)
                ? "rounded-full bg-[var(--rs-accent)] px-4 py-2 text-xs font-medium text-white"
                : "rounded-full border border-[var(--rs-border)] bg-[var(--rs-surface)] px-4 py-2 text-xs font-medium text-[var(--rs-muted)] transition hover:border-[var(--rs-border-strong)] hover:text-[var(--rs-ink)]"
            }
            href={buildTenantHref({
              search: filters.search,
              status: filter.value
            })}
            key={filter.label}
          >
            {filter.label}
          </Link>
        ))}
      </div>
    </Card>
  );
}

function SafetyCard() {
  return (
    <Card className="p-6">
      <CardHeader>
        <CardTitle>Operasyon sınırı</CardTitle>
        <CardDescription>
          Bu route platform-global çalışır; business tenant header göndermez.
        </CardDescription>
      </CardHeader>

      <div className="mt-6 space-y-3 text-sm leading-6">
        <RuleLine text="Closed tenant terminal durumdur ve geri alınabilir aksiyon gibi gösterilmez." />
        <RuleLine text="Revoked membership terminaldir; yeniden aktif yapılacak bir buton bu dilimde yoktur." />
        <RuleLine text="Provisioning, lifecycle, membership add/suspend/revoke mutasyonları açıldı." />
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

function TenantList({
  filters,
  selectedTenantId,
  tenants
}: {
  filters: PlatformTenantFilters;
  selectedTenantId?: string | null;
  tenants: PlatformTenantListItem[];
}) {
  return (
    <Card className="p-5">
      <CardHeader>
        <CardTitle>Tenant listesi</CardTitle>
        <CardDescription>
          İlk dilimde en fazla 50 kayıt okunur; cursor pagination sonraki
          sözleşme iyileştirmesidir.
        </CardDescription>
      </CardHeader>

      <div className="mt-5 grid gap-3">
        {tenants.length === 0 ? (
          <EmptyState text="Bu filtreyle tenant bulunamadı." />
        ) : (
          tenants.map((tenant) => (
            <Link
              className={
                selectedTenantId === tenant.tenantId
                  ? "rounded-[1.5rem] border border-[var(--rs-border-strong)] bg-[var(--rs-surface)] p-4 shadow-[var(--rs-shadow-card)]"
                  : "rounded-[1.5rem] border border-[var(--rs-border)] bg-[var(--rs-glass)] p-4 shadow-[var(--rs-shadow-soft)] transition hover:-translate-y-0.5 hover:border-[var(--rs-border-strong)]"
              }
              href={buildTenantHref({
                search: filters.search,
                status: filters.status,
                tenantId: tenant.tenantId
              })}
              key={tenant.tenantId}
            >
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <h2 className="text-xl font-semibold tracking-[-0.04em] text-[var(--rs-ink)]">
                    {tenant.displayName ?? tenant.slug ?? "Tenant"}
                  </h2>
                  <p className="mt-1 font-mono text-xs text-[var(--rs-muted)]">
                    {tenant.slug ?? shortGuid(tenant.tenantId)}
                  </p>
                </div>
                <StatusBadge status={tenant.status ?? "Unknown"} />
              </div>
              <div className="mt-4 grid gap-2 text-xs text-[var(--rs-muted)] sm:grid-cols-3">
                <span>ID: {shortGuid(tenant.tenantId)}</span>
                <span>Aktif üyelik: {tenant.activeMembershipCount ?? 0}</span>
                <span>Oluşturma: {formatUtcDateTime(tenant.createdAtUtc)}</span>
              </div>
            </Link>
          ))
        )}
      </div>
    </Card>
  );
}

function TenantDetailCard({
  onAddMembership,
  onSuspendMembership,
  onRevokeMembership,
  tenant
}: {
  onAddMembership: () => void;
  onSuspendMembership: (membership: PlatformTenantMembership) => void;
  onRevokeMembership: (membership: PlatformTenantMembership) => void;
  tenant: PlatformTenantDetail | null;
}) {
  if (!tenant) {
    return (
      <Card className="border-dashed bg-[var(--rs-glass)] p-10 text-center shadow-none">
        <CardTitle>Tenant detayı seçilmedi</CardTitle>
        <CardDescription className="mx-auto mt-2 max-w-lg">
          Liste üzerinden tenant seçildiğinde membership ve lifecycle snapshot
          burada gösterilir.
        </CardDescription>
      </Card>
    );
  }

  return (
    <Card className="p-5">
      <CardHeader>
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <CardTitle>{tenant.displayName ?? tenant.slug ?? "Tenant"}</CardTitle>
            <CardDescription className="mt-2">
              {tenant.slug ?? shortGuid(tenant.tenantId)} ·{" "}
              {shortGuid(tenant.tenantId)}
            </CardDescription>
          </div>
          <StatusBadge status={tenant.status ?? "Unknown"} />
        </div>
      </CardHeader>

      <div className="mt-6 grid gap-3 md:grid-cols-3">
        <InfoBox label="Oluşturma" value={formatUtcDateTime(tenant.createdAtUtc)} />
        <InfoBox label="Suspended" value={formatUtcDateTime(tenant.suspendedAtUtc)} />
        <InfoBox label="Closed" value={formatUtcDateTime(tenant.closedAtUtc)} />
      </div>

      <PlatformTenantLifecycleActions tenant={tenant} />

      <div className="mt-6 rounded-[1.5rem] border border-[var(--rs-border)] bg-[var(--rs-surface-muted)] p-4">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h3 className="font-semibold tracking-[-0.03em] text-[var(--rs-ink)]">
              Membership snapshot
            </h3>
            <p className="mt-1 text-sm leading-6 text-[var(--rs-muted)]">
              BusinessOwner tenant-wide; BranchManager ve Staff branch scope
              taşıyabilir. Revoked terminaldir.
            </p>
          </div>
          <Button onClick={onAddMembership} variant="secondary">
            Membership ekle
          </Button>
        </div>

        <MembershipList
          memberships={tenant.memberships ?? []}
          onSuspend={onSuspendMembership}
          onRevoke={onRevokeMembership}
        />
      </div>
    </Card>
  );
}

function MembershipList({
  memberships,
  onSuspend,
  onRevoke
}: {
  memberships: PlatformTenantMembership[];
  onSuspend: (membership: PlatformTenantMembership) => void;
  onRevoke: (membership: PlatformTenantMembership) => void;
}) {
  if (memberships.length === 0) {
    return <EmptyState text="Bu tenant için membership kaydı yok." />;
  }

  return (
    <div className="mt-4 grid gap-3">
      {memberships.map((membership) => {
        const isActive = membership.status === "Active";
        const isSuspended = membership.status === "Suspended";

        return (
          <article
            className="rounded-[1.25rem] border border-[var(--rs-border)] bg-[var(--rs-surface)] p-4"
            key={membership.membershipId}
          >
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div>
                <p className="font-semibold text-[var(--rs-ink)]">
                  {membership.role ?? "Rol yok"}
                </p>
                <p className="mt-1 font-mono text-xs text-[var(--rs-muted)]">
                  {shortGuid(membership.userAccountId)}
                </p>
              </div>
              <StatusBadge status={membership.status ?? "Unknown"} />
            </div>
            <div className="mt-4 grid gap-2 text-xs text-[var(--rs-muted)] sm:grid-cols-3">
              <span>Membership: {shortGuid(membership.membershipId)}</span>
              <span>Branch: {shortGuid(membership.branchId)}</span>
              <span>Oluşturma: {formatUtcDateTime(membership.createdAtUtc)}</span>
            </div>
            {isActive ? (
              <div className="mt-3 flex flex-wrap gap-2">
                <Button onClick={() => onSuspend(membership)} variant="ghost">
                  Askıya al
                </Button>
                <Button onClick={() => onRevoke(membership)} variant="ghost">
                  İptal et
                </Button>
              </div>
            ) : null}
            {isSuspended ? (
              <div className="mt-3">
                <Button onClick={() => onRevoke(membership)} variant="ghost">
                  İptal et
                </Button>
              </div>
            ) : null}
          </article>
        );
      })}
    </div>
  );
}

function InfoBox({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface)] p-4">
      <p className="text-xs text-[var(--rs-muted)]">{label}</p>
      <p className="mt-2 font-medium text-[var(--rs-ink)]">{value}</p>
    </div>
  );
}

function EmptyState({ text }: { text: string }) {
  return (
    <p className="rounded-2xl border border-dashed border-[var(--rs-border)] bg-[var(--rs-glass)] p-4 text-sm text-[var(--rs-muted)]">
      {text}
    </p>
  );
}

function buildTenantHref(filters: PlatformTenantFilters) {
  const params = new URLSearchParams();

  if (filters.search) {
    params.set("search", filters.search);
  }

  if (filters.status) {
    params.set("status", filters.status);
  }

  if (filters.tenantId) {
    params.set("tenantId", filters.tenantId);
  }

  const query = params.toString();

  return query ? `${routes.platform.tenants}?${query}` : routes.platform.tenants;
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
