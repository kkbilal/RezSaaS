"use client";

import Link from "next/link";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { Suspense, useMemo, useState, type ReactNode } from "react";
import { accessContextFromCapabilities } from "@/shared/auth/access-context";
import { routes } from "@/shared/config/routes";
import { cn } from "@/shared/lib/cn";
import { PANEL_NAV, pruneNav } from "@/shared/navigation/nav-manifest";
import { Avatar } from "@/shared/ui/avatar";

export type PanelTenantOption = {
  tenantId: string;
  label: string;
  branchLabel?: string;
};

type PanelShellProps = {
  children: ReactNode;
  sessionEmail: string;
  sessionDisplayName?: string;
  tenants: ReadonlyArray<PanelTenantOption>;
  currentTenantId?: string | null;
  pendingRequestCount?: number;
  /**
   * Aktif tenant'in capability listesi (GET /api/business/context -> tenant.capabilities).
   *
   * ZORUNLU ALAN -- opsiyonel yapilmaz. Bir sayfa gecirmeyi unutursa DERLEME HATASI alir.
   * Opsiyonel olsaydi, unutan sayfa sessizce yanlis menu gosterirdi; tam da onlemeye
   * calistigimiz fail-open hatasi.
   */
  capabilities: readonly string[];
};

export function PanelShell(props: PanelShellProps) {
  return (
    <Suspense>
      <PanelShellInner {...props} />
    </Suspense>
  );
}

