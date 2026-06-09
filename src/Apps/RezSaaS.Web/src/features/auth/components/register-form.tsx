"use client";

import { useState, type FormEvent } from "react";
import { apiClient } from "@/shared/api/client";
import { Button } from "@/shared/ui/button";
import { FormField, TextInput } from "@/shared/ui/form-field";

export function RegisterForm() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setMessage(null);
    setIsSubmitting(true);

    try {
      const { response } = await apiClient.POST("/api/auth/register", {
        body: {
          email,
          password
        }
      });

      if (!response.ok) {
        setError("Kayıt alınamadı. E-posta ve parola gereksinimlerini kontrol et.");
        return;
      }

      setMessage("Kayıt alındı. Production ortamında e-posta doğrulaması gerekir.");
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

      <FormField hint="Backend Identity politikası nihai otoritedir." label="Parola">
        <TextInput
          autoComplete="new-password"
          minLength={12}
          onChange={(event) => setPassword(event.target.value)}
          required
          type="password"
          value={password}
        />
      </FormField>

      {error ? <p className="text-sm leading-6 text-[var(--rs-danger)]">{error}</p> : null}
      {message ? <p className="text-sm leading-6 text-[var(--rs-success)]">{message}</p> : null}

      <Button className="w-full" disabled={isSubmitting} type="submit">
        {isSubmitting ? "Kaydediliyor..." : "Hesap oluştur"}
      </Button>
    </form>
  );
}
