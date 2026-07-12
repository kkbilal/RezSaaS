"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useState, type ReactNode } from "react";
import { routes } from "@/shared/config/routes";
import { cn } from "@/shared/lib/cn";
import { Avatar } from "@/shared/ui/avatar";
import { formatBranchDateTime } from "@/shared/lib/date-time";

type PlatformNavValue =
  | "abuse"
  | "appeals"
  | "tenants";

type PlatformShellProps = {
  children: ReactNode;
  sessionEmail: string;
  sessionDisplayName?: string;
  stepUpExpiresAtUtc?: string | null;
  activeNav?: PlatformNavValue;
};

const navItems: Array<{
  href: string;
  label: string;
  value: PlatformNavValue;
}> = [
  { href: routes.platform.abuse, label: "Abuse kontrol", value: "abuse" },
  { href: routes.platform.tenants, label: "Tenantlar", value: "tenants" },
  { href: routes.platform.appeals, label: "İtirazlar", value: "appeals" }
];

export function PlatformShell({
  activeNav,
  children,
  sessionDisplayName,
  sessionEmail,
  stepUpExpiresAtUtc
}: PlatformShellProps) {
  const pathname = usePathname();
  const [mobileOpen, setMobileOpen] = useState(false);

  const resolvedActive =
    activeNav ??
    navItems.find((item) => pathname === item.href)?.value ??
    "abuse";

  const stepUpActive = Boolean(stepUpExpiresAtUtc);

  return (
    <div className="studio-grid min-h-screen">
      <div className="mx-auto flex w-full max-w-7xl gap-0 lg:gap-6 lg:px-6 lg:py-6">
        <aside className="sticky top-0 hidden h-screen w-60 shrink-0 flex-col border-r border-[var(--rs-border)] bg-[var(--rs-surface)]/80 backdrop-blur-xl lg:flex">
          <div className="flex items-center gap-2 px-4 py-5">
            <span className="flex h-8 w-8 items-center justify-center rounded-full bg-[var(--rs-danger)] text-xs font-bold text-white">
              P
            </span>
            <div>
              <p className="text-sm font-semibold tracking-[-0.04em] text-[var(--rs-ink)]">
                RezSaaS
              </p>
              <p className="text-[0.6rem] uppercase tracking-[0.16em] text-[var(--rs-muted)]">
                Platform
              </p>
            </div>
          </div>

          {stepUpActive ? (
            <div className="mx-3 mb-2 flex items-center gap-2 rounded-2xl border border-[var(--rs-danger)]/30 bg-[var(--rs-danger-soft)] px-3 py-2">
              <span className="pulse-warning h-2 w-2 rounded-full bg-[var(--rs-danger)]" />
              <span className="text-[0.65rem] font-semibold uppercase tracking-[0.16em] text-[var(--rs-danger)]">
                Step-up aktif
              </span>
            </div>
          ) : (
            <div className="mx-3 mb-2 flex items-center gap-2 rounded-2xl border border-[var(--rs-warning-border)] bg-[var(--rs-warning-soft)] px-3 py-2">
              <span className="h-2 w-2 rounded-full bg-[var(--rs-warning)]" />
              <span className="text-[0.65rem] font-semibold uppercase tracking-[0.16em] text-[var(--rs-warning)]">
                Step-up gerekli
              </span>
            </div>
          )}

          <nav className="flex-1 space-y-1 px-2 py-2">
            {navItems.map((item) => {
              const active = resolvedActive === item.value;
              return (
                <Link
                  key={item.value}
                  href={item.href}
                  aria-current={active ? "page" : undefined}
                  className={cn(
                    "flex items-center gap-2 rounded-full px-3 py-2 text-sm font-medium transition",
                    active
                      ? "bg-[var(--rs-accent)] text-white shadow-[var(--rs-shadow-button)]"
                      : "text-[var(--rs-muted)] hover:bg-[var(--rs-surface-muted)] hover:text-[var(--rs-ink)]"
                  )}
                >
                  {item.label}
                </Link>
              );
            })}
          </nav>

          <div className="border-t border-[var(--rs-border)] px-3 py-4">
            <div className="flex items-center gap-2">
              <Avatar name={sessionDisplayName ?? sessionEmail} size="sm" />
              <div className="min-w-0 flex-1">
                <p className="truncate text-xs font-medium text-[var(--rs-ink)]">
                  {sessionDisplayName ?? "Platform admin"}
                </p>
                <p className="truncate text-[0.65rem] text-[var(--rs-muted)]">
                  {sessionEmail}
                </p>
              </div>
            </div>
            {stepUpExpiresAtUtc ? (
              <p className="mt-2 text-[0.6rem] text-[var(--rs-muted)]">
                Step-up bitiş: {formatBranchDateTime(stepUpExpiresAtUtc, "UTC")}
              </p>
            ) : null}
          </div>
        </aside>

        <div className="flex min-w-0 flex-1 flex-col px-4 py-6 lg:px-0">
          <header className="flex items-center justify-between gap-3 pb-4">
            <div className="flex items-center gap-3">
              <button
                type="button"
                onClick={() => setMobileOpen((open) => !open)}
                aria-label="Menüyü aç"
                className="inline-flex h-9 w-9 items-center justify-center rounded-full border border-[var(--rs-border)] bg-[var(--rs-surface)] text-[var(--rs-ink)] lg:hidden"
              >
                ☰
              </button>
              <p className="text-sm text-[var(--rs-muted)]">
                <span className="text-[var(--rs-muted)]/70">Platform control-plane</span>
                <span className="mx-2 text-[var(--rs-muted)]/50">/</span>
                <span className="font-medium text-[var(--rs-ink)]">
                  {resolvePlatformLabel(resolvedActive)}
                </span>
              </p>
            </div>
            <div className="flex items-center gap-2">
              {stepUpActive ? (
                <span className="hidden items-center gap-1.5 rounded-full border border-[var(--rs-danger)]/30 bg-[var(--rs-danger-soft)] px-3 py-1 text-[0.65rem] font-semibold uppercase tracking-[0.16em] text-[var(--rs-danger)] sm:inline-flex">
                  <span className="pulse-warning h-1.5 w-1.5 rounded-full bg-[var(--rs-danger)]" />
                  Step-up
                </span>
              ) : null}
              <Link
                href={routes.public.home}
                className="text-sm font-medium text-[var(--rs-muted)] hover:text-[var(--rs-ink)]"
              >
                Çıkış
              </Link>
            </div>
          </header>

          {mobileOpen ? (
            <nav className="mb-4 flex flex-col gap-1 rounded-[var(--rs-radius-lg)] border border-[var(--rs-border)] bg-[var(--rs-surface)] p-2 shadow-[var(--rs-shadow-soft)] lg:hidden">
              {navItems.map((item) => {
                const active = resolvedActive === item.value;
                return (
                  <Link
                    key={item.value}
                    href={item.href}
                    onClick={() => setMobileOpen(false)}
                    aria-current={active ? "page" : undefined}
                    className={cn(
                      "rounded-full px-3 py-2 text-sm font-medium",
                      active
                        ? "bg-[var(--rs-accent)] text-white"
                        : "text-[var(--rs-muted)]"
                    )}
                  >
                    {item.label}
                  </Link>
                );
              })}
            </nav>
          ) : null}

          <div className="fade-up flex-1">{children}</div>
        </div>
      </div>
    </div>
  );
}

function resolvePlatformLabel(value: PlatformNavValue): string {
  const map: Record<PlatformNavValue, string> = {
    abuse: "Abuse kontrol",
    appeals: "İtirazlar",
    tenants: "Tenantlar"
  };
  return map[value];
}
