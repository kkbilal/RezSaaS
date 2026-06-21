import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { PrivateRouteState } from "@/features/session/components/private-route-state";
import { requireSession } from "@/features/session/lib/guards";
import { routes, withReturnTo } from "@/shared/config/routes";
import { BusinessResourceManagementPage } from "@/features/business/components/business-resource-management-page";
import { getBusinessResourceTypesServer } from "@/features/business/api/get-business-resource-types-server";
import { getBusinessBranchesServer } from "@/features/business/api/get-business-branches-server";
import { getBusinessContext } from "@/features/business/api/get-business-context";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  robots: { index: false },
  title: "Kaynak Yönetimi"
};

export default async function BusinessResourcesRoute() {
  const sessionState = await requireSession(routes.business.resources);

  if (sessionState.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.auth.login}
        actionLabel="Giriş ekranına git"
        description={`${sessionState.reason} Kaynak yönetimi render edilmedi.`}
        title="Oturum doğrulanamadı"
      />
    );
  }

  const context = await getBusinessContext();

  if (context.kind === "unauthenticated") {
    redirect(withReturnTo(routes.auth.login, routes.business.resources));
  }

  if (context.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.auth.login}
        actionLabel="Giriş ekranına git"
        description={`${context.reason} Kaynak yönetimi render edilmedi.`}
        title="İşletme bilgisi alınamadı"
      />
    );
  }

  const [initialBranches, initialResourceTypes] = await Promise.all([
    getBusinessBranchesServer(),
    getBusinessResourceTypesServer()
  ]);

  return (
    <BusinessResourceManagementPage
      initialBranches={initialBranches}
      initialResourceTypes={initialResourceTypes}
    />
  );
}
