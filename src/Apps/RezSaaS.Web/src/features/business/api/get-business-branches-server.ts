import { cookies } from "next/headers";
import { createServerApiClient } from "@/shared/api/server-client";
import type { ApiSchema } from "@/shared/api/types";
import type { BusinessTenantContext } from "./get-business-context";
import "server-only";

export type BusinessBranch = ApiSchema<"BusinessBranchResponse">;

export type BusinessBranchesState =
  | {
      branches: BusinessBranch[];
      kind: "ready";
      tenant: BusinessTenantContext;
    }
  | {
      branches: [];
      kind: "unavailable";
      reason: string;
    };

export async function getBusinessBranchesServer(
  tenant: BusinessTenantContext
): Promise<BusinessBranchesState> {
  if (!tenant.tenantId) {
    return {
      branches: [],
      kind: "unavailable",
      reason: "İşletme bilgisi doğrulanamadı."
    };
  }

  try {
    const cookieHeader = (await cookies()).toString();
    const { data, response } = await createServerApiClient(
      cookieHeader,
      tenant.tenantId
    ).GET("/api/business/branches");

    if (!response.ok) {
      return {
        branches: [],
        kind: "unavailable",
        reason: "Şubeler şu anda alınamadı."
      };
    }

    return {
      branches: data ?? [],
      kind: "ready",
      tenant
    };
  } catch {
    return {
      branches: [],
      kind: "unavailable",
      reason: "Şubeler şu anda yüklenemedi."
    };
  }
}