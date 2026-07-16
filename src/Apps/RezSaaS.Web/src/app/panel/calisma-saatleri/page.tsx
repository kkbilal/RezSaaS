import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { getBusinessBranchesServer } from "@/features/business/api/get-business-branches-server";
import { getBusinessContext } from "@/features/business/api/get-business-context";
import { BusinessWorkingHoursPage } from "@/features/business/components/business-working-hours-page";
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
  title: "Çalışma Saatleri — İşletme Paneli"
};

type Props = {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

export default async function BusinessWorkingHoursRoute({ searchParams }: Props) {
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
        actionHref={routes.business.panel}
        actionLabel="Panele dön"
        description={`${context.reason} Çalışma saati yönetimi render edilmedi.`}
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

  // Calisma saatleri SUBE ALTINDA: izgarayi cizmeden once subeler lazim (Tuzak 1).
  const branchesState = await getBusinessBranchesServer(tenant);
  const sessionEmail = sessionState.session.account?.email ?? "Oturum";
  const tenantId = tenant.tenantId;

  return (
    <PanelShell
      capabilities={tenant.capabilities ?? []}
      currentTenantId={tenantId}
      sessionEmail={sessionEmail}
      tenants={buildPanelTenants(context.tenants)}
    >
      {branchesState.kind === "unavailable" || !tenantId ? (
        <PrivateRouteState
          actionHref={routes.business.panel}
          actionLabel="Panele dön"
          description={`${
            branchesState.kind === "unavailable"
              ? branchesState.reason
              : "İşletme bilgisi doğrulanamadı."
          } Çalışma saatleri render edilmedi.`}
          eyebrow="İşletme paneli"
          title="Şubeler alınamadı"
        />
      ) : (
        <BusinessWorkingHoursPage
          branches={branchesState.branches}
          tenantId={tenantId}
        />
      )}
    </PanelShell>
  );
}
