export type PlatformTenantLifecycleAction = "close" | "reactivate" | "suspend";

export type PlatformTenantLifecycleInput = {
  displayName?: string | null;
  slug?: string | null;
  status?: string | null;
  tenantId?: string | null;
};

export type TenantLifecycleActionDraft = {
  confirmation: string;
  reason: string;
};

export type TenantLifecycleActionConfig = {
  confirmPhrase: string;
  description: string;
  isDangerous: boolean;
  label: string;
  title: string;
};

export type TenantLifecycleValidationResult = {
  errors: string[];
  normalizedReason: string;
};

export const tenantLifecycleReasonMaxLength = 300;

export function getTenantLifecycleActionConfig(
  action: PlatformTenantLifecycleAction,
  tenant: PlatformTenantLifecycleInput
): TenantLifecycleActionConfig {
  const tenantName = getTenantDisplayName(tenant);
  const tenantSlug = getTenantConfirmationSlug(tenant);

  if (action === "suspend") {
    return {
      confirmPhrase: tenantSlug,
      description:
        "Suspended tenant public discovery, slot arama, yeni booking ve işletme operasyonlarına kapanır; müşteri mevcut geçmişini görmeye devam eder.",
      isDangerous: true,
      label: "Suspend",
      title: `${tenantName} tenant'ını askıya al`
    };
  }

  if (action === "reactivate") {
    return {
      confirmPhrase: tenantSlug,
      description:
        "Reactivation yalnız Suspended tenant için geçerlidir. Closed tenant terminaldir ve bu akıştan geri alınamaz.",
      isDangerous: false,
      label: "Reactivate",
      title: `${tenantName} tenant'ını yeniden aktif et`
    };
  }

  return {
    confirmPhrase: `KAPAT ${tenantSlug}`,
    description:
      "Close terminaldir. Tenant tekrar aktif edilemez; public discovery, yeni booking ve işletme operasyonları kalıcı olarak kapanır.",
    isDangerous: true,
    label: "Close",
    title: `${tenantName} tenant'ını kalıcı kapat`
  };
}

export function getTenantLifecycleActionAvailability(
  action: PlatformTenantLifecycleAction,
  tenant: PlatformTenantLifecycleInput
) {
  const status = tenant.status ?? "Unknown";

  if (action === "suspend") {
    return status === "Active"
      ? null
      : "Suspend yalnız Active tenant için açılır.";
  }

  if (action === "reactivate") {
    return status === "Suspended"
      ? null
      : "Reactivate yalnız Suspended tenant için açılır.";
  }

  return status === "Closed" ? "Closed tenant terminaldir." : null;
}

export function validateTenantLifecycleActionDraft(
  action: PlatformTenantLifecycleAction,
  tenant: PlatformTenantLifecycleInput,
  draft: TenantLifecycleActionDraft
): TenantLifecycleValidationResult {
  const errors: string[] = [];
  const normalizedReason = normalizeReason(draft.reason);
  const availabilityError = getTenantLifecycleActionAvailability(action, tenant);
  const config = getTenantLifecycleActionConfig(action, tenant);

  if (availabilityError) {
    errors.push(availabilityError);
  }

  if (normalizedReason.length === 0) {
    errors.push("Operasyon nedeni zorunlu.");
  }

  if (normalizedReason.length > tenantLifecycleReasonMaxLength) {
    errors.push(`Operasyon nedeni ${tenantLifecycleReasonMaxLength} karakteri aşamaz.`);
  }

  if (containsSensitiveOperationalText(normalizedReason)) {
    errors.push("Operasyon nedeni e-posta, telefon, token, parola veya secret içermemeli.");
  }

  if (draft.confirmation.trim() !== config.confirmPhrase) {
    errors.push(`Onay metni tam olarak '${config.confirmPhrase}' olmalı.`);
  }

  return {
    errors,
    normalizedReason
  };
}

export function normalizeReason(value: string) {
  return value.trim().replace(/\s+/g, " ");
}

function getTenantDisplayName(tenant: PlatformTenantLifecycleInput) {
  return tenant.displayName?.trim() || tenant.slug?.trim() || "Seçili";
}

function getTenantConfirmationSlug(tenant: PlatformTenantLifecycleInput) {
  return tenant.slug?.trim() || tenant.tenantId?.trim() || "tenant";
}

function containsSensitiveOperationalText(value: string) {
  const lowerValue = value.toLocaleLowerCase("tr-TR");

  if (/[^\s@]+@[^\s@]+\.[^\s@]+/.test(value)) {
    return true;
  }

  if (/(api[-_ ]?key|authorization|bearer|otp|parola|password|secret|token)/i.test(lowerValue)) {
    return true;
  }

  const digitCount = value.replace(/\D/g, "").length;

  return digitCount >= 10;
}
