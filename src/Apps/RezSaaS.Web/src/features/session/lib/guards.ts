import { redirect } from "next/navigation";
import {
  getSessionState,
  type SessionState
} from "@/features/session/api/get-session-bootstrap";
import { routes, withReturnTo } from "@/shared/config/routes";

export async function requireSession(
  returnTo: string = routes.business.panel
): Promise<Extract<SessionState, { kind: "authenticated" }> | Extract<SessionState, { kind: "unavailable" }>> {
  const state = await getSessionState();

  if (state.kind === "unauthenticated") {
    redirect(withReturnTo(routes.auth.login, returnTo));
  }

  return state;
}
