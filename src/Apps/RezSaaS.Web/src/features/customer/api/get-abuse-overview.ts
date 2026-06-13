import { cookies } from "next/headers";
import { createServerApiClient } from "@/shared/api/server-client";
import type { ApiSchema } from "@/shared/api/types";

export type CustomerAbuseOverview = ApiSchema<"CustomerAbuseOverviewResponse">;
export type CustomerAbuseAppeal = ApiSchema<"CustomerAbuseAppealResponse">;
export type CustomerSanction = ApiSchema<"CustomerSanctionResponse">;
export type CustomerStrike = ApiSchema<"CustomerStrikeResponse">;
export type CustomerClosureCase = ApiSchema<"CustomerAccountClosureCaseResponse">;

export type CustomerAbuseOverviewState =
  | {
      kind: "ready";
      overview: CustomerAbuseOverview;
    }
  | {
      kind: "unavailable";
      reason: string;
    };

export async function getCustomerAbuseOverview(): Promise<CustomerAbuseOverviewState> {
  try {
    const cookieHeader = (await cookies()).toString();
    const { data, response } = await createServerApiClient(cookieHeader).GET(
      "/api/customer/abuse/overview"
    );

    if (!response.ok || !data) {
      return {
        kind: "unavailable",
        reason: "İtiraz ve yaptırım bilgileri şu anda alınamadı."
      };
    }

    return {
      kind: "ready",
      overview: data
    };
  } catch {
    return {
      kind: "unavailable",
      reason: "İtiraz ve yaptırım bilgileri şu anda yüklenemedi."
    };
  }
}
