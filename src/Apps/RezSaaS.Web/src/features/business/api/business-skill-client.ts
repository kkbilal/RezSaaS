import { apiClient } from "@/shared/api/client";

export type SkillResponse = {
  id: string;
  name: string;
};

export type CreateSkillRequest = {
  name: string;
};

export type AssignSkillRequest = {
  skillId: string;
};

export async function listSkills(): Promise<SkillResponse[]> {
  const { data } = await apiClient.GET("/api/business/skills");
  return (data as unknown as SkillResponse[]) ?? [];
}

export async function createSkill(request: CreateSkillRequest): Promise<SkillResponse | null> {
  const { data } = await apiClient.POST("/api/business/skills", {
    body: request as never
  });
  return (data as unknown as SkillResponse) ?? null;
}

export async function deleteSkill(skillId: string): Promise<void> {
  await apiClient.DELETE("/api/business/skills/{skillId}", {
    params: { path: { skillId } }
  });
}

export async function assignSkillToStaff(staffMemberId: string, skillId: string): Promise<void> {
  await apiClient.POST("/api/business/staff/{staffMemberId}/skills", {
    params: { path: { staffMemberId } },
    body: { skillId } as never
  });
}

export async function removeSkillFromStaff(staffMemberId: string, skillId: string): Promise<void> {
  await apiClient.DELETE("/api/business/staff/{staffMemberId}/skills/{skillId}", {
    params: { path: { staffMemberId, skillId } }
  });
}
