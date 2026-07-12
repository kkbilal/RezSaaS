import { createTenantApiClient } from "@/shared/api/client";
import type { ApiSchema } from "@/shared/api/types";

export type BusinessBranchResponse = ApiSchema<"BusinessBranchResponse">;

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

export async function listBranches(tenantId: string): Promise<BusinessBranchResponse[]> {
  const client = createTenantApiClient(tenantId);
  const { data } = await client.GET("/api/business/branches");
  return data ?? [];
}

export async function getBranch(tenantId: string, branchId: string): Promise<BusinessBranchResponse | null> {
  const client = createTenantApiClient(tenantId);
  const { data } = await client.GET("/api/business/branches/{branchId}", {
    params: { path: { branchId } }
  });
  return data ?? null;
}

export async function createBranch(tenantId: string, request: CreateBranchRequest): Promise<BusinessBranchResponse | null> {
  const client = createTenantApiClient(tenantId);
  const { data } = await client.POST("/api/business/branches", {
    body: request as never
  });
  return data ?? null;
}

export async function updateBranch(
  tenantId: string,
  branchId: string,
  request: UpdateBranchRequest
): Promise<BusinessBranchResponse | null> {
  const client = createTenantApiClient(tenantId);
  const { data } = await client.PATCH("/api/business/branches/{branchId}", {
    params: { path: { branchId } },
    body: request as never
  });
  return data ?? null;
}

export async function updateBranchSlotSettings(
  tenantId: string,
  branchId: string,
  request: UpdateBranchSlotSettingsRequest
): Promise<BusinessBranchResponse | null> {
  const client = createTenantApiClient(tenantId);
  const { data } = await client.PATCH("/api/business/branches/{branchId}/slot-settings", {
    params: { path: { branchId } },
    body: request as never
  });
  return data ?? null;
}

export async function archiveBranch(tenantId: string, branchId: string): Promise<BusinessBranchResponse | null> {
  const client = createTenantApiClient(tenantId);
  const { data } = await client.POST("/api/business/branches/{branchId}/archive", {
    params: { path: { branchId } }
  });
  return data ?? null;
}
