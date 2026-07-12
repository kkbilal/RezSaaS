import type { Metadata } from "next";
import {
  getPlatformTenantsOverview,
  type PlatformTenantFilters
} from "@/features/platform/api/get-platform-tenants-overview";
import { PlatformShell } from "@/features/platform/components/platform-shell";
import { PlatformStepUpGate } from "@/features/platform/components/platform-step-up-gate";
import { PlatformTenantsPage } from "@/features/platform/components/platform-tenants-page";
import { PrivateRouteState } from "@/features/session/components/private-route-state";
import { requireSession } from "@/features/session/lib/guards";
import { routes } from "@/shared/config/routes";

type PlatformTenantsRouteProps = {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  robots: {
    index: false
  },
  title: "Platform Tenantlar"
};

export default async function PlatformTenantsRoute({
  searchParams
}: PlatformTenantsRouteProps) {
  const sessionState = await requireSession(routes.platform.tenants);

  if (sessionState.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.auth.login}
        actionLabel="Giriş ekranına git"
        description={`${sessionState.reason} Tenant verisi render edilmedi.`}
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

  const filters = normalizeTenantFilters(await searchParams);
  const overviewState = await getPlatformTenantsOverview(filters);

  if (overviewState.kind !== "ready") {
    return (
      <PrivateRouteState
        actionHref={routes.platform.tenants}
        actionLabel="Tekrar dene"
        description={overviewState.reason}
        eyebrow="Platform Control-plane"
        title={
          overviewState.kind === "forbidden"
            ? "Step-up veya platform yetkisi doğrulanamadı"
            : "Tenant verisi yüklenemedi"
        }
      />
    );
  }

  return (
    <PlatformShell
      sessionEmail={session.account?.email ?? "Platform hesabı"}
      stepUpExpiresAtUtc={session.stepUp.expiresAtUtc}
    >
      <PlatformTenantsPage overview={overviewState.overview} />
    </PlatformShell>
  );
}

function normalizeTenantFilters(
  params: Record<string, string | string[] | undefined>
): PlatformTenantFilters {
  return {
    search: first(params.search),
    status: first(params.status),
    tenantId: first(params.tenantId)
  };
}

function first(value?: string | string[]) {
  return Array.isArray(value) ? value[0] : value;
}
