import { apiClient } from "@/shared/api/client";

export type ResourceTypeResponse = {
  id: string;
  key: string;
  displayName: string;
};

export type CreateResourceTypeRequest = {
  key: string;
  displayName: string;
};

export async function listResourceTypes(): Promise<ResourceTypeResponse[]> {
  const { data } = await apiClient.GET("/api/business/resource-types");
  return (data as unknown as ResourceTypeResponse[]) ?? [];
}

export async function createResourceType(request: CreateResourceTypeRequest): Promise<ResourceTypeResponse | null> {
  const { data } = await apiClient.POST("/api/business/resource-types", {
    body: request as never
  });
  return (data as unknown as ResourceTypeResponse) ?? null;
}

export async function deleteResourceType(resourceTypeId: string): Promise<ResourceTypeResponse | null> {
  const { data } = await apiClient.DELETE("/api/business/resource-types/{resourceTypeId}", {
    params: { path: { resourceTypeId } }
  });
  return (data as unknown as ResourceTypeResponse) ?? null;
}
