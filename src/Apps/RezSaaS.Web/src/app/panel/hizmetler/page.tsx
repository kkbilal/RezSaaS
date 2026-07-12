import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { getBusinessContext } from "@/features/business/api/get-business-context";
import { getBusinessServicesServer } from "@/features/business/api/get-business-services-server";
import { BusinessServicesPage } from "@/features/business/components/business-services-page";
import { PanelShell } from "@/features/business/components/panel-shell";
import {
  firstSearchParam,
  selectBusinessTenant
} from "@/features/business/lib/business-tenant-selection";
import { buildPanelTenants } from "@/features/business/lib/panel-tenants";
import { PrivateRouteState } from "@/features/session/components/private-route-state";
import { requireSession } from "@/features/session/lib/guards";
import { routes, withReturnTo } from "@/shared/config/routes";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  robots: { index: false },
  title: "Hizmetler ve Fiyatlar — İşletme Paneli"
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
        actionHref={routes.business.panel}
        actionLabel="Panele dön"
        description={`${context.reason} Hizmet yönetimi render edilmedi.`}
        eyebrow="İşletme paneli"
        title="İşletme bilgisi alınamadı"
      />
    );
  }

  const tenant = selectBusinessTenant(
    context,
    firstSearchParam((await searchParams).tenantId)
  );

  if (!tenant) {
    return (
      <PrivateRouteState
        actionHref={routes.business.panel}
        actionLabel="Panele dön"
        description="Bu hesap için aktif işletme yetkisi görünmüyor."
        eyebrow="İşletme paneli"
        title="Aktif işletme üyeliği yok"
      />
    );
  }

  const catalog = await getBusinessServicesServer(tenant);
  const sessionEmail = sessionState.session.account?.email ?? "Oturum";

  return (
    <PanelShell
      capabilities={tenant.capabilities ?? []}
      currentTenantId={tenant.tenantId}
      sessionEmail={sessionEmail}
      tenants={buildPanelTenants(context.tenants)}
    >
      {catalog.kind === "unavailable" ? (
        <PrivateRouteState
          actionHref={routes.business.panel}
          actionLabel="Panele dön"
          description={`${catalog.reason} Hizmet listesi render edilmedi.`}
          eyebrow="İşletme paneli"
          title="Hizmetler alınamadı"
        />
      ) : (
        <BusinessServicesPage
          initialServices={catalog.services}
          resourceTypes={catalog.resourceTypes}
          tenantId={catalog.tenantId}
        />
      )}
    </PanelShell>
  );
}
