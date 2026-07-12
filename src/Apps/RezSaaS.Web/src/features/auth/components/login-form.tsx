"use client";

import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import { apiClient } from "@/shared/api/client";
import { routes } from "@/shared/config/routes";
import { Button } from "@/shared/ui/button";
import { FormField, TextInput } from "@/shared/ui/form-field";

type RoleTarget = "platform" | "business" | "customer" | "default";

function resolveRoleTarget(session: {
  platformRoles?: string[] | null;
  tenantMemberships?:
    | { role?: string | null }[]
    | null;
}): RoleTarget {
  const isPlatformAdmin =
    session.platformRoles?.some((role) =>
      ["PlatformAdmin", "PlatformSupport"].includes(role)
    ) ?? false;
  if (isPlatformAdmin) return "platform";

  const isBusiness =
    session.tenantMemberships?.some((m) =>
      m.role ? ["BusinessOwner", "BranchManager", "Staff"].includes(m.role) : false
    ) ?? false;
  if (isBusiness) return "business";

  return "customer";
}

function routeForRole(target: RoleTarget): string {
  switch (target) {
    case "platform":
      return routes.platform.tenants;
    case "business":
      return routes.business.panel;
    case "customer":
      return routes.customer.appointments;
    default:
      return routes.business.panel;
  }
}

export function LoginForm({ returnTo }: { returnTo?: string }) {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [twoFactorCode, setTwoFactorCode] = useState("");
  const [twoFactorRecoveryCode, setTwoFactorRecoveryCode] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      const { response } = await apiClient.POST("/api/auth/login", {
        body: {
          email,
          password,
          twoFactorCode: twoFactorCode || null,
          twoFactorRecoveryCode: twoFactorRecoveryCode || null
        },
        params: {
          query: {
            useCookies: true,
            useSessionCookies: true
          }
        }
      });

      if (!response.ok) {
        setError(getLoginErrorCopy(response.status));
        return;
      }

      // If an explicit returnTo was provided (e.g. user tried to access a
      // specific page), honor it. Otherwise determine the destination based
      // on the authenticated user's roles/memberships.
      if (returnTo) {
        router.replace(returnTo);
        router.refresh();
        return;
      }

      // Fetch the session to determine role-based redirect.
      const { data: session, response: sessionResponse } =
        await apiClient.GET("/api/session/bootstrap");

      if (!sessionResponse.ok || !session?.account) {
        // Session lookup failed; fall back to the business panel.
        router.replace(routes.business.panel);
        router.refresh();
        return;
      }

      router.replace(routeForRole(resolveRoleTarget(session)));
      router.refresh();
    } catch {
      setError("Giriş şu anda tamamlanamadı. Lütfen kısa süre sonra tekrar dene.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form className="space-y-5" onSubmit={handleSubmit}>
      <FormField label="E-posta">
        <TextInput
          autoComplete="email"
          inputMode="email"
          onChange={(event) => setEmail(event.target.value)}
          required
          type="email"
          value={email}
        />
      </FormField>

      <FormField label="Parola">
        <TextInput
          autoComplete="current-password"
          onChange={(event) => setPassword(event.target.value)}
          required
          type="password"
          value={password}
        />
      </FormField>

      <div className="grid gap-4 rounded-[1.5rem] border border-[var(--rs-border)] bg-[var(--rs-glass)] p-4 sm:grid-cols-2">
        <FormField
          hint="MFA açık hesaplarda authenticator kodu gir."
          label="MFA kodu"
        >
          <TextInput
            autoComplete="one-time-code"
            inputMode="numeric"
            onChange={(event) => setTwoFactorCode(event.target.value)}
            value={twoFactorCode}
          />
        </FormField>

        <FormField
          hint="Authenticator yerine recovery code kullanılabilir."
          label="Recovery code"
        >
          <TextInput
            autoComplete="one-time-code"
            onChange={(event) => setTwoFactorRecoveryCode(event.target.value)}
            value={twoFactorRecoveryCode}
          />
        </FormField>
      </div>

      {error ? <p className="text-sm leading-6 text-[var(--rs-danger)]">{error}</p> : null}

      <Button className="w-full" disabled={isSubmitting} type="submit">
        {isSubmitting ? "Giriş yapılıyor..." : "Giriş yap"}
      </Button>
    </form>
  );
}

function getLoginErrorCopy(status: number) {
  if (status === 401) {
    return "Giriş başarısız. E-posta, parola veya MFA/recovery kodunu kontrol et.";
  }

  if (status === 429) {
    return "Çok fazla giriş denemesi yapıldı. Lütfen kısa süre sonra tekrar dene.";
  }

  return "Giriş başarısız. Hesap durumunu ve bilgileri kontrol et.";
}