function PanelShellInner({
  capabilities,
  children,
  currentTenantId,
  pendingRequestCount,
  sessionDisplayName,
  sessionEmail,
  tenants
}: PanelShellProps) {
  const pathname = usePathname();
  const router = useRouter();
  const searchParams = useSearchParams();
  const [collapsed, setCollapsed] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);

  function handleTenantChange(tenantId: string) {
    const params = new URLSearchParams(searchParams?.toString() ?? "");
    params.set("tenantId", tenantId);
    router.push(`${pathname}?${params.toString()}`);
  }

  /*
   * MENU ARTIK SABIT DEGIL -- capability'den TURETILIYOR.
   *
   * Eskiden burada 8 ogeli sabit bir dizi vardi ve role BAKILMADAN herkese basiliyordu.
   * Sonuc: sube muduru (BranchManager) Personel/Hizmetler/Subeler/Kaynaklar/Yetenekler/
   * Kaynak turleri/Calisma saatleri/Ayarlar ogelerinin HEPSINI goruyor, ama backend'de
   * bu uclarin 11'i de yalnizca BusinessOwner kabul ediyor -- yani menudeki 7 ogeden
   * 7'sinde 403 duvarina carpiyordu.
   *
   * pruneNav FAIL-CLOSED: capability yoksa oge SILINIR (disabled birakilmaz -- tiklanabilir
   * bir tuzak biraktirmayiz). Tum ogeleri elenen grup, BASLIGIYLA BIRLIKTE yok olur.
   */
  const navGroups = useMemo(
    () => pruneNav(PANEL_NAV, accessContextFromCapabilities(capabilities)),
    [capabilities]
  );

  const currentTenant =
    tenants.find((tenant) => tenant.tenantId === currentTenantId) ?? tenants[0];

  return (
    <div className="studio-grid min-h-screen">
      <div className="mx-auto flex w-full max-w-7xl gap-0 lg:gap-6 lg:px-6 lg:py-6">
        <aside
          className={cn(
            "sticky top-0 hidden h-screen shrink-0 flex-col border-r border-[var(--rs-border)] bg-[var(--rs-surface)]/80 backdrop-blur-xl transition-[width] duration-300 lg:flex",
            collapsed ? "w-16" : "w-60"
          )}
        >
          <div className="flex items-center gap-2 px-4 py-5">
            <Link
              href={routes.public.home}
              className="flex items-center gap-2"
            >
              <span className="rs-gradient-bg flex h-8 w-8 items-center justify-center rounded-xl shadow-lg shadow-[rgba(99,102,241,0.3)]">
                <span className="text-xs font-bold text-white">R</span>
              </span>
              {!collapsed ? (
                <span
                  className="text-base font-bold tracking-tight text-[var(--rs-ink)]"
                  style={{ fontFamily: "var(--rs-font-display)" }}
                >
                  RezSaaS
                </span>
              ) : null}
            </Link>
            {!collapsed ? (
              <button
                type="button"
                onClick={() => setCollapsed((value) => !value)}
                aria-label="Kenar çubuğunu daralt"
                className="ml-auto rounded-full p-1 text-[var(--rs-muted)] hover:bg-[var(--rs-surface-muted)]"
              >
                ‹
              </button>
            ) : (
              <button
                type="button"
                onClick={() => setCollapsed((value) => !value)}
                aria-label="Kenar çubuğunu genişlet"
                className="mt-2 rounded-full p-1 text-[var(--rs-muted)] hover:bg-[var(--rs-surface-muted)]"
              >
                ›
              </button>
            )}
          </div>

          <TenantSwitcher
            collapsed={collapsed}
            currentTenant={currentTenant}
            tenants={tenants}
            onSelect={handleTenantChange}
          />

          <nav className="mt-2 flex-1 space-y-4 overflow-y-auto px-2 pb-4">
            {navGroups.map((group) => (
              <div key={group.id} className="space-y-1">
                {!collapsed ? (
                  <p className="px-3 py-1 font-mono text-[10px] font-semibold uppercase tracking-[0.18em] text-[var(--rs-muted)]">
                    {group.label}
                  </p>
                ) : null}
                {group.items.map((item) => {
                  const active = isActiveRoute(pathname, item.path);
                  const Icon = item.icon;
                  const badgeValue = resolveBadge(item.badge, pendingRequestCount);

                  return (
                    <Link
                      key={item.id}
                      href={item.path}
                      title={collapsed ? item.label : undefined}
                      aria-current={active ? "page" : undefined}
                      className={cn(
                        // min-h-11 = 44px: dokunma hedefi. Panelin birincil cihazi resepsiyon tableti.
                        "flex min-h-11 items-center gap-2 rounded-full px-3 py-2 text-sm font-medium transition",
                        active
                          ? "bg-[var(--rs-accent)] text-white shadow-[var(--rs-shadow-button)]"
                          : "text-[var(--rs-muted)] hover:bg-[var(--rs-surface-muted)] hover:text-[var(--rs-ink)]",
                        collapsed && "justify-center"
                      )}
                    >
                      {Icon ? <Icon aria-hidden className="size-4 shrink-0" /> : null}
                      {!collapsed ? (
                        <span className="truncate">{item.label}</span>
                      ) : null}
                      {!collapsed && badgeValue !== null ? (
                        <span className="ml-auto inline-flex min-w-5 items-center justify-center rounded-full bg-[var(--rs-accent-soft)] px-1.5 text-[0.65rem] font-semibold text-[var(--rs-accent-strong)]">
                          {badgeValue}
                        </span>
                      ) : null}
                    </Link>
                  );
                })}
              </div>
            ))}
          </nav>

          <div className="border-t border-[var(--rs-border)] px-3 py-4">
            <div
              className={cn(
                "flex items-center gap-2",
                collapsed && "justify-center"
              )}
            >
              <Avatar name={sessionDisplayName ?? sessionEmail} size="sm" />
              {!collapsed ? (
                <div className="min-w-0 flex-1">
                  <p className="truncate text-xs font-medium text-[var(--rs-ink)]">
                    {sessionDisplayName ?? "İşletme kullanıcısı"}
                  </p>
                  <p className="truncate text-[0.65rem] text-[var(--rs-muted)]">
                    {sessionEmail}
                  </p>
                </div>
              ) : null}
            </div>
          </div>
        </aside>

        <div className="flex min-w-0 flex-1 flex-col px-4 py-6 lg:px-0">
          <header className="flex items-center justify-between gap-3 pb-4">
            <button
              type="button"
              onClick={() => setMobileOpen((open) => !open)}
              aria-label="Menüyü aç"
              className="inline-flex h-9 w-9 items-center justify-center rounded-full border border-[var(--rs-border)] bg-[var(--rs-surface)] text-[var(--rs-ink)] lg:hidden"
            >
              ☰
            </button>
            <Breadcrumb pathname={pathname} />
            <Link
              href={routes.public.home}
              className="text-sm font-medium text-[var(--rs-muted)] hover:text-[var(--rs-ink)]"
            >
              Çıkış
            </Link>
          </header>

          {mobileOpen ? (
            <nav className="mb-4 flex flex-col gap-1 rounded-[var(--rs-radius-lg)] border border-[var(--rs-border)] bg-[var(--rs-surface)] p-2 shadow-[var(--rs-shadow-soft)] lg:hidden">
              {navGroups.flatMap((group) =>
                group.items.map((item) => {
                  const active = isActiveRoute(pathname, item.path);
                  const Icon = item.icon;
                  const badgeValue = resolveBadge(item.badge, pendingRequestCount);

                  return (
                    <Link
                      key={item.id}
                      href={item.path}
                      onClick={() => setMobileOpen(false)}
                      aria-current={active ? "page" : undefined}
                      className={cn(
                        // min-h-11 = 44px dokunma hedefi
                        "flex min-h-11 items-center gap-2 rounded-full px-3 py-2 text-sm font-medium",
                        active
                          ? "bg-[var(--rs-accent)] text-white"
                          : "text-[var(--rs-muted)]"
                      )}
                    >
                      {Icon ? <Icon aria-hidden className="size-4 shrink-0" /> : null}
                      <span className="truncate">{item.label}</span>
                      {badgeValue !== null ? (
                        <span className="ml-auto inline-flex min-w-5 items-center justify-center rounded-full bg-[var(--rs-accent-soft)] px-1.5 text-[0.65rem] font-semibold text-[var(--rs-accent-strong)]">
                          {badgeValue}
                        </span>
                      ) : null}
                    </Link>
                  );
                })
              )}
            </nav>
          ) : null}

          <div className="fade-up flex-1">{children}</div>
        </div>
      </div>
    </div>
  );
}

