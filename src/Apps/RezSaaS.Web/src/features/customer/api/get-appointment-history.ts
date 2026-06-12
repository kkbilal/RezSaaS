import { cookies } from "next/headers";
import { createServerApiClient } from "@/shared/api/server-client";
import type { ApiSchema } from "@/shared/api/types";

export type CustomerAppointmentHistoryItem =
  ApiSchema<"CustomerAppointmentHistoryItemResponse">;

export type CustomerAppointmentHistoryState =
  | {
      items: CustomerAppointmentHistoryItem[];
      kind: "ready";
    }
  | {
      items: [];
      kind: "unavailable";
      reason: string;
    };

export async function getCustomerAppointmentHistory(
  take: number = 50
): Promise<CustomerAppointmentHistoryState> {
  try {
    const cookieHeader = (await cookies()).toString();
    const { data, response } = await createServerApiClient(cookieHeader).GET(
      "/api/customer/appointment-history",
      {
        params: {
          query: {
            take
          }
        }
      }
    );

    if (!response.ok) {
      return {
        items: [],
        kind: "unavailable",
        reason: "Rezervasyon geçmişi şu anda alınamadı."
      };
    }

    return {
      items: data?.items ?? [],
      kind: "ready"
    };
  } catch {
    return {
      items: [],
      kind: "unavailable",
      reason: "Rezervasyon geçmişi şu anda yüklenemedi."
    };
  }
}
