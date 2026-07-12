import { cookies } from "next/headers";
import { createServerApiClient } from "@/shared/api/server-client";
import type { ApiSchema } from "@/shared/api/types";
import type { BusinessTenantContext } from "./get-business-context";
import "server-only";

export type SkillResponse = ApiSchema<"BusinessSkillResponse">;

export type SkillsState =
  | {
      skills: SkillResponse[];
      kind: "ready";
      tenant: BusinessTenantContext;
    }
  | {
      skills: [];
      kind: "unavailable";
      reason: string;
    };

export async function getBusinessSkillsServer(
  tenant: BusinessTenantContext
): Promise<SkillsState> {
  if (!tenant.tenantId) {
    return {
      skills: [],
      kind: "unavailable",
      reason: "İşletme bilgisi doğrulanamadı."
    };
  }

  try {
    const cookieHeader = (await cookies()).toString();
    const { data, response } = await createServerApiClient(
      cookieHeader,
      tenant.tenantId
    ).GET("/api/business/skills");

    if (!response.ok) {
      return {
        skills: [],
        kind: "unavailable",
        reason: "Yetkinlikler şu anda alınamadı."
      };
    }

    return {
      skills: data ?? [],
      kind: "ready",
      tenant
    };
  } catch {
    return {
      skills: [],
      kind: "unavailable",
      reason: "Yetkinlikler şu anda yüklenemedi."
    };
  }
}