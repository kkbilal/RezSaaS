import { cookies } from "next/headers";
import { createServerApiClient } from "@/shared/api/server-client";
import type { ApiSchema } from "@/shared/api/types";

export type SessionBootstrap = ApiSchema<"SessionBootstrapResponse">;

export type SessionState =
  | {
      kind: "authenticated";
      session: SessionBootstrap;
    }
  | {
      kind: "unauthenticated";
    }
  | {
      kind: "unavailable";
      reason: string;
    };

export async function getSessionState(): Promise<SessionState> {
  try {
    const cookieHeader = (await cookies()).toString();
    const { data, response } = await createServerApiClient(cookieHeader).GET(
      "/api/session/bootstrap"
    );

    if (response.status === 401) {
      return {
        kind: "unauthenticated"
      };
    }

    if (!response.ok) {
      return {
        kind: "unavailable",
        reason: "Oturum şu anda doğrulanamadı."
      };
    }

    if (!data?.account) {
      return {
        kind: "unauthenticated"
      };
    }

    return {
      kind: "authenticated",
      session: data
    };
  } catch {
    return {
      kind: "unavailable",
      reason: "Oturum şu anda doğrulanamadı."
    };
  }
}
