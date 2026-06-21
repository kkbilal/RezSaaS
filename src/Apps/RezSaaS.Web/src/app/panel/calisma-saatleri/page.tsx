import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { PrivateRouteState } from "@/features/session/components/private-route-state";
import { requireSession } from "@/features/session/lib/guards";
import { routes, withReturnTo } from "@/shared/config/routes";
import { BusinessWorkingHoursPage } from "@/features/business/components/business-working-hours-page";
import { getBusinessBranchesServer } from "@/features/business/api/get-business-branches-server";
import { getBusinessContext } from "@/features/business/api/get-business-context";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  robots: { index: false },
  title: "Çalışma Saatleri"
};

export default async function BusinessWorkingHoursRoute() {
  const sessionState = await requireSession(routes.business.workingHours);

  if (sessionState.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.auth.login}
        actionLabel="Giriş ekranına git"
        description={`${sessionState.reason} Çalışma saati yönetimi render edilmedi.`}
        title="Oturum doğrulanamadı"
      />
    );
  }

  const context = await getBusinessContext();

  if (context.kind === "unauthenticated") {
    redirect(withReturnTo(routes.auth.login, routes.business.workingHours));
  }

  if (context.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.auth.login}
        actionLabel="Giriş ekranına git"
        description={`${context.reason} Çalışma saati yönetimi render edilmedi.`}
        title="İşletme bilgisi alınamadı"
      />
    );
  }

  const initialBranches = await getBusinessBranchesServer();

  return <BusinessWorkingHoursPage initialBranches={initialBranches} />;
}
