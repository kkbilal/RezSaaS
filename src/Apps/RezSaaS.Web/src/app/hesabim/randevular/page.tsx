import type { Metadata } from "next";
import { getCustomerAppointmentHistory } from "@/features/customer/api/get-appointment-history";
import { CustomerAppointmentsPage } from "@/features/customer/components/customer-appointments-page";
import { CustomerShell } from "@/features/customer/components/customer-shell";
import { PrivateRouteState } from "@/features/session/components/private-route-state";
import { requireSession } from "@/features/session/lib/guards";
import { routes } from "@/shared/config/routes";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  robots: {
    index: false
  },
  title: "Randevularım"
};

// Musterinin BIRINCIL sayfasi (eskiden /hesabim/talepler'e yonlenen bir stub'di).
// TEK cagri (appointment-history) hem talepleri hem randevulari doner; ayrim client'ta
// "Yaklasan | Gecmis" olarak yapilir -- ItemType bir sekme degil, bir rozettir.
export default async function CustomerAppointmentsRoute() {
  const sessionState = await requireSession(routes.customer.appointments);

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
        eyebrow="Randevularım"
        title="Randevularınız yüklenemedi"
      />
    );
  }

  return (
    <CustomerShell
      activeNav="appointments"
      sessionEmail={sessionState.session.account?.email ?? "Hesabım"}
    >
      {/* nowUtc sunucudan gecer: SSR ile ilk client render'i birebir ayni kalsin. */}
      <CustomerAppointmentsPage
        items={history.items}
        nowUtc={new Date().toISOString()}
      />
    </CustomerShell>
  );
}
