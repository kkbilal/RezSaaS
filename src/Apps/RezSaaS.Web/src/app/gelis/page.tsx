import { redirect } from "next/navigation";
import { getSessionState } from "@/features/session/api/get-session-bootstrap";
import { routes } from "@/shared/config/routes";

export const dynamic = "force-dynamic";

// FAIL-CLOSED ROL DAGITIMI (bkz. docs/29)
//
// Bir rolu buraya eklemek, o kullaniciyi bir kabuga ATMAK demektir. Backend'de o kabugun
// uclarina erisemeyen bir rolu buraya eklemek, her ogesi 403 tuzagi olan bir menu gostermektir.
// Bu yuzden asagidaki iki liste, backend'in GERCEK yetkilerine gore daraltilmistir:
//
// - "Staff" CIKARILDI: BusinessContextComposer bu role BOS capability dizisi veriyor ve
//   TenantBookingAuthorizationService'in uc metodu da Staff'i reddediyor. Panele girse
//   kendi takvimini bile goremez -> her /api/business/* cagrisi 403. Musteri kabuguna duser.
// - "PlatformSupport" CIKARILDI: PlatformSupportOrAdmin policy'si tanimli ama repo genelinde
//   HICBIR endpoint'e bagli degil. /platform'a atilirsa "yetki yok" ekraninda kisir donguye
//   girer. Musteri kabuguna duser.
//
// V2'de bu roller backend'de gercek yetki kazanirsa buraya geri eklenecek.
const BUSINESS_ROLES = ["BusinessOwner", "BranchManager"];
const PLATFORM_ROLES = ["PlatformAdmin"];

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
      // Abuse/moderasyon suruyeni MVP disi (docs/29). Platform admin'in MVP'deki isi
      // tenant provisioning: yeni salon ac + BusinessOwner uyeligi ver.
      return routes.platform.tenants;
    case "business":
      return routes.business.panel;
    case "customer":
      return routes.customer.appointments;
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