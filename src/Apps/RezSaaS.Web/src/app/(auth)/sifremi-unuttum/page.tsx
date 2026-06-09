import type { Metadata } from "next";
import Link from "next/link";
import { AuthShell } from "@/features/auth/components/auth-shell";
import { ForgotPasswordForm } from "@/features/auth/components/forgot-password-form";
import { routes } from "@/shared/config/routes";

export const metadata: Metadata = {
  robots: {
    index: false
  },
  title: "Şifremi Unuttum"
};

export default function ForgotPasswordPage() {
  return (
    <AuthShell
      description="Hesap varlığı sızdırmadan şifre sıfırlama isteği oluşturur."
      footer={
        <Link
          className="font-medium text-[var(--rs-ink)] underline underline-offset-4"
          href={routes.auth.login}
        >
          Girişe dön
        </Link>
      }
      title="Şifre sıfırlama"
    >
      <ForgotPasswordForm />
    </AuthShell>
  );
}
