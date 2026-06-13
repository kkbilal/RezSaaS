import { cookies } from "next/headers";
import { getPublicBusinessProfile } from "@/features/public-discovery/api/public-businesses";
import type { PublicBusinessProfile } from "@/features/public-discovery/api/public-businesses";
import { createServerApiClient } from "@/shared/api/server-client";
import type { ApiSchema } from "@/shared/api/types";
import type { BusinessTenantContext } from "./get-business-context";

export type BusinessProfileSettings =
  ApiSchema<"BusinessProfileSettingsResponse">;

export type BusinessProfileSettingsState =
  | {
      kind: "ready";
      profile: BusinessProfileSettings;
    }
  | {
      kind: "forbidden";
      reason: string;
    }
  | {
      kind: "unavailable";
      reason: string;
    }
  | {
      kind: "unsupported";
      reason: string;
    };

export type BusinessSettingsOverview = {
  profile: PublicBusinessProfile;
  profileSettings: BusinessProfileSettingsState;
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
      profileSettings: await getBusinessProfileSettings(tenant),
      tenant
    }
  };
}

async function getBusinessProfileSettings(
  tenant: BusinessTenantContext
): Promise<BusinessProfileSettingsState> {
  const canManageSettings = (tenant.capabilities ?? []).includes(
    "business.settings.manage"
  );

  if (!canManageSettings) {
    return {
      kind: "unsupported",
      reason:
        "Bu kullanıcı tenant-wide business settings capability taşımıyor. Profil formu read-only kalır."
    };
  }

  if (!tenant.tenantId) {
    return {
      kind: "unavailable",
      reason: "Tenant context doğrulanamadığı için profil ayarları okunamadı."
    };
  }

  try {
    const cookieHeader = (await cookies()).toString();
    const { data, response } = await createServerApiClient(
      cookieHeader,
      tenant.tenantId
    ).GET("/api/business/settings/profile");

    if (response.status === 403) {
      return {
        kind: "forbidden",
        reason: "Bu profil ayarı için BusinessOwner yetkisi gerekiyor."
      };
    }

    if (!response.ok || !data) {
      return {
        kind: "unavailable",
        reason: "Profil ayarları şu anda okunamadı."
      };
    }

    return {
      kind: "ready",
      profile: data
    };
  } catch {
    return {
      kind: "unavailable",
      reason: "Profil ayarları şu anda yüklenemedi."
    };
  }
}
