import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { PrivateRouteState } from "@/features/session/components/private-route-state";
import { requireSession } from "@/features/session/lib/guards";
import { routes, withReturnTo } from "@/shared/config/routes";
import { BusinessSkillManagementPage } from "@/features/business/components/business-skill-management-page";
import { getBusinessSkillsServer } from "@/features/business/api/get-business-skills-server";
import { getBusinessContext } from "@/features/business/api/get-business-context";
import {
  firstSearchParam,
  selectBusinessTenant
} from "@/features/business/lib/business-tenant-selection";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  robots: { index: false },
  title: "Yetenek Yönetimi"
};

type Props = {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

export default async function BusinessSkillsRoute({ searchParams }: Props) {
  const sessionState = await requireSession(routes.business.skills);

  if (sessionState.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.auth.login}
        actionLabel="Giriş ekranına git"
        description={`${sessionState.reason} Yetenek yönetimi render edilmedi.`}
        title="Oturum doğrulanamadı"
      />
    );
  }

  const context = await getBusinessContext();

  if (context.kind === "unauthenticated") {
    redirect(withReturnTo(routes.auth.login, routes.business.skills));
  }

  if (context.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.auth.login}
        actionLabel="Giriş ekranına git"
        description={`${context.reason} Yetenek yönetimi render edilmedi.`}
        title="İşletme bilgisi alınamadı"
      />
    );
  }

  const initialSkills = await getBusinessSkillsServer();

  return <BusinessSkillManagementPage initialSkills={initialSkills} />;
}
