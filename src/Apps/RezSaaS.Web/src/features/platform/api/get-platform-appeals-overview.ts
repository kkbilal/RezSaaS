import { cookies } from "next/headers";
import { createServerApiClient } from "@/shared/api/server-client";
import type { ApiSchema } from "@/shared/api/types";

export type PlatformAppeal = ApiSchema<"AdminAbuseAppealResponse">;
export type PlatformClosureCase =
  ApiSchema<"AdminAccountClosureCaseResponse">;

export type PlatformAppealsFilters = {
  appealId?: string;
  appealStatus?: string;
  closureCaseId?: string;
  closureStatus?: string;
  userAccountId?: string;
};

export type PlatformAppealsOverview = {
  appeals: PlatformAppeal[];
  closureCases: PlatformClosureCase[];
  detailNotice?: string;
  filters: PlatformAppealsFilters;
  selectedAppeal: PlatformAppeal | null;
  selectedClosureCase: PlatformClosureCase | null;
};

export type PlatformAppealsOverviewState =
  | {
      kind: "ready";
      overview: PlatformAppealsOverview;
    }
  | {
      kind: "forbidden";
      reason: string;
    }
  | {
      kind: "unavailable";
      reason: string;
    };

export async function getPlatformAppealsOverview(
  filters: PlatformAppealsFilters
): Promise<PlatformAppealsOverviewState> {
  try {
    const normalizedFilters = normalizeFilters(filters);
    const cookieHeader = (await cookies()).toString();
    const client = createServerApiClient(cookieHeader);

    const [appealsResult, closureCasesResult] = await Promise.all([
      client.GET("/api/admin/abuse/appeals", {
        params: {
          query: {
            status: normalizedFilters.appealStatus,
            take: 50,
            userAccountId: normalizedFilters.userAccountId
          }
        }
      }),
      client.GET("/api/admin/abuse/closure-cases", {
        params: {
          query: {
            status: normalizedFilters.closureStatus,
            take: 50,
            userAccountId: normalizedFilters.userAccountId
          }
        }
      })
    ]);

    const listState = mapResponseState(
      [appealsResult.response, closureCasesResult.response],
      "İtiraz ve closure case listesi şu anda alınamadı."
    );

    if (listState) {
      return listState;
    }

    const selectedAppealId = normalizedFilters.appealId;
    const selectedClosureCaseId = normalizedFilters.closureCaseId;
    let detailNotice: string | undefined;
    let selectedAppeal: PlatformAppeal | null = null;
    let selectedClosureCase: PlatformClosureCase | null = null;

    if (selectedAppealId) {
      const appealResult = await client.GET(
        "/api/admin/abuse/appeals/{appealId}",
        {
          params: {
            path: {
              appealId: selectedAppealId
            }
          }
        }
      );

      const detailState = mapDetailResponseState(
        appealResult.response,
        "Seçili itiraz kaydı şu anda alınamadı."
      );

      if (detailState?.kind === "forbidden") {
        return detailState;
      }

      if (detailState?.kind === "unavailable") {
        detailNotice = detailState.reason;
      } else {
        selectedAppeal = appealResult.data ?? null;
      }
    }

    if (selectedClosureCaseId) {
      const closureResult = await client.GET(
        "/api/admin/abuse/closure-cases/{closureCaseId}",
        {
          params: {
            path: {
              closureCaseId: selectedClosureCaseId
            }
          }
        }
      );

      const detailState = mapDetailResponseState(
        closureResult.response,
        "Seçili closure case kaydı şu anda alınamadı."
      );

      if (detailState?.kind === "forbidden") {
        return detailState;
      }

      if (detailState?.kind === "unavailable") {
        detailNotice = detailState.reason;
      } else {
        selectedClosureCase = closureResult.data ?? null;
      }
    }

    return {
      kind: "ready",
      overview: {
        appeals: appealsResult.data?.appeals ?? [],
        closureCases: closureCasesResult.data?.closureCases ?? [],
        detailNotice,
        filters: normalizedFilters,
        selectedAppeal,
        selectedClosureCase
      }
    };
  } catch {
    return {
      kind: "unavailable",
      reason: "Platform itiraz verisi şu anda yüklenemedi."
    };
  }
}

function mapResponseState(
  responses: Response[],
  unavailableReason: string
): Exclude<PlatformAppealsOverviewState, { kind: "ready" }> | null {
  if (responses.some((response) => response.status === 401)) {
    return {
      kind: "forbidden",
      reason: "Platform oturumu doğrulanamadı."
    };
  }

  if (responses.some((response) => response.status === 403)) {
    return {
      kind: "forbidden",
      reason:
        "Bu ekran PlatformAdmin rolü ve geçerli MFA/step-up oturumu gerektirir."
    };
  }

  if (responses.some((response) => response.status === 429)) {
    return {
      kind: "unavailable",
      reason:
        "Platform operasyon rate limit'i devrede. Kısa süre sonra tekrar denenmeli."
    };
  }

  if (responses.some((response) => !response.ok)) {
    return {
      kind: "unavailable",
      reason: unavailableReason
    };
  }

  return null;
}

function mapDetailResponseState(
  response: Response,
  unavailableReason: string
): Exclude<PlatformAppealsOverviewState, { kind: "ready" }> | null {
  if (response.status === 404) {
    return {
      kind: "unavailable",
      reason: "Seçili kayıt bulunamadı veya artık erişilebilir değil."
    };
  }

  return mapResponseState([response], unavailableReason);
}

const allowedAppealStatuses = new Set(["Accepted", "PendingReview", "Rejected"]);
const allowedClosureStatuses = new Set([
  "Approved",
  "CancelledByAppeal",
  "Executed",
  "Executing",
  "PendingApproval",
  "Rejected"
]);
const guidPattern =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

function normalizeFilters(filters: PlatformAppealsFilters): PlatformAppealsFilters {
  const appealStatus = normalizeFilter(filters.appealStatus);
  const closureStatus = normalizeFilter(filters.closureStatus);

  return {
    appealId: normalizeGuidFilter(filters.appealId),
    appealStatus:
      appealStatus && allowedAppealStatuses.has(appealStatus)
        ? appealStatus
        : undefined,
    closureCaseId: normalizeGuidFilter(filters.closureCaseId),
    closureStatus:
      closureStatus && allowedClosureStatuses.has(closureStatus)
        ? closureStatus
        : undefined,
    userAccountId: normalizeGuidFilter(filters.userAccountId)
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
