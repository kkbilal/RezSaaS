import { apiClient } from "@/shared/api/client";

export type ServiceResponse = {
  id: string;
  name: string;
  categoryKey: string;
  status: string;
  createdAtUtc: string;
};

export type CreateServiceRequest = {
  name: string;
  categoryKey: string;
};

export type UpdateServiceRequest = {
  name: string;
  categoryKey: string;
};

export async function listServices(): Promise<ServiceResponse[]> {
  const { data } = await apiClient.GET("/api/business/services");
  return (data as unknown as ServiceResponse[]) ?? [];
}

export async function createService(request: CreateServiceRequest): Promise<ServiceResponse | null> {
  const { data } = await apiClient.POST("/api/business/services", {
    body: request as never
  });
  return (data as unknown as ServiceResponse) ?? null;
}

export async function updateService(serviceId: string, request: UpdateServiceRequest): Promise<ServiceResponse | null> {
  const { data } = await apiClient.PATCH("/api/business/services/{serviceId}", {
    params: { path: { serviceId } },
    body: request as never
  });
  return (data as unknown as ServiceResponse) ?? null;
}

export async function archiveService(serviceId: string): Promise<ServiceResponse | null> {
  const { data } = await apiClient.POST("/api/business/services/{serviceId}/archive", {
    params: { path: { serviceId } }
  });
  return (data as unknown as ServiceResponse) ?? null;
}
