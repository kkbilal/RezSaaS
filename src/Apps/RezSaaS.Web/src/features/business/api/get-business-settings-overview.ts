import { getPublicBusinessProfile } from "@/features/public-discovery/api/public-businesses";
import type { PublicBusinessProfile } from "@/features/public-discovery/api/public-businesses";
import type { BusinessTenantContext } from "./get-business-context";

export type BusinessSettingsOverview = {
  profile: PublicBusinessProfile;
  tenant: BusinessTenantContext;
};

export type BusinessSettingsOverviewState =
  | {
      kind: "ready";
      overview: BusinessSettingsOverview;
    }
  | {
      kind: "not-published";
      reason: string;
    }
  | {
      kind: "unavailable";
      reason: string;
    };

export async function getBusinessSettingsOverview(
  tenant: BusinessTenantContext
): Promise<BusinessSettingsOverviewState> {
  if (!tenant.tenantSlug) {
    return {
      kind: "unavailable",
      reason:
        "İşletme slug bilgisi business context içinde dönmediği için public profil snapshot'ı okunamadı."
    };
  }

  const profileState = await getPublicBusinessProfile(tenant.tenantSlug);

  if (profileState.kind === "not-found") {
    return {
      kind: "not-published",
      reason:
        "Bu işletme için public profil henüz yayınlanmış görünmüyor. Yönetim CRUD ekranları açılmadan sahte profil formu gösterilmez."
    };
  }

  if (profileState.kind === "unavailable") {
    return {
      kind: "unavailable",
      reason: profileState.reason
    };
  }

  return {
    kind: "ready",
    overview: {
      profile: profileState.profile,
      tenant
    }
  };
}
