"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import { apiClient } from "@/shared/api/client";
import { routes } from "@/shared/config/routes";
import { Button } from "@/shared/ui/button";
import { Card, CardDescription, CardHeader, CardTitle } from "@/shared/ui/card";
import { FormField, TextInput } from "@/shared/ui/form-field";

export function PlatformStepUpGate({ sessionEmail }: { sessionEmail: string }) {
  const router = useRouter();
  const [password, setPassword] = useState("");
  const [twoFactorCode, setTwoFactorCode] = useState("");
  const [recoveryCode, setRecoveryCode] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      const { data, response } = await apiClient.POST("/api/session/step-up", {
        body: {
          password,
          recoveryCode: recoveryCode || null,
          twoFactorCode: twoFactorCode || null
        }
      });

      if (!response.ok || !data?.isSatisfied) {
        setError(getStepUpErrorCopy(response.status));
        return;
      }

      setPassword("");
      setTwoFactorCode("");
      setRecoveryCode("");
      router.refresh();
    } catch {
      setError("Step-up doğrulaması şu anda tamamlanamadı. Lütfen tekrar dene.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="studio-grid grid min-h-screen place-items-center px-4 py-10">
      <Card className="fade-up w-full max-w-3xl p-7 sm:p-9">
        <CardHeader>
          <p className="w-fit rounded-full bg-[var(--rs-warning-soft)] px-4 py-2 text-sm font-medium text-[var(--rs-warning)]">
            PlatformAdmin step-up gerekli
          </p>
          <CardTitle className="mt-5 text-4xl sm:text-5xl">
            Yüksek riskli platform ekranı için oturumu güçlendir.
          </CardTitle>
          <CardDescription className="max-w-2xl">
            {sessionEmail} hesabı PlatformAdmin rolüne sahip, ancak geçerli
            MFA/step-up oturumu yok. Admin API çağrıları bu doğrulama olmadan
            yapılmaz.
          </CardDescription>
        </CardHeader>

        <form className="mt-7 space-y-5" onSubmit={handleSubmit}>
          <FormField label="Parola">
            <TextInput
              autoComplete="current-password"
              onChange={(event) => setPassword(event.target.value)}
              required
              type="password"
              value={password}
            />
          </FormField>
          <div className="grid gap-4 sm:grid-cols-2">
            <FormField hint="Authenticator kodu varsa gir." label="MFA kodu">
              <TextInput
                autoComplete="one-time-code"
                inputMode="numeric"
                onChange={(event) => setTwoFactorCode(event.target.value)}
                value={twoFactorCode}
              />
            </FormField>
            <FormField hint="MFA yerine recovery code kullanılabilir." label="Recovery code">
              <TextInput
                autoComplete="one-time-code"
                onChange={(event) => setRecoveryCode(event.target.value)}
                value={recoveryCode}
              />
            </FormField>
          </div>

          {error ? (
            <p className="rounded-2xl border border-[var(--rs-warning-border)] bg-[var(--rs-warning-soft)] px-4 py-3 text-sm text-[var(--rs-warning)]">
              {error}
            </p>
          ) : null}

          <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
            <Button disabled={isSubmitting} type="submit">
              {isSubmitting ? "Doğrulanıyor..." : "Step-up oturumu aç"}
            </Button>
            <Button asChild variant="secondary">
              <Link href={routes.public.home}>Ana sayfaya dön</Link>
            </Button>
          </div>
        </form>
      </Card>
    </main>
  );
}

function getStepUpErrorCopy(status: number) {
  if (status === 400) {
    return "Step-up isteği eksik veya geçersiz.";
  }

  if (status === 401) {
    return "Parola veya doğrulama kodu kabul edilmedi.";
  }

  if (status === 422) {
    return "Bu hesap için MFA kodu veya recovery code gerekli.";
  }

  if (status === 429) {
    return "Çok fazla step-up denemesi yapıldı. Lütfen kısa süre sonra tekrar dene.";
  }

  return "Step-up doğrulaması tamamlanamadı.";
}
