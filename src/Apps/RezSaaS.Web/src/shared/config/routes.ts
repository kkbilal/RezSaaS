export const routes = {
  auth: {
    dispatch: "/gelis",
    emailVerify: "/eposta-dogrula",
    forgotPassword: "/sifremi-unuttum",
    login: "/giris",
    mfaStepUp: "/platform/adim",
    register: "/kayit",
    resetPassword: "/sifre-sifirla"
  },
  business: {
    panel: "/panel",
    abuseReports: "/panel/abuse-raporlari",
    appointmentDetail: (appointmentId: string) =>
      `/panel/randevular/${encodeURIComponent(appointmentId)}`,
    appointmentOperations: (appointmentId: string) =>
      `/panel/randevular/${encodeURIComponent(appointmentId)}/islem`,
    appointments: "/panel/randevular",
    branches: "/panel/subeler",
    calendar: "/panel/takvim",
    messaging: "/panel/mesajlar",
    requests: "/panel/talepler",
    resources: "/panel/kaynaklar",
    resourceTypes: "/panel/kaynak-turleri",
    reviews: "/panel/degerlendirmeler",
    services: "/panel/hizmetler",
    settings: "/panel/ayarlar",
    skills: "/panel/yetenekler",
    staff: "/panel/personel",
    workingHours: "/panel/calisma-saatleri"
  },
  customer: {
    appeals: "/hesabim/itirazlar",
    appointmentDetail: (appointmentId: string) =>
      `/hesabim/talepler?talep=${encodeURIComponent(appointmentId)}`,
    appointments: "/hesabim/talepler",
    dashboard: "/hesabim",
    profile: "/hesabim/profil",
    requests: "/hesabim/talepler",
    reviews: "/hesabim/degerlendirmeler"
  },
  platform: {
    abuse: "/platform/abuse",
    abuseUser: (userAccountId: string) =>
      `/platform/abuse/kullanici/${encodeURIComponent(userAccountId)}`,
    appeals: "/platform/itirazlar",
    auditLog: "/platform/denetim-gunlugu",
    dashboard: "/platform",
    identities: "/platform/kimlikler",
    sanctions: "/platform/cezalar",
    support: "/platform/destek",
    tenants: "/platform/tenantlar",
    tenantMembers: (tenantId: string) =>
      `/platform/tenantlar/${encodeURIComponent(tenantId)}/uyeler`,
    newTenant: "/platform/tenantlar/yeni"
  },
  public: {
    booking: (businessSlug: string) =>
      `/isletme/${encodeURIComponent(businessSlug)}/rezervasyon`,
    businessProfile: (businessSlug: string) =>
      `/isletme/${encodeURIComponent(businessSlug)}`,
    businessReviews: (businessSlug: string) =>
      `/isletme/${encodeURIComponent(businessSlug)}/degerlendirmeler`,
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
