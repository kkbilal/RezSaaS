// Tek doğruluk kaynağı: buradaki her rotanın src/app altında bir page.tsx'i VARDIR.
// Sayfası olmayan rota buraya EKLENMEZ — aksi halde sidebar/link canlı 404 üretir.
// (Bu dosyada bir zamanlar 14 hayalet rota vardı; üçü canlı 404 olarak kullanıcıya çıkıyordu.)
export const routes = {
  auth: {
    dispatch: "/gelis",
    forgotPassword: "/sifremi-unuttum",
    login: "/giris",
    register: "/kayit",
    resetPassword: "/sifre-sifirla"
  },
  business: {
    panel: "/panel",
    appointments: "/panel/randevular",
    branches: "/panel/subeler",
    calendar: "/panel/takvim",
    requests: "/panel/talepler",
    resources: "/panel/kaynaklar",
    resourceTypes: "/panel/kaynak-turleri",
    services: "/panel/hizmetler",
    settings: "/panel/ayarlar",
    skills: "/panel/yetenekler",
    staff: "/panel/personel",
    workingHours: "/panel/calisma-saatleri"
  },
  customer: {
    appeals: "/hesabim/itirazlar",
    dashboard: "/hesabim",
    profile: "/hesabim/profil",
    // Musterinin TEK gercek listesi burasi: GET /api/customer/appointment-history
    // hem talepleri hem randevulari birlikte donuyor (ItemType ile ayrisiyor).
    // /hesabim/randevular sadece eski linkler icin buraya yonlenen bir stub.
    // Adim 3'te bu sayfa "Randevularim" olarak yeniden adlandirilip sekmeli hale gelecek.
    requests: "/hesabim/talepler"
  },
  platform: {
    abuse: "/platform/abuse",
    abuseUser: (userAccountId: string) =>
      `/platform/abuse/kullanici/${encodeURIComponent(userAccountId)}`,
    appeals: "/platform/itirazlar",
    dashboard: "/platform",
    tenants: "/platform/tenantlar",
    tenantMembers: (tenantId: string) =>
      `/platform/tenantlar/${encodeURIComponent(tenantId)}/uyeler`,
    newTenant: "/platform/tenantlar/yeni"
  },
  public: {
    businessProfile: (businessSlug: string) =>
      `/isletme/${encodeURIComponent(businessSlug)}`,
    discover: "/kesfet",
    home: "/"
  }
} as const;

export function normalizeReturnTo(
  value: string | string[] | undefined,
  fallback: string = routes.business.panel
) {
  const candidate = Array.isArray(value) ? value[0] : value;

  if (!candidate || !candidate.startsWith("/") || candidate.startsWith("//")) {
    return fallback;
  }

  if (candidate.startsWith("/api/")) {
    return fallback;
  }

  return candidate;
}

export function withReturnTo(path: string, returnTo: string) {
  return `${path}?returnTo=${encodeURIComponent(returnTo)}`;
}

export function withTenant(path: string, tenantId?: string | null) {
  if (!tenantId) {
    return path;
  }

  return `${path}?tenantId=${encodeURIComponent(tenantId)}`;
}
