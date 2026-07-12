import type { Metadata } from "next";
import {
  getPlatformAppealsOverview,
  type PlatformAppealsFilters
} from "@/features/platform/api/get-platform-appeals-overview";
import { PlatformAppealsPage } from "@/features/platform/components/platform-appeals-page";
import { PlatformShell } from "@/features/platform/components/platform-shell";
import { PlatformStepUpGate } from "@/features/platform/components/platform-step-up-gate";
import { PrivateRouteState } from "@/features/session/components/private-route-state";
import { requireSession } from "@/features/session/lib/guards";
import { routes } from "@/shared/config/routes";

type PlatformAppealsRouteProps = {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  robots: {
    index: false
  },
  title: "Platform İtirazlar"
};

export default async function PlatformAppealsRoute({
  searchParams
}: PlatformAppealsRouteProps) {
  const sessionState = await requireSession(routes.platform.appeals);

  if (sessionState.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.auth.login}
        actionLabel="Giriş ekranına git"
        description={`${sessionState.reason} İtiraz verisi render edilmedi.`}
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

  const filters = normalizeAppealsFilters(await searchParams);
  const overviewState = await getPlatformAppealsOverview(filters);

  if (overviewState.kind !== "ready") {
    return (
      <PrivateRouteState
        actionHref={routes.platform.appeals}
        actionLabel="Tekrar dene"
        description={overviewState.reason}
        eyebrow="Platform Control-plane"
        title={
          overviewState.kind === "forbidden"
            ? "Step-up veya platform yetkisi doğrulanamadı"
            : "İtiraz verisi yüklenemedi"
        }
      />
    );
  }

  return (
    <PlatformShell
      sessionEmail={session.account?.email ?? "Platform hesabı"}
      stepUpExpiresAtUtc={session.stepUp.expiresAtUtc}
    >
      <PlatformAppealsPage overview={overviewState.overview} />
    </PlatformShell>
  );
}

function normalizeAppealsFilters(
  params: Record<string, string | string[] | undefined>
): PlatformAppealsFilters {
  return {
    appealId: first(params.appealId),
    appealStatus: first(params.appealStatus),
    closureCaseId: first(params.closureCaseId),
    closureStatus: first(params.closureStatus),
    userAccountId: first(params.userAccountId)
  };
}

function first(value?: string | string[]) {
  return Array.isArray(value) ? value[0] : value;
}
