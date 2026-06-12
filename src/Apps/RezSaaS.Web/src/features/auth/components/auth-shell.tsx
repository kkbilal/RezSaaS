import Link from "next/link";
import type { ReactNode } from "react";
import { routes } from "@/shared/config/routes";
import { Card, CardDescription, CardHeader, CardTitle } from "@/shared/ui/card";

type AuthShellProps = {
  children: ReactNode;
  description: string;
  footer?: ReactNode;
  title: string;
};

export function AuthShell({ children, description, footer, title }: AuthShellProps) {
  return (
    <main className="studio-grid min-h-screen px-4 py-8 sm:px-6">
      <div className="mx-auto grid min-h-[calc(100vh-4rem)] max-w-6xl items-center gap-8 lg:grid-cols-[1fr_29rem]">
        <section className="fade-up max-w-2xl space-y-7">
          <Link
            className="inline-flex rounded-full border border-[var(--rs-border)] bg-white/70 px-4 py-2 text-sm text-[var(--rs-muted)] shadow-[var(--rs-shadow-soft)] transition hover:text-[var(--rs-ink)]"
            href={routes.public.home}
          >
            RezSaaS
          </Link>
          <div className="space-y-5">
            <h1 className="text-5xl font-semibold tracking-[-0.07em] text-[var(--rs-ink)] sm:text-7xl">
              Tek giriş, doğru panel.
            </h1>
            <p className="max-w-xl text-lg leading-8 text-[var(--rs-muted-strong)]">
              Hesabına giriş yap; müşteri, işletme veya platform yetkilerine göre
              göreceğin ekranlar otomatik belirlenir.
            </p>
          </div>
        </section>

        <Card className="fade-up p-6 [animation-delay:90ms] sm:p-8">
          <CardHeader>
            <CardTitle>{title}</CardTitle>
            <CardDescription>{description}</CardDescription>
          </CardHeader>
          <div className="mt-6">{children}</div>
          {footer ? <div className="mt-6 text-sm text-[var(--rs-muted)]">{footer}</div> : null}
        </Card>
      </div>
    </main>
  );
}
