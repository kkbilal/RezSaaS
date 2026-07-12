import type { Metadata } from "next";
import { PlatformShell } from "@/features/platform/components/platform-shell";
import { PlatformStepUpGate } from "@/features/platform/components/platform-step-up-gate";
import { PrivateRouteState } from "@/features/session/components/private-route-state";
import { requireSession } from "@/features/session/lib/guards";
import { routes } from "@/shared/config/routes";
import { Button } from "@/shared/ui/button";
import { Card, CardDescription, CardHeader, CardTitle } from "@/shared/ui/card";
import { EmptyState } from "@/shared/ui/empty-state";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  robots: {
    index: false
  },
  title: "Tenant üyelik — Platform"
};

type TenantMembershipRouteProps = {
  params: Promise<{ tenantId: string }>;
};

export default async function PlatformTenantMembershipRoute({
  params
}: TenantMembershipRouteProps) {
  const { tenantId } = await params;
  const sessionState = await requireSession(routes.platform.tenantMembers(tenantId));

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
      activeNav="tenants"
      sessionEmail={session.account?.email ?? "Platform hesabı"}
      stepUpExpiresAtUtc={session.stepUp.expiresAtUtc}
    >
      <div className="space-y-6">
        <section className="fade-up">
          <p className="w-fit rounded-full bg-[var(--rs-accent-soft)] px-4 py-2 text-xs font-medium uppercase tracking-[0.18em] text-[var(--rs-accent-strong)]">
            Tenant üyelik yönetimi
          </p>
          <h1 className="mt-4 text-4xl font-semibold tracking-[-0.07em] text-[var(--rs-ink)] sm:text-5xl">
            Üyelikler · {tenantId}
          </h1>
          <p className="mt-3 max-w-2xl text-base leading-7 text-[var(--rs-muted-strong)]">
            Tenant membership add/suspend/revoke endpoint&apos;leri{" "}
            {`PlatformAdminWithStepUp`} policy&apos;si altında hazır olmadan bu ekran
            publish edilmez (AGENTS.md §6.6). Mock veri veya simülasyon taşımaz.
          </p>
        </section>

        <Card className="p-6 sm:p-8">
          <CardHeader>
            <CardTitle>Hazırlık durumu</CardTitle>
            <CardDescription>
              Aktif UserAccount doğrulaması, {`Revoked`} terminal durumu ve audit
              adımları backend tarafında tamamlandığında açılacak.
            </CardDescription>
          </CardHeader>
          <EmptyState
            description="Membership listesi, add/suspend/revoke akışları ve son aktif BusinessOwner koruması endpoint'leri hazır olduğunda bu ekrandan yayınlanacak. Şu an için mock simülasyon kaldırıldı."
            title="Üyelik yönetimi yakında"
          />
          <div className="mt-6">
            <Button asChild variant="secondary">
              <a href={routes.platform.tenants}>Tenant listesine dön</a>
            </Button>
          </div>
        </Card>
      </div>
    </PlatformShell>
  );
}
