import { apiClient } from "@/shared/api/client";

export type BranchResponse = {
  id: string;
  slug: string;
  displayName: string;
  timeZoneId: string;
  city: string;
  district: string;
  addressLine: string;
  slotIntervalMinutes: number | null;
  maxPublicSlots: number | null;
  createdAtUtc: string;
};

export type CreateBranchRequest = {
  slug: string;
  displayName: string;
  timeZoneId: string;
  city?: string;
  district?: string;
  addressLine?: string;
};

export type UpdateBranchRequest = {
  displayName?: string;
  city?: string;
  district?: string;
  addressLine?: string;
};

export type UpdateBranchSlotSettingsRequest = {
  slotIntervalMinutes?: number | null;
  maxPublicSlots?: number | null;
};

export async function listBranches(): Promise<BranchResponse[]> {
  const { data } = await apiClient.GET("/api/business/branches");
  return (data as unknown as BranchResponse[]) ?? [];
}

export async function getBranch(branchId: string): Promise<BranchResponse | null> {
  const { data } = await apiClient.GET("/api/business/branches/{branchId}", {
    params: { path: { branchId } }
  });
  return (data as unknown as BranchResponse) ?? null;
}

export async function createBranch(request: CreateBranchRequest): Promise<BranchResponse | null> {
  const { data } = await apiClient.POST("/api/business/branches", {
    body: request as never
  });
  return (data as unknown as BranchResponse) ?? null;
}

export async function updateBranch(
  branchId: string,
  request: UpdateBranchRequest
): Promise<BranchResponse | null> {
  const { data } = await apiClient.PATCH("/api/business/branches/{branchId}", {
    params: { path: { branchId } },
    body: request as never
  });
  return (data as unknown as BranchResponse) ?? null;
}

export async function updateBranchSlotSettings(
  branchId: string,
  request: UpdateBranchSlotSettingsRequest
): Promise<BranchResponse | null> {
  const { data } = await apiClient.PATCH("/api/business/branches/{branchId}/slot-settings", {
    params: { path: { branchId } },
    body: request as never
  });
  return (data as unknown as BranchResponse) ?? null;
}

export async function archiveBranch(branchId: string): Promise<BranchResponse | null> {
  const { data } = await apiClient.POST("/api/business/branches/{branchId}/archive", {
    params: { path: { branchId } }
  });
  return (data as unknown as BranchResponse) ?? null;
}
