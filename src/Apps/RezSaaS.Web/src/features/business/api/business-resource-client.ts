import { apiClient } from "@/shared/api/client";

export type ResourceResponse = {
  id: string;
  resourceTypeId: string;
  displayName: string;
  status: string;
};

export type CreateResourceRequest = {
  resourceTypeId: string;
  displayName: string;
};

export type RenameResourceRequest = {
  displayName: string;
};

export async function listResourcesByBranch(branchId: string): Promise<ResourceResponse[]> {
  const { data } = await apiClient.GET("/api/business/branches/{branchId}/resources", {
    params: { path: { branchId } }
  });
  return (data as unknown as ResourceResponse[]) ?? [];
}

export async function createResource(branchId: string, request: CreateResourceRequest): Promise<ResourceResponse | null> {
  const { data } = await apiClient.POST("/api/business/branches/{branchId}/resources", {
    params: { path: { branchId } },
    body: request as never
  });
  return (data as unknown as ResourceResponse) ?? null;
}

export async function renameResource(branchId: string, resourceId: string, request: RenameResourceRequest): Promise<ResourceResponse | null> {
  const { data } = await apiClient.PATCH("/api/business/branches/{branchId}/resources/{resourceId}", {
    params: { path: { branchId, resourceId } },
    body: request as never
  });
  return (data as unknown as ResourceResponse) ?? null;
}

export async function markResourceOutOfService(branchId: string, resourceId: string): Promise<ResourceResponse | null> {
  const { data } = await apiClient.POST("/api/business/branches/{branchId}/resources/{resourceId}/out-of-service", {
    params: { path: { branchId, resourceId } }
  });
  return (data as unknown as ResourceResponse) ?? null;
}

export async function restoreResource(branchId: string, resourceId: string): Promise<ResourceResponse | null> {
  const { data } = await apiClient.POST("/api/business/branches/{branchId}/resources/{resourceId}/restore", {
    params: { path: { branchId, resourceId } }
  });
  return (data as unknown as ResourceResponse) ?? null;
}
