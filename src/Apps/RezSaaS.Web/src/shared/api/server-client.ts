import { createApiClient, tenantContextHeaderName } from "./client";
import "server-only";

// Server-side (RSC/SSR) API calls go directly to the backend API, forwarding
// the browser's Cookie header so authenticated requests work during SSR.
// REZSAAS_API_BASE_URL must be set; in development it defaults to localhost:5252.
const isDevelopment = process.env.NODE_ENV === "development";
const serverBaseUrl = process.env.REZSAAS_API_BASE_URL;

if (!serverBaseUrl) {
  if (isDevelopment) {
    console.warn("REZSAAS_API_BASE_URL not set, falling back to http://localhost:5252 (development only)");
  } else {
    throw new Error("REZSAAS_API_BASE_URL environment variable is required in production");
  }
}

const baseUrl = serverBaseUrl ?? "http://localhost:5252";

export function createServerApiClient(cookieHeader?: string, tenantId?: string) {
  const headers = {
    ...(cookieHeader ? { cookie: cookieHeader } : {}),
    ...(tenantId ? { [tenantContextHeaderName]: tenantId } : {})
  };

  return createApiClient({
    baseUrl,
    headers: Object.keys(headers).length > 0 ? headers : undefined
  });
}
