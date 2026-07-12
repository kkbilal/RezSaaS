import { redirect } from "next/navigation";
import { getSessionState } from "@/features/session/api/get-session-bootstrap";
import { routes } from "@/shared/config/routes";

export const dynamic = "force-dynamic";

const BUSINESS_ROLES = ["BusinessOwner", "BranchManager", "Staff"];
const PLATFORM_ROLES = ["PlatformAdmin", "PlatformSupport"];

type RoleTarget = "platform" | "business" | "customer";

function resolveRoleTarget(session: {
  platformRoles?: string[] | null;
  tenantMemberships?: { role?: string | null }[] | null;
}): RoleTarget {
  const isPlatform =
    session.platformRoles?.some((role) => PLATFORM_ROLES.includes(role)) ?? false;
  if (isPlatform) return "platform";

  const isBusiness =
    session.tenantMemberships?.some(
      (m) => m.role != null && BUSINESS_ROLES.includes(m.role)
    ) ?? false;
  if (isBusiness) return "business";

  return "customer";
}

function routeForRole(target: RoleTarget): string {
  switch (target) {
    case "platform":
      return routes.platform.abuse;
    case "business":
      return routes.business.panel;
    case "customer":
      return routes.customer.requests;
  }
}

export default async function DispatchPage() {
  const state = await getSessionState();

  if (state.kind === "unauthenticated") {
    redirect(routes.auth.login);
  }

  if (state.kind === "unavailable") {
    // If session can't be verified, send to login which will show an error
    redirect(routes.auth.login);
  }

  redirect(routeForRole(resolveRoleTarget(state.session)));
}