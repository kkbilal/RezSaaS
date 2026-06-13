import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { getPrimaryBusinessTenant } from "@/features/business/api/get-appointment-inbox";
import { getBusinessContext } from "@/features/business/api/get-business-context";
import { getBusinessSettingsOverview } from "@/features/business/api/get-business-settings-overview";
import { BusinessSettingsPage } from "@/features/business/components/business-settings-page";
import { PrivateRouteState } from "@/features/session/components/private-route-state";
import { requireSession } from "@/features/session/lib/guards";
import { routes, withReturnTo } from "@/shared/config/routes";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  robots: {
    index: false
  },
  title: "İşletme Ayarları"
};

export default async function BusinessSettingsRoute() {
  const sessionState = await requireSession(routes.business.settings);

  if (sessionState.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.auth.login}
        actionLabel="Giriş ekranına git"
        description={`${sessionState.reason} İşletme ayarları render edilmedi.`}
        title="Oturum doğrulanamadı"
      />
    );
  }

  const context = await getBusinessContext();

  if (context.kind === "unauthenticated") {
    redirect(withReturnTo(routes.auth.login, routes.business.settings));
  }

  if (context.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.public.home}
        actionLabel="Ana sayfaya dön"
        description={`${context.reason} Hesabına bağlı işletme yetkileri doğrulanmadan ayar snapshot'ı açılmaz.`}
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
        description="Bu hesap için aktif işletme yetkisi görünmüyor. Yetki tanımlandıktan sonra ayar snapshot'ı açılır."
        title="Aktif işletme üyeliği yok"
      />
    );
  }

  const overviewState = await getBusinessSettingsOverview(tenant);

  if (overviewState.kind !== "ready") {
    return (
      <PrivateRouteState
        actionHref={routes.business.panel}
        actionLabel="Operasyon paneline dön"
        description={overviewState.reason}
        eyebrow="İşletme yönetimi"
        title={
          overviewState.kind === "not-published"
            ? "Public profil yayınlanmamış"
            : "İşletme ayarları yüklenemedi"
        }
      />
    );
  }

  return (
    <BusinessSettingsPage
      overview={overviewState.overview}
      sessionEmail={sessionState.session.account?.email ?? "Oturum"}
    />
  );
}
