import { cookies } from "next/headers";
import { createServerApiClient } from "@/shared/api/server-client";
import type { ApiSchema } from "@/shared/api/types";
import type { BusinessTenantContext } from "./get-business-context";
import "server-only";

export type ServiceResponse = ApiSchema<"BusinessServiceResponse">;

export type ServicesState =
  | {
      services: ServiceResponse[];
      kind: "ready";
      tenant: BusinessTenantContext;
    }
  | {
      services: [];
      kind: "unavailable";
      reason: string;
    };

export async function getBusinessServicesServer(
  tenant: BusinessTenantContext
): Promise<ServicesState> {
  if (!tenant.tenantId) {
    return {
      services: [],
      kind: "unavailable",
      reason: "İşletme bilgisi doğrulanamadı."
    };
  }

  try {
    const cookieHeader = (await cookies()).toString();
    const { data, response } = await createServerApiClient(
      cookieHeader,
      tenant.tenantId
    ).GET("/api/business/services");

    if (!response.ok) {
      return {
        services: [],
        kind: "unavailable",
        reason: "Hizmetler şu anda alınamadı."
      };
    }

    return {
      services: data ?? [],
      kind: "ready",
      tenant
    };
  } catch {
    return {
      services: [],
      kind: "unavailable",
      reason: "Hizmetler şu anda yüklenemedi."
    };
  }
}