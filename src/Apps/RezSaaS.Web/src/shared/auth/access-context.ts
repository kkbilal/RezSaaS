// NOT: goreli + .ts uzantili import. `@/` alias'i node --test tarafindan cozulemiyor;
// bu modul saf ve UNIT TEST EDILEBILIR kalmali (yetki mantiginin testi pazarlik konusu degil).
// tsconfig'de allowImportingTsExtensions acik, Next/webpack de bu bicimi cozuyor.
import {
  isBusinessCapability,
  type BusinessCapability,
  type Permission
} from "./capabilities.ts";

/**
 * ERISIM BAGLAMI -- kullanicinin SU AN ne yapabildigi.
 *
 * Tek kaynaktan uretilir (resolveAccessContext), tek fonksiyonla sorgulanir (can).
 * `canWrite` benzeri boolean'lari component agacinda prop olarak surukleme kalibi YASAK:
 * referans projelerin en pahali hatasi buydu (bir dosyada unutmak cok kolay).
 */
export type AccessContext = {
  readonly isAuthenticated: boolean;
  readonly isPlatformAdmin: boolean;
  readonly stepUpSatisfied: boolean;
  readonly activeTenant: ActiveTenantAccess | null;
};

export type ActiveTenantAccess = {
  readonly tenantId: string;
  readonly tenantDisplayName: string;
  readonly role: string;
  /** Uyelik sube-scoped ise dolu. Doluysa TUM business liste cagrilarina branchId eklenmelidir. */
  readonly branchId: string | null;
  readonly isTenantWide: boolean;
  readonly capabilities: ReadonlySet<BusinessCapability>;
};

/** Hicbir yetkisi olmayan baglam. Hata/belirsizlik durumlarinda buraya duseriz (FAIL-CLOSED). */
export const ANONYMOUS_ACCESS: AccessContext = {
  activeTenant: null,
  isAuthenticated: false,
  isPlatformAdmin: false,
  stepUpSatisfied: false
};

const PLATFORM_ADMIN_ROLE = "PlatformAdmin";

/**
 * Backend yanitlarindan AccessContext uretir. SAF fonksiyon -- test edilebilir.
 *
 * FAIL-CLOSED: taninmayan capability atilir; eksik/null alanlar yetki VERMEZ.
 * Generated tiplerde her alan opsiyonel oldugu icin bu sadece bir tercih degil, zorunluluk.
 */
export function buildAccessContext(input: {
  session: {
    account?: unknown;
    platformRoles?: string[] | null;
    stepUp?: { isSatisfied?: boolean } | null;
  } | null;
  tenants?: ReadonlyArray<{
    tenantId?: string;
    tenantDisplayName?: string | null;
    tenantSlug?: string | null;
    role?: string | null;
    branchId?: string | null;
    isTenantWide?: boolean;
    capabilities?: string[] | null;
  }> | null;
  /** Kullanicinin sectigi tenant (URL ?tenantId=). Yoksa ilk uyelik secilir. */
  requestedTenantId?: string | null;
}): AccessContext {
  const { requestedTenantId, session, tenants } = input;

  if (!session?.account) {
    return ANONYMOUS_ACCESS;
  }

  const platformRoles = session.platformRoles ?? [];
  const isPlatformAdmin = platformRoles.includes(PLATFORM_ADMIN_ROLE);

  const membership =
    (requestedTenantId
      ? tenants?.find((tenant) => tenant.tenantId === requestedTenantId)
      : undefined) ??
    tenants?.[0] ??
    null;

  return {
    activeTenant: membership?.tenantId
      ? {
          branchId: membership.branchId ?? null,
          capabilities: new Set(
            (membership.capabilities ?? []).filter(isBusinessCapability)
          ),
          isTenantWide: membership.isTenantWide ?? false,
          role: membership.role ?? "",
          tenantDisplayName:
            membership.tenantDisplayName ?? membership.tenantSlug ?? "İşletme",
          tenantId: membership.tenantId
        }
      : null,
    isAuthenticated: true,
    isPlatformAdmin,
    stepUpSatisfied: session.stepUp?.isSatisfied ?? false
  };
}

/**
 * FAIL-CLOSED yetki sorgusu. Tek kapi.
 *
 * Bilinmeyen/eslesmeyen her durumda FALSE doner. "Emin degilsem hayir" -- cunku bir izni
 * yanlislikla VERMEK, yanlislikla ESIRGEMEKTEN cok daha pahalidir.
 */
export function can(
  context: AccessContext | null | undefined,
  permission: Permission
): boolean {
  if (permission === "public") {
    return true;
  }

  if (!context) {
    return false;
  }

  if (permission === "auth") {
    return context.isAuthenticated;
  }

  if (permission === "platform.admin") {
    return context.isAuthenticated && context.isPlatformAdmin;
  }

  // Geriye yalnizca BusinessCapability kaliyor.
  if (!context.isAuthenticated || !context.activeTenant) {
    return false;
  }

  return context.activeTenant.capabilities.has(permission);
}

/**
 * Sube-scoped uyelik icin backend SOZLESMESI:
 * membership.branchId doluysa, business liste cagrilarina branchId GONDERMEK ZORUNLUDUR.
 *
 * Kanit: TenantBookingAuthorizationService -- BranchManager'in m.BranchId'si doluysa ve
 * cagrida branchId yoksa kosul false doner -> 403 Forbidden. Bu bir backend bug'i degil,
 * fail-closed tasarim. Gondermezsek kullanici bos ekran degil, HATA gorur.
 */
export function requiredBranchScope(context: AccessContext): string | null {
  return context.activeTenant?.branchId ?? null;
}
