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
          password
        },
        params: {
          query: {
            useCookies: true,
            useSessionCookies: true
          }
        }
      });

      if (!response.ok) {
        setError("Giriş başarısız. E-posta, parola veya hesap durumunu kontrol et.");
        return;
      }

      router.replace(returnTo);
      router.refresh();
    } catch {
      setError("API'ye ulaşılamadı. Backend veya Next proxy ayarını kontrol et.");
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

      {error ? <p className="text-sm leading-6 text-[var(--rs-danger)]">{error}</p> : null}

      <Button className="w-full" disabled={isSubmitting} type="submit">
        {isSubmitting ? "Giriş yapılıyor..." : "Giriş yap"}
      </Button>
    </form>
  );
}
