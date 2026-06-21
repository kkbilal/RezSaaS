import { apiClient } from "@/shared/api/client";

export type VariantResponse = {
  id: string;
  serviceId: string;
  name: string;
  durationMinutes: number;
  priceAmount: number;
  currencyCode: string;
  requiredResourceTypeId: string | null;
  createdAtUtc: string;
};

export type CreateVariantRequest = {
  name: string;
  durationMinutes: number;
  priceAmount: number;
  currencyCode: string;
  requiredResourceTypeId?: string | null;
};

export type UpdateVariantRequest = {
  name: string;
  durationMinutes: number;
  priceAmount: number;
  currencyCode: string;
  requiredResourceTypeId?: string | null;
};

export async function listVariants(serviceId: string): Promise<VariantResponse[]> {
  const { data } = await apiClient.GET("/api/business/services/{serviceId}/variants", {
    params: { path: { serviceId } }
  });
  return (data as unknown as VariantResponse[]) ?? [];
}

export async function createVariant(serviceId: string, request: CreateVariantRequest): Promise<VariantResponse | null> {
  const { data } = await apiClient.POST("/api/business/services/{serviceId}/variants", {
    params: { path: { serviceId } },
    body: request as never
  });
  return (data as unknown as VariantResponse) ?? null;
}

export async function updateVariant(serviceId: string, variantId: string, request: UpdateVariantRequest): Promise<VariantResponse | null> {
  const { data } = await apiClient.PATCH("/api/business/services/{serviceId}/variants/{variantId}", {
    params: { path: { serviceId, variantId } },
    body: request as never
  });
  return (data as unknown as VariantResponse) ?? null;
}

export async function deleteVariant(serviceId: string, variantId: string): Promise<void> {
  await apiClient.DELETE("/api/business/services/{serviceId}/variants/{variantId}", {
    params: { path: { serviceId, variantId } }
  });
}

export async function assignRequiredSkill(variantId: string, skillId: string): Promise<void> {
  await apiClient.POST("/api/business/services/{serviceId}/variants/{variantId}/required-skills/{skillId}", {
    params: { path: { serviceId: "", variantId, skillId } }
  });
}

export async function removeRequiredSkill(variantId: string, skillId: string): Promise<void> {
  await apiClient.DELETE("/api/business/services/{serviceId}/variants/{variantId}/required-skills/{skillId}", {
    params: { path: { serviceId: "", variantId, skillId } }
  });
}
