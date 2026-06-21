import { apiClient } from "@/shared/api/client";

export type StaffResponse = {
  id: string;
  branchId: string;
  displayName: string;
  userAccountId: string | null;
  status: string;
  createdAtUtc: string;
};

export type CreateStaffRequest = {
  displayName: string;
  userAccountId?: string | null;
};

export type UpdateStaffRequest = {
  displayName: string;
};

export async function listStaff(branchId: string): Promise<StaffResponse[]> {
  const { data } = await apiClient.GET("/api/business/branches/{branchId}/staff", {
    params: { path: { branchId } }
  });
  return (data as unknown as StaffResponse[]) ?? [];
}

export async function createStaff(
  branchId: string,
  request: CreateStaffRequest
): Promise<StaffResponse | null> {
  const { data } = await apiClient.POST("/api/business/branches/{branchId}/staff", {
    params: { path: { branchId } },
    body: request as never
  });
  return (data as unknown as StaffResponse) ?? null;
}

export async function updateStaff(
  branchId: string,
  staffId: string,
  request: UpdateStaffRequest
): Promise<StaffResponse | null> {
  const { data } = await apiClient.PATCH("/api/business/branches/{branchId}/staff/{staffId}", {
    params: { path: { branchId, staffId } },
    body: request as never
  });
  return (data as unknown as StaffResponse) ?? null;
}

export async function archiveStaff(branchId: string, staffId: string): Promise<StaffResponse | null> {
  const { data } = await apiClient.POST("/api/business/branches/{branchId}/staff/{staffId}/archive", {
    params: { path: { branchId, staffId } }
  });
  return (data as unknown as StaffResponse) ?? null;
}
