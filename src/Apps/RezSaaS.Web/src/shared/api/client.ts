import createClient, { type HeadersOptions } from "openapi-fetch";
import type { paths } from "./rezsaas-api.generated";

type ApiClientOptions = {
  baseUrl?: string;
  headers?: HeadersOptions;
};

export const tenantContextHeaderName = "X-RezSaaS-Tenant";

// Browser-side API calls MUST go through the Next.js rewrite proxy
// (relative "/api/..." → backend "http://localhost:5252/api/..."). This keeps
// auth cookies same-origin so that Next.js server components can read them via
// cookies() during SSR. Only set NEXT_PUBLIC_REZSAAS_API_BASE_URL if the API
// is on a different origin in production and the cookie domain is configured
// accordingly (cross-site cookies require SameSite=None;Secure).
const browserBaseUrl = process.env.NEXT_PUBLIC_REZSAAS_API_BASE_URL ?? "";

export function createApiClient(options: ApiClientOptions = {}) {
  return createClient<paths>({
    baseUrl: options.baseUrl ?? browserBaseUrl,
    credentials: "include",
    headers: options.headers
  });
}

export function createTenantApiClient(tenantId: string) {
  return createApiClient({
    headers: {
      [tenantContextHeaderName]: tenantId
    }
  });
}

export const apiClient = createApiClient();