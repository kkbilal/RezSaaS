import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { PrivateRouteState } from "@/features/session/components/private-route-state";
import { requireSession } from "@/features/session/lib/guards";
import { routes, withReturnTo } from "@/shared/config/routes";
import { BusinessBranchManagementPage } from "@/features/business/components/business-branch-management-page";
import { getBusinessBranchesServer } from "@/features/business/api/get-business-branches-server";
import { getBusinessContext } from "@/features/business/api/get-business-context";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  robots: { index: false },
  title: "Şube Yönetimi"
};

type Props = {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

export default async function BusinessBranchesRoute({ searchParams }: Props) {
  const sessionState = await requireSession(routes.business.branches);

  if (sessionState.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.auth.login}
        actionLabel="Giriş ekranına git"
        description={`${sessionState.reason} Şube yönetimi render edilmedi.`}
        title="Oturum doğrulanamadı"
      />
    );
  }

  const context = await getBusinessContext();

  if (context.kind === "unauthenticated") {
    redirect(withReturnTo(routes.auth.login, routes.business.branches));
  }

  if (context.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.auth.login}
        actionLabel="Giriş ekranına git"
        description={`${context.reason} Şube yönetimi render edilmedi.`}
        title="İşletme bilgisi alınamadı"
      />
    );
  }

  const initialBranches = await getBusinessBranchesServer();

  return <BusinessBranchManagementPage initialBranches={initialBranches} />;
}
