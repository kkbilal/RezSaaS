import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { PrivateRouteState } from "@/features/session/components/private-route-state";
import { requireSession } from "@/features/session/lib/guards";
import { routes, withReturnTo } from "@/shared/config/routes";
import { BusinessStaffManagementPage } from "@/features/business/components/business-staff-management-page";
import { getBusinessBranchesServer } from "@/features/business/api/get-business-branches-server";
import { listStaff } from "@/features/business/api/business-staff-client";
import { getBusinessContext } from "@/features/business/api/get-business-context";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  robots: { index: false },
  title: "Personel Yönetimi"
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
        description={`${sessionState.reason} Personel yönetimi render edilmedi.`}
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
        actionHref={routes.auth.login}
        actionLabel="Giriş ekranına git"
        description={`${context.reason} Personel yönetimi render edilmedi.`}
        title="İşletme bilgisi alınamadı"
      />
    );
  }

  const [branches] = await Promise.all([
    getBusinessBranchesServer()
  ]);

  const initialStaff = branches.length > 0
    ? await listStaff(branches[0].id)
    : [];

  return (
    <BusinessStaffManagementPage
      branches={branches}
      initialStaff={initialStaff}
    />
  );
}
