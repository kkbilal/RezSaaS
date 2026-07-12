import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { getBusinessAppointments } from "@/features/business/api/get-business-appointments";
import { getBusinessContext } from "@/features/business/api/get-business-context";
import { BusinessCalendarPage } from "@/features/business/components/business-calendar-page";
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
  title: "Takvim — İşletme Paneli"
};

type CalendarRouteProps = {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

export default async function BusinessCalendarRoute({
  searchParams
}: CalendarRouteProps) {
  const sessionState = await requireSession(routes.business.calendar);

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
    redirect(withReturnTo(routes.auth.login, routes.business.calendar));
  }

  if (context.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.business.panel}
        actionLabel="Panele dön"
        description={`${context.reason} İşletme yetkileri doğrulanmadan takvim açılmaz.`}
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
        title="Takvim yüklenemedi"
      />
    );
  }

  const branchTimeZoneId =
    appointmentSchedule.appointments.find(
      (appointment) => Boolean(appointment.branchTimeZoneId)
    )?.branchTimeZoneId ?? "Europe/Istanbul";

  return (
    <PanelShell
      capabilities={tenant.capabilities ?? []}
      currentTenantId={tenant.tenantId}
      sessionEmail={sessionState.session.account?.email ?? "Oturum"}
      tenants={buildPanelTenants(context.tenants)}
    >
      <BusinessCalendarPage
        appointments={appointmentSchedule.appointments}
        branchTimeZoneId={branchTimeZoneId}
        tenant={appointmentSchedule.tenant}
      />
    </PanelShell>
  );
}
