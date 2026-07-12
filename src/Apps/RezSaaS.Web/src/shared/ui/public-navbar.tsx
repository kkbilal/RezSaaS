"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useState } from "react";
import { routes } from "@/shared/config/routes";
import { cn } from "@/shared/lib/cn";
import { Button } from "@/shared/ui/button";

const navLinks = [
  { href: routes.public.discover, label: "Keşfet" },
  { href: routes.auth.login, label: "İşletmeler için" }
];

export function PublicNavbar() {
  const pathname = usePathname();
  const [scrolled, setScrolled] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);

  useEffect(() => {
    const handler = () => setScrolled(window.scrollY > 24);
    handler();
    window.addEventListener("scroll", handler, { passive: true });
    return () => window.removeEventListener("scroll", handler);
  }, []);

  return (
    <nav
      className={cn(
        "fixed inset-x-0 top-0 z-40 transition-all duration-300",
        scrolled &&
          "border-b border-[var(--rs-border)] bg-[var(--rs-bg)]/85 backdrop-blur-2xl"
      )}
    >
      <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 sm:px-6">
        <Link className="flex items-center gap-2" href={routes.public.home}>
          <span className="rs-gradient-bg flex h-8 w-8 items-center justify-center rounded-xl shadow-lg shadow-[rgba(99,102,241,0.3)]">
            <LogoMark />
          </span>
          <span
            className="text-lg font-bold tracking-tight text-[var(--rs-ink)]"
            style={{ fontFamily: 'var(--rs-font-display)' }}
          >
            RezSaaS
          </span>
        </Link>

        <div className="hidden items-center gap-6 md:flex">
          {navLinks.map((item) => (
            <Link
              key={item.href}
              href={item.href}
              aria-current={pathname === item.href ? "page" : undefined}
              className={cn(
                "text-sm transition-colors",
                pathname === item.href
                  ? "text-[var(--rs-ink)]"
                  : "text-[var(--rs-muted)] hover:text-[var(--rs-ink)]"
              )}
            >
              {item.label}
            </Link>
          ))}
        </div>

        <div className="hidden items-center gap-2 md:flex">
          <Button asChild variant="ghost" size="sm">
            <Link href={routes.auth.login}>Giriş Yap</Link>
          </Button>
          <Button asChild size="sm">
            <Link href={routes.auth.register}>Üye Ol</Link>
          </Button>
        </div>

        <button
          type="button"
          aria-label="Menüyü aç"
          aria-expanded={mobileOpen}
          onClick={() => setMobileOpen((open) => !open)}
          className="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-[var(--rs-border)] bg-[var(--rs-glass)] text-[var(--rs-ink)] backdrop-blur-xl md:hidden"
        >
          {mobileOpen ? "✕" : "☰"}
        </button>
      </div>

      {mobileOpen ? (
        <div className="border-t border-[var(--rs-border)] bg-[var(--rs-bg)]/95 backdrop-blur-2xl md:hidden">
          <div className="mx-auto flex max-w-7xl flex-col gap-1 px-4 py-3">
            {navLinks.map((item) => (
              <Link
                key={item.href}
                href={item.href}
                onClick={() => setMobileOpen(false)}
                className="rounded-lg px-3 py-2 text-sm text-[var(--rs-muted)] hover:bg-[var(--rs-glass)] hover:text-[var(--rs-ink)]"
              >
                {item.label}
              </Link>
            ))}
            <div className="mt-2 grid grid-cols-2 gap-2">
              <Button asChild variant="secondary" size="sm">
                <Link href={routes.auth.login} onClick={() => setMobileOpen(false)}>
                  Giriş Yap
                </Link>
              </Button>
              <Button asChild size="sm">
                <Link href={routes.auth.register} onClick={() => setMobileOpen(false)}>
                  Üye Ol
                </Link>
              </Button>
            </div>
          </div>
        </div>
      ) : null}
    </nav>
  );
}

function LogoMark() {
  return (
    <svg
      aria-hidden
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={2.2}
      strokeLinecap="round"
      strokeLinejoin="round"
      className="h-4 w-4 text-white"
    >
      <path d="M3 19V5a2 2 0 0 1 2-2h6v16" />
      <path d="M21 19V5a2 2 0 0 0-2-2h-6v16" />
      <path d="M3 19h18" />
    </svg>
  );
}
