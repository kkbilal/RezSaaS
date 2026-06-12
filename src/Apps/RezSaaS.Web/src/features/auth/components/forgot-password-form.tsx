"use client";

import { useState, type FormEvent } from "react";
import { apiClient } from "@/shared/api/client";
import { Button } from "@/shared/ui/button";
import { FormField, TextInput } from "@/shared/ui/form-field";

export function ForgotPasswordForm() {
  const [email, setEmail] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setMessage(null);
    setIsSubmitting(true);

    try {
      const { response } = await apiClient.POST("/api/auth/forgotPassword", {
        body: {
          email
        }
      });

      if (!response.ok) {
        setError("Şifre sıfırlama isteği alınamadı. E-posta formatını kontrol et.");
        return;
      }

      setMessage("Eğer hesap uygunsa şifre sıfırlama yönergesi gönderildi.");
    } catch {
      setError("Şifre sıfırlama şu anda başlatılamadı. Lütfen kısa süre sonra tekrar dene.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form className="space-y-5" onSubmit={handleSubmit}>
      <FormField hint="Bu ekran hesap varlığını açık etmez." label="E-posta">
        <TextInput
          autoComplete="email"
          inputMode="email"
          onChange={(event) => setEmail(event.target.value)}
          required
          type="email"
          value={email}
        />
      </FormField>

      {error ? <p className="text-sm leading-6 text-[var(--rs-danger)]">{error}</p> : null}
      {message ? <p className="text-sm leading-6 text-[var(--rs-success)]">{message}</p> : null}

      <Button className="w-full" disabled={isSubmitting} type="submit">
        {isSubmitting ? "Gönderiliyor..." : "Sıfırlama iste"}
      </Button>
    </form>
  );
}