function TenantSwitcher({
  collapsed,
  currentTenant,
  onSelect,
  tenants
}: {
  collapsed: boolean;
  currentTenant?: PanelTenantOption;
  tenants: ReadonlyArray<PanelTenantOption>;
  onSelect?: (tenantId: string) => void;
}) {
  if (tenants.length === 0) {
    return null;
  }

  if (tenants.length === 1) {
    return (
      <div
        className={cn(
          "mx-2 rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface-muted)] px-3 py-2",
          collapsed && "text-center"
        )}
      >
        <p className="truncate text-xs font-semibold text-[var(--rs-ink)]">
          {collapsed ? currentTenant?.label.slice(0, 1) : currentTenant?.label}
        </p>
        {!collapsed && currentTenant?.branchLabel ? (
          <p className="truncate text-[0.65rem] text-[var(--rs-muted)]">
            {currentTenant.branchLabel}
          </p>
        ) : null}
      </div>
    );
  }

  return (
    <div className="mx-2">
      <label className="relative block">
        <span className="sr-only">Aktif işletme</span>
        <select
          value={currentTenant?.tenantId ?? ""}
          onChange={(event) => onSelect?.(event.target.value)}
          className={cn(
            "w-full appearance-none rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface-muted)] px-3 py-2 text-xs font-semibold text-[var(--rs-ink)] outline-none focus:border-[var(--rs-accent)]",
            collapsed && "px-2 text-center"
          )}
        >
          {tenants.map((tenant) => (
            <option key={tenant.tenantId} value={tenant.tenantId}>
              {tenant.label}
              {tenant.branchLabel ? ` · ${tenant.branchLabel}` : ""}
            </option>
          ))}
        </select>
      </label>
    </div>
  );
}

function Breadcrumb({ pathname }: { pathname: string }) {
  const label = resolveBreadcrumbLabel(pathname);

  return (
    <p className="truncate text-sm text-[var(--rs-muted)]">
      <span className="text-[var(--rs-muted)]/70">İşletme paneli</span>
      <span className="mx-2 text-[var(--rs-muted)]/50">/</span>
      <span className="font-medium text-[var(--rs-ink)]">{label}</span>
    </p>
  );
}

function resolveBreadcrumbLabel(pathname: string): string {
  const byPath: Record<string, string> = {
    [routes.business.panel]: "Genel bakış",
    [routes.business.requests]: "Talepler",
    [routes.business.calendar]: "Takvim",
    [routes.business.staff]: "Personel",
    [routes.business.services]: "Hizmetler",
    [routes.business.branches]: "Şubeler",
    [routes.business.resources]: "Kaynaklar",
    [routes.business.skills]: "Yetenekler",
    [routes.business.resourceTypes]: "Kaynak türleri",
    [routes.business.workingHours]: "Çalışma saatleri",
    [routes.business.settings]: "Ayarlar"
  };

  return byPath[pathname] ?? "Panel";
}

function isActiveRoute(pathname: string, href: string): boolean {
  if (href === routes.business.panel) {
    return pathname === href;
  }
  return pathname === href || pathname.startsWith(`${href}/`);
}

/**
 * Manifestteki rozet ANAHTARINI gercek sayiya cevirir.
 *
 * Manifest bir VERI dosyasidir; icinde canli sayi tasimaz. Sayiyi kabuk saglar.
 * Sifir veya tanimsizsa rozet HIC cizilmez ("0" rozeti gurultudur).
 */
function resolveBadge(
  badge: "pendingRequests" | undefined,
  pendingRequestCount: number | undefined
): number | null {
  if (badge !== "pendingRequests") {
    return null;
  }

  return pendingRequestCount && pendingRequestCount > 0 ? pendingRequestCount : null;
}
