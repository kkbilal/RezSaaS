"use client";

import { useState, type FormEvent } from "react";
import { apiClient } from "@/shared/api/client";
import { Button } from "@/shared/ui/button";
import { FormField, TextInput } from "@/shared/ui/form-field";

type ResetPasswordFormProps = {
  defaultCode?: string;
  defaultEmail?: string;
};

export function ResetPasswordForm({
  defaultCode = "",
  defaultEmail = ""
}: ResetPasswordFormProps) {
  const [email, setEmail] = useState(defaultEmail);
  const [resetCode, setResetCode] = useState(defaultCode);
  const [newPassword, setNewPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setMessage(null);
    setIsSubmitting(true);

    try {
      const { response } = await apiClient.POST("/api/auth/resetPassword", {
        body: {
          email,
          newPassword,
          resetCode
        }
      });

      if (!response.ok) {
        setError("Şifre sıfırlanamadı. Kodun süresini ve parola gereksinimlerini kontrol et.");
        return;
      }

      setMessage("Parola güncellendi. Şimdi giriş yapabilirsin.");
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

      <FormField label="Sıfırlama kodu">
        <TextInput
          autoComplete="one-time-code"
          onChange={(event) => setResetCode(event.target.value)}
          required
          type="text"
          value={resetCode}
        />
      </FormField>

      <FormField label="Yeni parola">
        <TextInput
          autoComplete="new-password"
          minLength={12}
          onChange={(event) => setNewPassword(event.target.value)}
          required
          type="password"
          value={newPassword}
        />
      </FormField>

      {error ? <p className="text-sm leading-6 text-[var(--rs-danger)]">{error}</p> : null}
      {message ? <p className="text-sm leading-6 text-[var(--rs-success)]">{message}</p> : null}

      <Button className="w-full" disabled={isSubmitting} type="submit">
        {isSubmitting ? "Güncelleniyor..." : "Parolayı güncelle"}
      </Button>
    </form>
  );
}
