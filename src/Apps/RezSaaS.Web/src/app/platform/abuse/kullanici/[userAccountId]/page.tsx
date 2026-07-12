import { redirect } from "next/navigation";
import { requireSession } from "@/features/session/lib/guards";
import { routes } from "@/shared/config/routes";
import { getPlatformUserAbuseOverview } from "@/features/platform/api/get-platform-user-abuse-overview";
import { PlatformUserAbuseOverviewPage } from "@/features/platform/components/platform-user-abuse-overview-page";
import { PlatformShell } from "@/features/platform/components/platform-shell";
import { PlatformStepUpGate } from "@/features/platform/components/platform-step-up-gate";
import { PrivateRouteState } from "@/features/session/components/private-route-state";

export const dynamic = "force-dynamic";

type Params = Promise<{
  userAccountId: string;
}>;

export default async function AbuseUserPage(props: { params: Params }) {
  const { userAccountId } = await props.params;
  const sessionState = await requireSession(routes.platform.abuseUser(userAccountId));

  if (sessionState.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.auth.login}
        actionLabel="Giriş ekranına git"
        description={`${sessionState.reason} Kullanıcı abuse özeti render edilmedi.`}
        title="Oturum doğrulanamadı"
      />
    );
  }

  const { session } = sessionState;
  const sessionEmail = session.account?.email ?? "Platform hesabı";

  if (!session.platformRoles?.includes("PlatformAdmin")) {
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
    return <PlatformStepUpGate sessionEmail={sessionEmail} />;
  }

  const overviewState = await getPlatformUserAbuseOverview(userAccountId);

  if (overviewState.kind === "forbidden") {
    return <PlatformStepUpGate sessionEmail={sessionEmail} />;
  }

  if (overviewState.kind === "unavailable") {
    return (
      <div className="p-6">
        <p className="rounded-2xl border border-dashed border-[var(--rs-border)] bg-[var(--rs-glass)] p-4 text-sm text-[var(--rs-muted)]">
          {overviewState.reason}
        </p>
      </div>
    );
  }

  return (
    <PlatformShell
      sessionEmail={sessionEmail}
      stepUpExpiresAtUtc={session.stepUp?.expiresAtUtc}
    >
      <PlatformUserAbuseOverviewPage overview={overviewState.overview} />
    </PlatformShell>
  );
}

