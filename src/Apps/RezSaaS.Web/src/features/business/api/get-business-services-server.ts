import { apiClient } from "@/shared/api/client";
import type { ServiceResponse } from "@/features/business/api/business-service-client";

export async function getBusinessServicesServer(): Promise<ServiceResponse[]> {
  const { data } = await apiClient.GET("/api/business/services");
  return (data as unknown as ServiceResponse[]) ?? [];
}
