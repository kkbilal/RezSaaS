import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { getBusinessAppointmentInbox } from "@/features/business/api/get-appointment-inbox";
import { getBusinessContext } from "@/features/business/api/get-business-context";
import { BusinessRequestInboxPage } from "@/features/business/components/business-request-inbox-page";
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
  title: "Talep Kutusu — İşletme Paneli"
};

type Props = {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

export default async function BusinessRequestsRoute({ searchParams }: Props) {
  const sessionState = await requireSession(routes.business.requests);

  if (sessionState.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.auth.login}
        actionLabel="Giriş ekranına git"
        description={`${sessionState.reason} Talep kutusu render edilmedi.`}
        title="Oturum doğrulanamadı"
      />
    );
  }

  const context = await getBusinessContext();

  if (context.kind === "unauthenticated") {
    redirect(withReturnTo(routes.auth.login, routes.business.requests));
  }

  if (context.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.business.panel}
        actionLabel="Panele dön"
        description={`${context.reason} Talep kutusu render edilmedi.`}
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

  const inbox = await getBusinessAppointmentInbox(tenant);

  // Rozet sunucudaki taze veriden gelir; istemcideki iyimser durum degisiklikleri
  // router.refresh() ile bu sayiyi da tazeler.
  const pendingCount =
    inbox.kind === "ready"
      ? inbox.requests.filter(
          (request) => (request.status ?? "Unknown") === "PendingApproval"
        ).length
      : 0;

  return (
    <PanelShell
      capabilities={tenant.capabilities ?? []}
      currentTenantId={tenant.tenantId}
      pendingRequestCount={pendingCount}
      sessionEmail={sessionState.session.account?.email ?? "Oturum"}
      tenants={buildPanelTenants(context.tenants)}
    >
      <BusinessRequestInboxPage
        inbox={inbox}
        tenantId={tenant.tenantId ?? null}
      />
    </PanelShell>
  );
}
