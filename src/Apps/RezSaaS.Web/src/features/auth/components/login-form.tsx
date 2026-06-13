"use client";

import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import { apiClient } from "@/shared/api/client";
import { Button } from "@/shared/ui/button";
import { FormField, TextInput } from "@/shared/ui/form-field";

export function LoginForm({ returnTo }: { returnTo: string }) {
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

      router.replace(returnTo);
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

      <div className="grid gap-4 rounded-[1.5rem] border border-[var(--rs-border)] bg-white/65 p-4 sm:grid-cols-2">
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
