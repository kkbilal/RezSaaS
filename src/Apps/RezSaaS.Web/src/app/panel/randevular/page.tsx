import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { getBusinessAppointments } from "@/features/business/api/get-business-appointments";
import { getBusinessContext } from "@/features/business/api/get-business-context";
import { BusinessAppointmentListPage } from "@/features/business/components/business-appointment-list-page";
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
  robots: {
    index: false
  },
  title: "Randevular — İşletme Paneli"
};

type AppointmentsRouteProps = {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

export default async function BusinessAppointmentsRoute({
  searchParams
}: AppointmentsRouteProps) {
  const sessionState = await requireSession(routes.business.appointments);

  if (sessionState.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.auth.login}
        actionLabel="Giriş ekranına git"
        description={`${sessionState.reason} Lütfen yeniden giriş yapmayı dene.`}
        title="Oturum doğrulanamadı"
      />
    );
  }

  const context = await getBusinessContext();

  if (context.kind === "unauthenticated") {
    redirect(withReturnTo(routes.auth.login, routes.business.appointments));
  }

  if (context.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.business.panel}
        actionLabel="Panele dön"
        description={`${context.reason} İşletme yetkileri doğrulanmadan randevular açılmaz.`}
        eyebrow="İşletme paneli"
        title="İşletme yetkileri doğrulanamadı"
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

  const appointmentSchedule = await getBusinessAppointments(tenant);

  if (appointmentSchedule.kind !== "ready") {
    return (
      <PrivateRouteState
        actionHref={routes.business.panel}
        actionLabel="Panele dön"
        description={appointmentSchedule.reason}
        eyebrow="İşletme paneli"
        title="Randevular yüklenemedi"
      />
    );
  }

  return (
    <PanelShell
      // capabilities ZORUNLU: gecirilmezse menu fail-open acilirdi.
      capabilities={tenant.capabilities ?? []}
      currentTenantId={tenant.tenantId}
      sessionEmail={sessionState.session.account?.email ?? "Oturum"}
      tenants={buildPanelTenants(context.tenants)}
    >
      <BusinessAppointmentListPage
        appointments={appointmentSchedule.appointments}
        tenantId={tenant.tenantId ?? null}
      />
    </PanelShell>
  );
}
