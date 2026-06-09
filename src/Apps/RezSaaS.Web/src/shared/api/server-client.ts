import { createApiClient, tenantContextHeaderName } from "./client";

const serverBaseUrl = process.env.REZSAAS_API_BASE_URL ?? "http://localhost:5252";

export function createServerApiClient(cookieHeader?: string, tenantId?: string) {
  const headers = {
    ...(cookieHeader ? { cookie: cookieHeader } : {}),
    ...(tenantId ? { [tenantContextHeaderName]: tenantId } : {})
  };

  return createApiClient({
    baseUrl: serverBaseUrl,
    headers: Object.keys(headers).length > 0 ? headers : undefined
  });
}
