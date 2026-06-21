import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { PrivateRouteState } from "@/features/session/components/private-route-state";
import { requireSession } from "@/features/session/lib/guards";
import { routes, withReturnTo } from "@/shared/config/routes";
import { BusinessResourceTypeManagementPage } from "@/features/business/components/business-resource-type-management-page";
import { getBusinessResourceTypesServer } from "@/features/business/api/get-business-resource-types-server";
import { getBusinessContext } from "@/features/business/api/get-business-context";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  robots: { index: false },
  title: "Kaynak Türleri"
};

export default async function BusinessResourceTypesRoute() {
  const sessionState = await requireSession(routes.business.resourceTypes);

  if (sessionState.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.auth.login}
        actionLabel="Giriş ekranına git"
        description={`${sessionState.reason} Kaynak türü yönetimi render edilmedi.`}
        title="Oturum doğrulanamadı"
      />
    );
  }

  const context = await getBusinessContext();

  if (context.kind === "unauthenticated") {
    redirect(withReturnTo(routes.auth.login, routes.business.resourceTypes));
  }

  if (context.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.auth.login}
        actionLabel="Giriş ekranına git"
        description={`${context.reason} Kaynak türü yönetimi render edilmedi.`}
        title="İşletme bilgisi alınamadı"
      />
    );
  }

  const initialResourceTypes = await getBusinessResourceTypesServer();

  return <BusinessResourceTypeManagementPage initialResourceTypes={initialResourceTypes} />;
}
