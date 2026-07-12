import type { BusinessTenantContext } from "@/features/business/api/get-business-context";
import type { PanelTenantOption } from "@/features/business/components/panel-shell";

export function buildPanelTenants(
  tenants: ReadonlyArray<BusinessTenantContext>
): PanelTenantOption[] {
  return tenants.map((tenant) => ({
    tenantId: tenant.tenantId ?? "",
    label:
      tenant.tenantDisplayName ??
      tenant.tenantSlug ??
      "İşletme",
    branchLabel: tenant.branchId
      ? `Şube: ${tenant.branchId.slice(0, 8)}`
      : undefined
  }));
}
