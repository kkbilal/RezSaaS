import { apiClient } from "@/shared/api/client";
import type { ResourceTypeResponse } from "@/features/business/api/business-resource-type-client";

export async function getBusinessResourceTypesServer(): Promise<ResourceTypeResponse[]> {
  const { data } = await apiClient.GET("/api/business/resource-types");
  return (data as unknown as ResourceTypeResponse[]) ?? [];
}
