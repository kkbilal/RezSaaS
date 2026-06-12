import { cookies } from "next/headers";
import { createServerApiClient } from "@/shared/api/server-client";
import type { ApiSchema } from "@/shared/api/types";
import type { BusinessTenantContext } from "./get-business-context";

export type BusinessAppointment = ApiSchema<"BusinessAppointmentResponse">;

export type BusinessAppointmentScheduleState =
  | {
      appointments: BusinessAppointment[];
      kind: "ready";
      tenant: BusinessTenantContext;
    }
  | {
      appointments: [];
      kind: "unavailable";
      reason: string;
    };

export async function getBusinessAppointments(
  tenant: BusinessTenantContext,
  take: number = 75
): Promise<BusinessAppointmentScheduleState> {
  if (!tenant.tenantId) {
    return {
      appointments: [],
      kind: "unavailable",
      reason: "İşletme bilgisi doğrulanamadı."
    };
  }

  try {
    const cookieHeader = (await cookies()).toString();
    const { data, response } = await createServerApiClient(
      cookieHeader,
      tenant.tenantId
    ).GET("/api/business/appointments", {
      params: {
        query: {
          take
        }
      }
    });

    if (!response.ok) {
      return {
        appointments: [],
        kind: "unavailable",
        reason: "Kesinleşmiş randevular şu anda alınamadı."
      };
    }

    return {
      appointments: data?.appointments ?? [],
      kind: "ready",
      tenant
    };
  } catch {
    return {
      appointments: [],
      kind: "unavailable",
      reason: "Kesinleşmiş randevular şu anda yüklenemedi."
    };
  }
}
