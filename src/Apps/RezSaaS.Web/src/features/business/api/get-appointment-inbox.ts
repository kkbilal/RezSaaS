import { cookies } from "next/headers";
import { createServerApiClient } from "@/shared/api/server-client";
import type { ApiSchema } from "@/shared/api/types";
import type {
  BusinessContextState,
  BusinessTenantContext
} from "./get-business-context";

export type BusinessAppointmentRequest =
  ApiSchema<"BusinessAppointmentRequestResponse">;

export type BusinessAppointmentInboxState =
  | {
      kind: "ready";
      requests: BusinessAppointmentRequest[];
      tenant: BusinessTenantContext;
    }
  | {
      kind: "unavailable";
      reason: string;
      requests: [];
    };

export function getPrimaryBusinessTenant(
  context: BusinessContextState
): BusinessTenantContext | null {
  return context.kind === "ready" ? context.tenants[0] ?? null : null;
}

export async function getBusinessAppointmentInbox(
  tenant: BusinessTenantContext,
  take: number = 50
): Promise<BusinessAppointmentInboxState> {
  if (!tenant.tenantId) {
    return {
      kind: "unavailable",
      reason: "Backend business context tenant id döndürmedi.",
      requests: []
    };
  }

  try {
    const cookieHeader = (await cookies()).toString();
    const { data, response } = await createServerApiClient(
      cookieHeader,
      tenant.tenantId
    ).GET("/api/business/appointment-requests", {
      params: {
        query: {
          take
        }
      }
    });

    if (!response.ok) {
      return {
        kind: "unavailable",
        reason: `Appointment inbox API ${response.status} döndü.`,
        requests: []
      };
    }

    return {
      kind: "ready",
      requests: data?.requests ?? [],
      tenant
    };
  } catch {
    return {
      kind: "unavailable",
      reason: "Appointment inbox API bağlantısı kurulamadı.",
      requests: []
    };
  }
}
