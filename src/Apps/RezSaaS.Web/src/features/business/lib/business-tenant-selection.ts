import type {
  BusinessContextState,
  BusinessTenantContext
} from "@/features/business/api/get-business-context";

export function selectBusinessTenant(
  context: BusinessContextState,
  requestedTenantId?: string
): BusinessTenantContext | null {
  if (context.kind !== "ready") {
    return null;
  }

  if (requestedTenantId) {
    const requestedTenant = context.tenants.find(
      (tenant) => tenant.tenantId === requestedTenantId
    );

    if (requestedTenant) {
      return requestedTenant;
    }
  }

  return context.tenants[0] ?? null;
}

export function firstSearchParam(value?: string | string[]) {
  return Array.isArray(value) ? value[0] : value;
}
