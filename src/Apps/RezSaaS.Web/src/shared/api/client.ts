import createClient, { type HeadersOptions } from "openapi-fetch";
import type { paths } from "./rezsaas-api.generated";

type ApiClientOptions = {
  baseUrl?: string;
  headers?: HeadersOptions;
};

const browserBaseUrl = process.env.NEXT_PUBLIC_REZSAAS_API_BASE_URL ?? "";
export const tenantContextHeaderName = "X-RezSaaS-Tenant";

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
