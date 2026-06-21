import { apiClient } from "@/shared/api/client";

export type StaffUnavailableResponse = {
  id: string;
  staffMemberId: string;
  startUtc: string;
  endUtc: string;
  reason: string;
};

export type CreateStaffUnavailableRequest = {
  startUtc: string;
  endUtc: string;
  reason: string;
};

export async function listStaffUnavailable(staffMemberId: string): Promise<StaffUnavailableResponse[]> {
  const { data } = await apiClient.GET("/api/business/staff/{staffMemberId}/unavailable", {
    params: { path: { staffMemberId } }
  });
  return (data as unknown as StaffUnavailableResponse[]) ?? [];
}

export async function createStaffUnavailable(
  staffMemberId: string,
  request: CreateStaffUnavailableRequest
): Promise<StaffUnavailableResponse | null> {
  const { data } = await apiClient.POST("/api/business/staff/{staffMemberId}/unavailable", {
    params: { path: { staffMemberId } },
    body: request as never
  });
  return (data as unknown as StaffUnavailableResponse) ?? null;
}

export async function deleteStaffUnavailable(
  staffMemberId: string,
  unavailableId: string
): Promise<StaffUnavailableResponse | null> {
  const { data } = await apiClient.DELETE("/api/business/staff/{staffMemberId}/unavailable/{unavailableId}", {
    params: { path: { staffMemberId, unavailableId } }
  });
  return (data as unknown as StaffUnavailableResponse) ?? null;
}
