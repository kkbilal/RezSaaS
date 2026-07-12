import type { Metadata } from "next";
import { getCustomerAppointmentHistory } from "@/features/customer/api/get-appointment-history";
import { CustomerRequestsPage } from "@/features/customer/components/customer-requests-page";
import { CustomerShell } from "@/features/customer/components/customer-shell";
import { PrivateRouteState } from "@/features/session/components/private-route-state";
import { requireSession } from "@/features/session/lib/guards";
import { routes } from "@/shared/config/routes";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  robots: {
    index: false
  },
  title: "Taleplerim"
};

export default async function CustomerRequestsRoute() {
  const sessionState = await requireSession(routes.customer.requests);

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

  const history = await getCustomerAppointmentHistory();

  if (history.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.public.discover}
        actionLabel="Keşfe dön"
        description={history.reason}
        eyebrow="Taleplerim"
        title="Rezervasyon geçmişi yüklenemedi"
      />
    );
  }

  return (
    <CustomerShell
      activeNav="requests"
      sessionEmail={sessionState.session.account?.email ?? "Hesabım"}
    >
      <CustomerRequestsPage items={history.items} />
    </CustomerShell>
  );
}
