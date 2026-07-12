"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useState, type ReactNode } from "react";
import { routes } from "@/shared/config/routes";
import { cn } from "@/shared/lib/cn";
import { Avatar } from "@/shared/ui/avatar";

type CustomerNavValue = "dashboard" | "requests" | "appeals" | "profile";

type CustomerShellProps = {
  children: ReactNode;
  sessionEmail: string;
  sessionDisplayName?: string;
  activeNav?: CustomerNavValue;
};

const navItems: Array<{ href: string; label: string; value: CustomerNavValue }> = [
  { href: routes.customer.dashboard, label: "Genel bakış", value: "dashboard" },
  { href: routes.customer.requests, label: "Taleplerim", value: "requests" },
  { href: routes.customer.appeals, label: "İtirazlar", value: "appeals" },
  { href: routes.customer.profile, label: "Profil", value: "profile" }
];

export function CustomerShell({
  activeNav,
  children,
  sessionDisplayName,
  sessionEmail
}: CustomerShellProps) {
  const pathname = usePathname();
  const [mobileOpen, setMobileOpen] = useState(false);

  const resolvedActive =
    activeNav ??
    navItems.find((item) => pathname === item.href)?.value ??
    "dashboard";

  return (
    <main className="studio-grid min-h-screen px-4 py-6 sm:px-8">
      <div className="mx-auto max-w-6xl space-y-8">
        <header className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <Link
            className="flex items-center gap-2"
            href={routes.public.home}
          >
            <span className="rs-gradient-bg flex h-8 w-8 items-center justify-center rounded-xl shadow-lg shadow-[rgba(99,102,241,0.3)]">
              <span className="text-xs font-bold text-white">R</span>
            </span>
            <span
              className="text-lg font-bold tracking-tight text-[var(--rs-ink)]"
              style={{ fontFamily: "var(--rs-font-display)" }}
            >
              RezSaaS
            </span>
          </Link>

          <nav className="hidden items-center gap-1 rounded-full border border-[var(--rs-border)] bg-[var(--rs-surface)] p-1 shadow-[var(--rs-shadow-soft)] sm:flex">
            {navItems.map((item) => (
              <Link
                key={item.value}
                href={item.href}
                aria-current={resolvedActive === item.value ? "page" : undefined}
                className={cn(
                  "rounded-full px-4 py-1.5 text-sm font-medium transition",
                  resolvedActive === item.value
                    ? "bg-[var(--rs-accent)] text-white shadow-[var(--rs-shadow-button)]"
                    : "text-[var(--rs-muted)] hover:text-[var(--rs-ink)]"
                )}
              >
                {item.label}
              </Link>
            ))}
          </nav>

          <div className="flex items-center gap-3">
            <Avatar
              name={sessionDisplayName ?? sessionEmail}
              size="sm"
            />
            <span className="hidden rounded-full border border-[var(--rs-border)] bg-[var(--rs-surface)] px-3 py-1.5 text-xs text-[var(--rs-muted)] sm:inline">
              {sessionEmail}
            </span>
            <button
              type="button"
              onClick={() => setMobileOpen((open) => !open)}
              aria-expanded={mobileOpen}
              aria-label="Menüyü aç/kapat"
              className="inline-flex h-9 w-9 items-center justify-center rounded-full border border-[var(--rs-border)] bg-[var(--rs-surface)] text-[var(--rs-ink)] sm:hidden"
            >
              {mobileOpen ? "✕" : "☰"}
            </button>
          </div>
        </header>

        {mobileOpen ? (
          <nav className="flex flex-col gap-1 rounded-[var(--rs-radius-lg)] border border-[var(--rs-border)] bg-[var(--rs-surface)] p-2 shadow-[var(--rs-shadow-soft)] sm:hidden">
            {navItems.map((item) => (
              <Link
                key={item.value}
                href={item.href}
                onClick={() => setMobileOpen(false)}
                aria-current={resolvedActive === item.value ? "page" : undefined}
                className={cn(
                  "rounded-full px-4 py-2 text-sm font-medium transition",
                  resolvedActive === item.value
                    ? "bg-[var(--rs-accent)] text-white"
                    : "text-[var(--rs-muted)] hover:text-[var(--rs-ink)]"
                )}
              >
                {item.label}
              </Link>
            ))}
          </nav>
        ) : null}

        <div className="fade-up">{children}</div>
      </div>
    </main>
  );
}
