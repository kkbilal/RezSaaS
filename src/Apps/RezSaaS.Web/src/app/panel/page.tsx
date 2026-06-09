import type { Metadata } from "next";
import { redirect } from "next/navigation";
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
        description={`${sessionState.reason} Session doğrulanmadan işletme paneli render edilmez.`}
        title="Oturum kapısı kullanılamıyor"
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
        description={`${context.reason} Tenant context doğrulanmadan işletme paneli açılmaz.`}
        title="İşletme bağlamı doğrulanamadı"
      />
    );
  }

  const tenant = getPrimaryBusinessTenant(context);

  if (!tenant) {
    return (
      <PrivateRouteState
        actionHref={routes.public.home}
        actionLabel="Ana sayfaya dön"
        description="Bu kullanıcı için aktif BusinessOwner veya BranchManager bağlamı dönmedi. Kullanıcıya serbest tenant GUID seçtirilmez."
        title="Aktif işletme üyeliği yok"
      />
    );
  }

  const inbox = await getBusinessAppointmentInbox(tenant);

  return (
    <BusinessPanel
      context={context}
      inbox={inbox}
      sessionEmail={sessionState.session.account?.email ?? "Oturum"}
    />
  );
}
