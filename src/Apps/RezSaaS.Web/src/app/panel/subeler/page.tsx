import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { BusinessBranchManagementPage } from "@/features/business/components/business-branch-management-page";
import { getBusinessContext } from "@/features/business/api/get-business-context";
import { getBusinessBranchesServer } from "@/features/business/api/get-business-branches-server";
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
  title: "Şube Yönetimi — İşletme Paneli"
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
        actionHref={routes.business.panel}
        actionLabel="Panele dön"
        description={`${context.reason} Şube yönetimi render edilmedi.`}
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

  const branchesState = await getBusinessBranchesServer(tenant);

  if (branchesState.kind !== "ready") {
    return (
      <PrivateRouteState
        actionHref={routes.business.panel}
        actionLabel="Panele dön"
        description={branchesState.reason}
        eyebrow="Şube yönetimi"
        title="Şubeler yüklenemedi"
      />
    );
  }

  // Bos liste durumu artik component icinde ele aliniyor (shadcn empty-state + "Şube ekle" CTA).
  return (
    <PanelShell
      capabilities={tenant.capabilities ?? []}
      currentTenantId={tenant.tenantId}
      sessionEmail={sessionState.session.account?.email ?? "Oturum"}
      tenants={buildPanelTenants(context.tenants)}
    >
      <BusinessBranchManagementPage
        initialBranches={branchesState.branches}
        tenantId={tenant.tenantId ?? ""}
      />
    </PanelShell>
  );
}
