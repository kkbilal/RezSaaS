import { apiClient } from "@/shared/api/client";

export type WorkingHoursResponse = {
  id: string;
  dayOfWeek: string;
  opensAt: string;
  closesAt: string;
  isClosed: boolean;
};

export type UpsertWorkingHoursRequest = {
  opensAt: string;
  closesAt: string;
  isClosed: boolean;
};

export async function listWorkingHours(branchId: string): Promise<WorkingHoursResponse[]> {
  const { data } = await apiClient.GET("/api/business/branches/{branchId}/working-hours", {
    params: { path: { branchId } }
  });
  return (data as unknown as WorkingHoursResponse[]) ?? [];
}

export async function upsertWorkingHours(
  branchId: string,
  dayOfWeek: string,
  request: UpsertWorkingHoursRequest
): Promise<WorkingHoursResponse | null> {
  const { data } = await apiClient.PUT("/api/business/branches/{branchId}/working-hours/{dayOfWeek}", {
    params: { path: { branchId, dayOfWeek } },
    body: request as never
  });
  return (data as unknown as WorkingHoursResponse) ?? null;
}

export async function clearWorkingHours(branchId: string): Promise<WorkingHoursResponse[]> {
  const { data } = await apiClient.DELETE("/api/business/branches/{branchId}/working-hours", {
    params: { path: { branchId } }
  });
  return (data as unknown as WorkingHoursResponse[]) ?? [];
}
