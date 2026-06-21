import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { PrivateRouteState } from "@/features/session/components/private-route-state";
import { requireSession } from "@/features/session/lib/guards";
import { routes, withReturnTo } from "@/shared/config/routes";
import { getBusinessContext } from "@/features/business/api/get-business-context";
import { getBusinessServicesServer } from "@/features/business/api/get-business-services-server";
import { BusinessServiceManagementPage } from "@/features/business/components/business-service-management-page";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  robots: { index: false },
  title: "Hizmet Yönetimi"
};

type Props = {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

export default async function BusinessServicesRoute({ searchParams }: Props) {
  const sessionState = await requireSession(routes.business.services);

  if (sessionState.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.auth.login}
        actionLabel="Giriş ekranına git"
        description={`${sessionState.reason} Hizmet yönetimi render edilmedi.`}
        title="Oturum doğrulanamadı"
      />
    );
  }

  const context = await getBusinessContext();

  if (context.kind === "unauthenticated") {
    redirect(withReturnTo(routes.auth.login, routes.business.services));
  }

  if (context.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.auth.login}
        actionLabel="Giriş ekranına git"
        description={`${context.reason} Hizmet yönetimi render edilmedi.`}
        title="İşletme bilgisi alınamadı"
      />
    );
  }

  const initialServices = await getBusinessServicesServer();

  return <BusinessServiceManagementPage initialServices={initialServices} />;
}
