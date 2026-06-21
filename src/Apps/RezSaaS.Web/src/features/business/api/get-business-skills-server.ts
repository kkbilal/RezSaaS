import { apiClient } from "@/shared/api/client";

export type BusinessSkillsData = Array<{ id: string; name: string }>;

export async function getBusinessSkillsServer(): Promise<BusinessSkillsData> {
  const { data } = await apiClient.GET("/api/business/skills");
  return (data as unknown as BusinessSkillsData) ?? [];
}
