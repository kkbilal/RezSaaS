import type { Metadata } from "next";
import { redirect } from "next/navigation";
import {
  firstSearchParam,
  selectBusinessTenant
} from "@/features/business/lib/business-tenant-selection";
import { getBusinessContext } from "@/features/business/api/get-business-context";
import { PrivateRouteState } from "@/features/session/components/private-route-state";
import { requireSession } from "@/features/session/lib/guards";
import { routes, withReturnTo } from "@/shared/config/routes";
import { Card, CardDescription, CardHeader, CardTitle } from "@/shared/ui/card";
import { EmptyState } from "@/shared/ui/empty-state";
import { PanelShell } from "@/features/business/components/panel-shell";
import { buildPanelTenants } from "@/features/business/lib/panel-tenants";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  robots: { index: false },
  title: "Kaynak Türleri — İşletme Paneli"
};

type Props = {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

export default async function BusinessResourceTypesRoute({
  searchParams
}: Props) {
  const sessionState = await requireSession(routes.business.resourceTypes);

  if (sessionState.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.auth.login}
        actionLabel="Giriş ekranına git"
        description={`${sessionState.reason} Kaynak türü yönetimi render edilmedi.`}
        title="Oturum doğrulanamadı"
      />
    );
  }

  const context = await getBusinessContext();

  if (context.kind === "unauthenticated") {
    redirect(withReturnTo(routes.auth.login, routes.business.resourceTypes));
  }

  if (context.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.business.panel}
        actionLabel="Panele dön"
        description={`${context.reason} Kaynak türü yönetimi render edilmedi.`}
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

  const sessionEmail = sessionState.session.account?.email ?? "Oturum";
    return (
    <PanelShell
      capabilities={tenant.capabilities ?? []}
      currentTenantId={tenant.tenantId}
      sessionEmail={sessionEmail}
      tenants={buildPanelTenants(context.tenants)}
    >
      <div className="space-y-6">
        <section>
          <p className="w-fit rounded-full bg-[var(--rs-accent-soft)] px-4 py-2 text-xs font-medium uppercase tracking-[0.18em] text-[var(--rs-accent-strong)]">
            Kaynak türleri
          </p>
          <h1 className="mt-4 text-4xl font-semibold tracking-[-0.07em] text-[var(--rs-ink)] sm:text-5xl">
            Kaynak türleri
          </h1>
        </section>
        <Card className="p-6 sm:p-8">
          <CardHeader>
            <CardTitle>Hazırlık durumu</CardTitle>
            <CardDescription>
              Kaynak türü (chair, room, bed, station vb.) yönetimi Phase 5a
              backend endpoint'leriyle birlikte açılır.
            </CardDescription>
          </CardHeader>
          <EmptyState
            description="Kaynak türü oluşturma ve güncelleme akışları Phase 5a kapsamında yayınlanacak."
            title="Kaynak türü yönetimi yakında"
          />
        </Card>
      </div>
    </PanelShell>
  );
}
