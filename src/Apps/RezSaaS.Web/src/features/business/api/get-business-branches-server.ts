import { apiClient } from "@/shared/api/client";
import type { BranchResponse } from "@/features/business/api/business-branch-client";

export async function getBusinessBranchesServer(): Promise<BranchResponse[]> {
  const { data } = await apiClient.GET("/api/business/branches");
  return (data as unknown as BranchResponse[]) ?? [];
}
