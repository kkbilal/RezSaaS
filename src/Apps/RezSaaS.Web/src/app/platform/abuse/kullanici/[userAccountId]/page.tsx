import { redirect } from "next/navigation";
import { requireSession } from "@/features/session/lib/require-session";
import { routes } from "@/shared/config/routes";
import { getPlatformUserAbuseOverview } from "@/features/platform/api/get-platform-user-abuse-overview";
import { PlatformUserAbuseOverviewPage } from "@/features/platform/components/platform-user-abuse-overview-page";
import { PlatformStepUpGate } from "@/features/platform/components/platform-step-up-gate";

export const dynamic = "force-dynamic";

type Params = Promise<{
  userAccountId: string;
}>;

export default async function AbuseUserPage(props: { params: Params }) {
  const { userAccountId } = await props.params;
  const sessionState = await requireSession(routes.platform.abuse);

  if (sessionState.kind === "unauthenticated") {
    redirect(routes.auth.login);
  }

  if (sessionState.kind === "unavailable") {
    return <PlatformStepUpGate sessionEmail="" />;
  }

  const { session } = sessionState;

  if (!session.platformRoles?.includes("PlatformAdmin")) {
    redirect(routes.public.home);
  }

  if (!session.stepUp?.isSatisfied) {
    return <PlatformStepUpGate sessionEmail={session.email} />;
  }

  const overviewState = await getPlatformUserAbuseOverview(userAccountId);

  if (overviewState.kind === "forbidden") {
    return <PlatformStepUpGate sessionEmail={session.email} />;
  }

  if (overviewState.kind === "unavailable") {
    return (
      <main className="studio-grid min-h-screen px-4 py-6 sm:px-8">
        <div className="mx-auto max-w-xl space-y-6">
          <p className="rounded-2xl border border-dashed border-[var(--rs-border)] bg-white/60 p-4 text-sm text-[var(--rs-muted)]">
            {overviewState.reason}
          </p>
        </div>
      </main>
    );
  }

  return (
    <PlatformUserAbuseOverviewPage
      overview={overviewState.overview}
      sessionEmail={session.email}
      stepUpExpiresAtUtc={session.stepUp?.expiresAtUtc}
    />
  );
}
