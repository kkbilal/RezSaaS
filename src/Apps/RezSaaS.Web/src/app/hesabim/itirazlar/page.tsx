import type { Metadata } from "next";
import { CustomerAbusePage } from "@/features/customer/components/customer-abuse-page";
import { CustomerShell } from "@/features/customer/components/customer-shell";
import { getCustomerAbuseOverview } from "@/features/customer/api/get-abuse-overview";
import { PrivateRouteState } from "@/features/session/components/private-route-state";
import { requireSession } from "@/features/session/lib/guards";
import { routes } from "@/shared/config/routes";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  robots: {
    index: false
  },
  title: "İtirazlarım"
};

export default async function CustomerAppealsRoute() {
  const sessionState = await requireSession(routes.customer.appeals);

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

  const abuseOverview = await getCustomerAbuseOverview();

  if (abuseOverview.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.customer.requests}
        actionLabel="Taleplerime dön"
        description={abuseOverview.reason}
        eyebrow="İtirazlarım"
        title="İtiraz bilgileri yüklenemedi"
      />
    );
  }

  return (
    <CustomerShell
      activeNav="appeals"
      sessionEmail={sessionState.session.account?.email ?? "Hesabım"}
    >
      <CustomerAbusePage overview={abuseOverview.overview} />
    </CustomerShell>
  );
}
