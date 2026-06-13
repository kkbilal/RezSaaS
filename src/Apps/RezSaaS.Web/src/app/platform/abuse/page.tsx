import type { Metadata } from "next";
import { getPlatformAbuseOverview } from "@/features/platform/api/get-platform-abuse-overview";
import { PlatformAbusePage } from "@/features/platform/components/platform-abuse-page";
import { PlatformStepUpGate } from "@/features/platform/components/platform-step-up-gate";
import { PrivateRouteState } from "@/features/session/components/private-route-state";
import { requireSession } from "@/features/session/lib/guards";
import { routes } from "@/shared/config/routes";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  robots: {
    index: false
  },
  title: "Platform Abuse"
};

export default async function PlatformAbuseRoute() {
  const sessionState = await requireSession(routes.platform.abuse);

  if (sessionState.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.auth.login}
        actionLabel="Giriş ekranına git"
        description={`${sessionState.reason} Platform verisi render edilmedi.`}
        title="Platform oturumu doğrulanamadı"
      />
    );
  }

  const session = sessionState.session;
  const hasPlatformAdminRole = (session.platformRoles ?? []).includes("PlatformAdmin");

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

  const overviewState = await getPlatformAbuseOverview();

  if (overviewState.kind !== "ready") {
    return (
      <PrivateRouteState
        actionHref={routes.platform.abuse}
        actionLabel="Tekrar dene"
        description={overviewState.reason}
        eyebrow="Platform Control-plane"
        title={
          overviewState.kind === "forbidden"
            ? "Step-up veya platform yetkisi doğrulanamadı"
            : "Platform verisi yüklenemedi"
        }
      />
    );
  }

  return (
    <PlatformAbusePage
      overview={overviewState.overview}
      sessionEmail={session.account?.email ?? "Platform hesabı"}
      stepUpExpiresAtUtc={session.stepUp.expiresAtUtc}
    />
  );
}
