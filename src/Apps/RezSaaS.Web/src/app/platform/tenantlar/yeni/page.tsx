import type { Metadata } from "next";
import { PlatformShell } from "@/features/platform/components/platform-shell";
import { PlatformStepUpGate } from "@/features/platform/components/platform-step-up-gate";
import { PrivateRouteState } from "@/features/session/components/private-route-state";
import { requireSession } from "@/features/session/lib/guards";
import { routes } from "@/shared/config/routes";
import { Card, CardDescription, CardHeader, CardTitle } from "@/shared/ui/card";
import { EmptyState } from "@/shared/ui/empty-state";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  robots: {
    index: false
  },
  title: "Yeni tenant — Platform"
};

export default async function PlatformNewTenantRoute() {
  const sessionState = await requireSession(routes.platform.newTenant);

  if (sessionState.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.auth.login}
        actionLabel="Giriş ekranına git"
        description={`${sessionState.reason} Hiçbir veri render edilmedi.`}
        title="Platform oturumu doğrulanamadı"
      />
    );
  }

  const session = sessionState.session;
  const hasPlatformAdminRole = (session.platformRoles ?? []).includes(
    "PlatformAdmin"
  );

  if (!hasPlatformAdminRole) {
    return (
      <PrivateRouteState
        actionHref={routes.public.home}
        actionLabel="Ana sayfaya dön"
        description="Bu route yalnızca PlatformAdmin rolü olan hesaplar içindir."
        eyebrow="Platform Control-plane"
        title="Platform yetkisi yok"
      />
    );
  }

  if (!session.stepUp?.isSatisfied) {
    return (
      <PlatformStepUpGate
        sessionEmail={session.account?.email ?? "Platform hesabı"}
      />
    );
  }

  return (
    <PlatformShell
      sessionEmail={session.account?.email ?? "Platform hesabı"}
      stepUpExpiresAtUtc={session.stepUp.expiresAtUtc}
    >
      <div className="space-y-6">
        <section className="fade-up">
          <p className="w-fit rounded-full bg-[var(--rs-accent-soft)] px-4 py-2 text-xs font-medium uppercase tracking-[0.18em] text-[var(--rs-accent-strong)]">
            Tenant provisioning
          </p>
          <h1 className="mt-4 text-4xl font-semibold tracking-[-0.07em] text-[var(--rs-ink)] sm:text-5xl">
            Yeni tenant
          </h1>
          <p className="mt-3 max-w-2xl text-base leading-7 text-[var(--rs-muted-strong)]">
            Tenant provisioning endpoint&apos;i {`PlatformAdminWithStepUp`} policy&apos;si
            altında hazır olmadan bu ekran publish edilmez (AGENTS.md §6.6).
            Aşağıdaki form mock veri üretmez; gerçek akış açılana kadar yer
            tutucudur.
          </p>
        </section>

        <Card className="p-6 sm:p-8">
          <CardHeader>
            <CardTitle>Hazırlık durumu</CardTitle>
            <CardDescription>
              Owner doğrulaması, tenant rolü ve audit adımları backend tarafında
              tamamlandığında bu ekrandan açılacak.
            </CardDescription>
          </CardHeader>
          <EmptyState
            description="Slug kontrolü, owner hesap doğrulaması ve tenant provisioning akışı endpoint'leri hazır olduğunda bu ekrandan yayınlanacak. Şu an için mock simülasyon kaldırıldı."
            title="Provisioning yakında"
          />
        </Card>
      </div>
    </PlatformShell>
  );
}
