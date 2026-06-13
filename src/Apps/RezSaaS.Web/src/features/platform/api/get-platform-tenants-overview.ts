import { cookies } from "next/headers";
import { createServerApiClient } from "@/shared/api/server-client";
import type { ApiSchema } from "@/shared/api/types";

export type PlatformTenantListItem = ApiSchema<"AdminTenantListItemResponse">;
export type PlatformTenantDetail = ApiSchema<"AdminTenantDetailResponse">;
export type PlatformTenantMembership =
  ApiSchema<"AdminTenantMembershipResponse">;

export type PlatformTenantFilters = {
  search?: string;
  status?: string;
  tenantId?: string;
};

export type PlatformTenantsOverview = {
  filters: PlatformTenantFilters;
  selectedTenant: PlatformTenantDetail | null;
  tenants: PlatformTenantListItem[];
};

export type PlatformTenantsOverviewState =
  | {
      kind: "ready";
      overview: PlatformTenantsOverview;
    }
  | {
      kind: "forbidden";
      reason: string;
    }
  | {
      kind: "unavailable";
      reason: string;
    };

export async function getPlatformTenantsOverview(
  filters: PlatformTenantFilters
): Promise<PlatformTenantsOverviewState> {
  try {
    const normalizedFilters = normalizeFilters(filters);
    const cookieHeader = (await cookies()).toString();
    const client = createServerApiClient(cookieHeader);
    const tenantListResult = await client.GET("/api/admin/tenants", {
      params: {
        query: {
          search: normalizedFilters.search,
          status: normalizedFilters.status,
          take: 50
        }
      }
    });

    if (tenantListResult.response.status === 401) {
      return {
        kind: "forbidden",
        reason: "Platform oturumu doğrulanamadı."
      };
    }

    if (tenantListResult.response.status === 403) {
      return {
        kind: "forbidden",
        reason:
          "Tenant control-plane için PlatformAdmin rolü ve geçerli MFA/step-up oturumu gerekir."
      };
    }

    if (tenantListResult.response.status === 429) {
      return {
        kind: "unavailable",
        reason:
          "Platform operasyon rate limit'i devrede. Kısa süre sonra tekrar denenmeli."
      };
    }

    if (!tenantListResult.response.ok) {
      return {
        kind: "unavailable",
        reason: "Tenant listesi şu anda alınamadı."
      };
    }

    const tenants = tenantListResult.data?.tenants ?? [];
    const selectedTenantId = normalizedFilters.tenantId;
    let selectedTenant: PlatformTenantDetail | null = null;

    if (selectedTenantId) {
      const detailResult = await client.GET("/api/admin/tenants/{tenantId}", {
        params: {
          path: {
            tenantId: selectedTenantId
          }
        }
      });

      if (
        detailResult.response.status === 401 ||
        detailResult.response.status === 403
      ) {
        return {
          kind: "forbidden",
          reason:
            "Seçili tenant detayı için platform yetkisi veya step-up oturumu doğrulanamadı."
        };
      }

      if (detailResult.response.status === 429) {
        return {
          kind: "unavailable",
          reason:
            "Platform operasyon rate limit'i devrede. Kısa süre sonra tekrar denenmeli."
        };
      }

      if (detailResult.response.status === 404) {
        selectedTenant = null;
      } else if (!detailResult.response.ok) {
        return {
          kind: "unavailable",
          reason: "Seçili tenant detayı şu anda alınamadı."
        };
      }

      selectedTenant = detailResult.data ?? null;
    }

    return {
      kind: "ready",
      overview: {
        filters: normalizedFilters,
        selectedTenant,
        tenants
      }
    };
  } catch {
    return {
      kind: "unavailable",
      reason: "Tenant control-plane verisi şu anda yüklenemedi."
    };
  }
}

const allowedTenantStatuses = new Set(["Active", "Suspended", "Closed"]);
const guidPattern =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

function normalizeFilters(filters: PlatformTenantFilters): PlatformTenantFilters {
  const status = normalizeFilter(filters.status);

  return {
    search: normalizeFilter(filters.search),
    status: status && allowedTenantStatuses.has(status) ? status : undefined,
    tenantId: normalizeGuidFilter(filters.tenantId)
  };
}

function normalizeFilter(value?: string) {
  const normalized = value?.trim();

  return normalized && normalized.length > 0 ? normalized : undefined;
}

function normalizeGuidFilter(value?: string) {
  const normalized = normalizeFilter(value);

  return normalized && guidPattern.test(normalized) ? normalized : undefined;
}
