import { createServerApiClient } from "@/shared/api/server-client";
import type { ApiSchema } from "@/shared/api/types";

export type PublicBusinessSummary = ApiSchema<"PublicBusinessSummaryView">;
export type PublicBusinessProfile = ApiSchema<"PublicBusinessProfileResponse">;

export type PublicBusinessSearchParams = {
  categoryKey?: string;
  city?: string;
  district?: string;
  searchText?: string;
};

export type PublicBusinessSearchState =
  | {
      businesses: PublicBusinessSummary[];
      kind: "ready";
    }
  | {
      businesses: [];
      kind: "unavailable";
      reason: string;
    };

export type PublicBusinessProfileState =
  | {
      kind: "ready";
      profile: PublicBusinessProfile;
    }
  | {
      kind: "not-found";
    }
  | {
      kind: "unavailable";
      reason: string;
    };

export async function searchPublicBusinesses(
  params: PublicBusinessSearchParams
): Promise<PublicBusinessSearchState> {
  try {
    const { data, response } = await createServerApiClient().GET(
      "/api/public/businesses",
      {
        params: {
          query: compactQuery({
            ...params,
            take: 24
          })
        }
      }
    );

    if (response.status === 429) {
      return {
        businesses: [],
        kind: "unavailable",
        reason: "Arama şu anda yoğun. Lütfen kısa süre sonra tekrar dene."
      };
    }

    if (!response.ok) {
      return {
        businesses: [],
        kind: "unavailable",
        reason: "İşletme araması şu anda tamamlanamadı."
      };
    }

    return {
      businesses: data ?? [],
      kind: "ready"
    };
  } catch {
    return {
      businesses: [],
      kind: "unavailable",
      reason: "İşletme araması şu anda tamamlanamadı."
    };
  }
}

export async function getPublicBusinessProfile(
  businessSlug: string
): Promise<PublicBusinessProfileState> {
  try {
    const { data, response } = await createServerApiClient().GET(
      "/api/public/businesses/{slug}/profile",
      {
        params: {
          path: {
            slug: businessSlug
          }
        }
      }
    );

    if (response.status === 404) {
      return {
        kind: "not-found"
      };
    }

    if (!response.ok || !data) {
      return {
        kind: "unavailable",
        reason: "İşletme profili şu anda yüklenemedi."
      };
    }

    return {
      kind: "ready",
      profile: data
    };
  } catch {
    return {
      kind: "unavailable",
      reason: "İşletme profili şu anda yüklenemedi."
    };
  }
}

function compactQuery<T extends Record<string, string | number | undefined>>(
  query: T
) {
  return Object.fromEntries(
    Object.entries(query).filter(([, value]) => value !== undefined && value !== "")
  ) as T;
}
