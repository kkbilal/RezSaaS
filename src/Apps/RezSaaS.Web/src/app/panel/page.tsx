import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { getBusinessAppointments } from "@/features/business/api/get-business-appointments";
import {
  getBusinessAppointmentInbox,
  getPrimaryBusinessTenant
} from "@/features/business/api/get-appointment-inbox";
import { getBusinessContext } from "@/features/business/api/get-business-context";
import { BusinessPanel } from "@/features/business/components/business-panel";
import { PrivateRouteState } from "@/features/session/components/private-route-state";
import { requireSession } from "@/features/session/lib/guards";
import { routes, withReturnTo } from "@/shared/config/routes";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  title: "İşletme Paneli"
};

export default async function PanelPage() {
  const sessionState = await requireSession(routes.business.panel);

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
    redirect(withReturnTo(routes.auth.login, routes.business.panel));
  }

  if (context.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.public.home}
        actionLabel="Ana sayfaya dön"
        description={`${context.reason} Hesabına bağlı işletme yetkileri doğrulanmadan panel açılmaz.`}
        title="İşletme yetkileri doğrulanamadı"
      />
    );
  }

  const tenant = getPrimaryBusinessTenant(context);

  if (!tenant) {
    return (
      <PrivateRouteState
        actionHref={routes.public.home}
        actionLabel="Ana sayfaya dön"
        description="Bu hesap için aktif işletme yetkisi görünmüyor. Yetki tanımlandıktan sonra panel açılır."
        title="Aktif işletme üyeliği yok"
      />
    );
  }

  const [inbox, appointmentSchedule] = await Promise.all([
    getBusinessAppointmentInbox(tenant),
    getBusinessAppointments(tenant)
  ]);

  return (
    <BusinessPanel
      appointmentSchedule={appointmentSchedule}
      context={context}
      inbox={inbox}
      sessionEmail={sessionState.session.account?.email ?? "Oturum"}
    />
  );
}
