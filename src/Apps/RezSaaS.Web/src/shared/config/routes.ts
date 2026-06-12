export const routes = {
  auth: {
    forgotPassword: "/sifremi-unuttum",
    login: "/giris",
    register: "/kayit",
    resetPassword: "/sifre-sifirla"
  },
  business: {
    panel: "/panel"
  },
  customer: {
    requests: "/hesabim/talepler"
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
