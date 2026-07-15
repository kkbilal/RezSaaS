import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { getBusinessContext } from "@/features/business/api/get-business-context";
import { getBusinessBranchesServer } from "@/features/business/api/get-business-branches-server";
import { BusinessStaffPage } from "@/features/business/components/business-staff-page";
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
  title: "Ekip — İşletme Paneli"
};

type Props = {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

export default async function BusinessStaffRoute({ searchParams }: Props) {
  const sessionState = await requireSession(routes.business.staff);

  if (sessionState.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.auth.login}
        actionLabel="Giriş ekranına git"
        description={`${sessionState.reason} Ekip yönetimi render edilmedi.`}
        title="Oturum doğrulanamadı"
      />
    );
  }

  const context = await getBusinessContext();

  if (context.kind === "unauthenticated") {
    redirect(withReturnTo(routes.auth.login, routes.business.staff));
  }

  if (context.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.business.panel}
        actionLabel="Panele dön"
        description={`${context.reason} Ekip yönetimi render edilmedi.`}
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

  // Personel SUBE ALTINDA NESTED: listeyi cizebilmek icin once subeler lazim.
  // (Personel ucları branchId ISTER; sube secilmeden personel getirilemez.)
  const branchesState = await getBusinessBranchesServer(tenant);
  const sessionEmail = sessionState.session.account?.email ?? "Oturum";

  // tenantId tipi `string | undefined`; asagidaki ternary'nin ELSE dalinda daralir.
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
          } Ekip listesi render edilmedi.`}
          eyebrow="İşletme paneli"
          title="Şubeler alınamadı"
        />
      ) : (
        <BusinessStaffPage branches={branchesState.branches} tenantId={tenantId} />
      )}
    </PanelShell>
  );
}
