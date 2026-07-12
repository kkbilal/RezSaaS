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
    // Musterinin BIRINCIL sayfasi. GET /api/customer/appointment-history hem talepleri
    // hem randevulari doner; ekran bunlari "Yaklasan | Gecmis" olarak gosterir.
    //
    // "talepler" ANAHTARI KALDIRILDI: musterinin zihninde "talep" diye bir nesne yok --
    // "randevu aldim, onay bekliyorum" var. Talep/randevu ayrimi artik bir ROZET, rota degil.
    // /hesabim/talepler eski linkler icin buraya yonlenen bir redirect olarak duruyor
    // (sayfasi var ama routes'ta anahtari yok; nav-manifest'te hidden kayitli).
    appointments: "/hesabim/randevular",
    dashboard: "/hesabim",
    profile: "/hesabim/profil"
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
