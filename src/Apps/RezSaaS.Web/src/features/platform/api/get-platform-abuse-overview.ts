import { cookies } from "next/headers";
import { createServerApiClient } from "@/shared/api/server-client";
import type { ApiSchema } from "@/shared/api/types";

export type PlatformAbuseAppeal = ApiSchema<"AdminAbuseAppealResponse">;
export type PlatformAbuseEvent = ApiSchema<"AdminAbuseEventResponse">;
export type PlatformAbuseReport =
  ApiSchema<"AdminBusinessAbuseReportResponse">;
export type PlatformClosureCase =
  ApiSchema<"AdminAccountClosureCaseResponse">;
export type PlatformReconciliation =
  ApiSchema<"AdminOperationsReconciliationResponse">;

export type PlatformAbuseOverview = {
  appeals: PlatformAbuseAppeal[];
  closureCases: PlatformClosureCase[];
  events: PlatformAbuseEvent[];
  reconciliation: PlatformReconciliation | null;
  reports: PlatformAbuseReport[];
};

export type PlatformAbuseOverviewState =
  | {
      kind: "ready";
      overview: PlatformAbuseOverview;
    }
  | {
      kind: "forbidden";
      reason: string;
    }
  | {
      kind: "unavailable";
      reason: string;
    };

export async function getPlatformAbuseOverview(): Promise<PlatformAbuseOverviewState> {
  try {
    const cookieHeader = (await cookies()).toString();
    const client = createServerApiClient(cookieHeader);

    const [eventsResult, reportsResult, appealsResult, closureCasesResult, reconciliationResult] =
      await Promise.all([
        client.GET("/api/admin/abuse/events", {
          params: {
            query: {
              take: 20
            }
          }
        }),
        client.GET("/api/admin/abuse/reports", {
          params: {
            query: {
              status: "PendingReview",
              take: 20
            }
          }
        }),
        client.GET("/api/admin/abuse/appeals", {
          params: {
            query: {
              status: "PendingReview",
              take: 20
            }
          }
        }),
        client.GET("/api/admin/abuse/closure-cases", {
          params: {
            query: {
              take: 20
            }
          }
        }),
        client.GET("/api/admin/operations/reconciliation")
      ]);

    const results = [
      eventsResult,
      reportsResult,
      appealsResult,
      closureCasesResult,
      reconciliationResult
    ];

    if (results.some((result) => result.response.status === 401)) {
      return {
        kind: "forbidden",
        reason: "Platform oturumu doğrulanamadı."
      };
    }

    if (results.some((result) => result.response.status === 403)) {
      return {
        kind: "forbidden",
        reason:
          "Bu ekran PlatformAdmin rolü ve geçerli MFA/step-up oturumu gerektirir."
      };
    }

    if (results.some((result) => result.response.status === 429)) {
      return {
        kind: "unavailable",
        reason:
          "Platform operasyon rate limit'i devrede. Kısa süre sonra tekrar denenmeli."
      };
    }

    if (results.some((result) => !result.response.ok)) {
      return {
        kind: "unavailable",
        reason: "Platform abuse verisi şu anda alınamadı."
      };
    }

    return {
      kind: "ready",
      overview: {
        appeals: appealsResult.data?.appeals ?? [],
        closureCases: closureCasesResult.data?.closureCases ?? [],
        events: eventsResult.data?.events ?? [],
        reconciliation: reconciliationResult.data ?? null,
        reports: reportsResult.data?.reports ?? []
      }
    };
  } catch {
    return {
      kind: "unavailable",
      reason: "Platform abuse verisi şu anda yüklenemedi."
    };
  }
}
