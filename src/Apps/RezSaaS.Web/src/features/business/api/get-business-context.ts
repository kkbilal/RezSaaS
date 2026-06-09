import { cookies } from "next/headers";
import { createServerApiClient } from "@/shared/api/server-client";
import type { ApiSchema } from "@/shared/api/types";

export type BusinessTenantContext = ApiSchema<"BusinessTenantContextResponse">;

export type BusinessContextState =
  | {
      kind: "ready";
      tenants: BusinessTenantContext[];
    }
  | {
      kind: "unauthenticated";
      tenants: [];
    }
  | {
      kind: "unavailable";
      reason: string;
      tenants: [];
    };

export async function getBusinessContext(): Promise<BusinessContextState> {
  try {
    const cookieHeader = (await cookies()).toString();
    const { data, response } = await createServerApiClient(cookieHeader).GET(
      "/api/business/context"
    );

    if (response.status === 401) {
      return {
        kind: "unauthenticated",
        tenants: []
      };
    }

    if (!response.ok) {
      return {
        kind: "unavailable",
        reason: `Backend ${response.status} döndü.`,
        tenants: []
      };
    }

    return {
      kind: "ready",
      tenants: data?.tenants ?? []
    };
  } catch {
    return {
      kind: "unavailable",
      reason: "Backend bağlantısı kurulamadı.",
      tenants: []
    };
  }
}
