import { cookies } from "next/headers";
import { createServerApiClient } from "@/shared/api/server-client";
import type { ApiSchema } from "@/shared/api/types";

export type PlatformUserAbuseEvent = ApiSchema<"AdminAbuseEventResponse">;
export type PlatformUserSanction =
  ApiSchema<"AdminUserSanctionResponse">;
export type PlatformUserStrike = ApiSchema<"AdminUserStrikeResponse">;
export type PlatformUserReport =
  ApiSchema<"AdminBusinessAbuseReportResponse">;
export type PlatformUserRisk = ApiSchema<"AdminUserRiskResponse">;

export type PlatformUserAbuseOverview = {
  userAccountId: string;
  events: PlatformUserAbuseEvent[];
  sanctions: PlatformUserSanction[];
  strikes: PlatformUserStrike[];
  reports: PlatformUserReport[];
  risk: PlatformUserRisk | null;
};

export type PlatformUserAbuseOverviewState =
  | {
      kind: "ready";
      overview: PlatformUserAbuseOverview;
    }
  | {
      kind: "forbidden";
      reason: string;
    }
  | {
      kind: "unavailable";
      reason: string;
    };

export async function getPlatformUserAbuseOverview(
  userAccountId: string
): Promise<PlatformUserAbuseOverviewState> {
  try {
    const cookieHeader = (await cookies()).toString();
    const client = createServerApiClient(cookieHeader);

    const result = await client.GET(
      "/api/admin/abuse/users/{userAccountId}",
      {
        params: {
          path: {
            userAccountId
          }
        }
      }
    );

    if (result.response.status === 401) {
      return {
        kind: "forbidden",
        reason: "Platform oturumu doğrulanamadı."
      };
    }

    if (result.response.status === 403) {
      return {
        kind: "forbidden",
        reason:
          "Bu ekran PlatformAdmin rolü ve geçerli MFA/step-up oturumu gerektirir."
      };
    }

    if (result.response.status === 404) {
      return {
        kind: "unavailable",
        reason: "Kullanıcı bulunamadı."
      };
    }

    if (result.response.status === 429) {
      return {
        kind: "unavailable",
        reason:
          "Platform operasyon rate limit'i devrede. Kısa süre sonra tekrar denenmeli."
      };
    }

    if (!result.response.ok || !result.data) {
      return {
        kind: "unavailable",
        reason: "Kullanıcı abuse verisi şu anda alınamadı."
      };
    }

    return {
      kind: "ready",
      overview: {
        userAccountId: result.data.userAccountId ?? userAccountId,
        events: result.data.events ?? [],
        sanctions: result.data.sanctions ?? [],
        strikes: result.data.strikes ?? [],
        reports: result.data.reports ?? [],
        risk: result.data.risk ?? null
      }
    };
  } catch {
    return {
      kind: "unavailable",
      reason: "Kullanıcı abuse verisi şu anda yüklenemedi."
    };
  }
}
