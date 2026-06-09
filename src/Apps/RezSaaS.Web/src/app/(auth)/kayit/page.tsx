import type { Metadata } from "next";
import Link from "next/link";
import { AuthShell } from "@/features/auth/components/auth-shell";
import { RegisterForm } from "@/features/auth/components/register-form";
import { routes } from "@/shared/config/routes";

export const metadata: Metadata = {
  robots: {
    index: false
  },
  title: "Kayıt"
};

export default function RegisterPage() {
  return (
    <AuthShell
      description="Platform-global kullanıcı hesabı açılır; tenant rolü global Identity rolüne dönüştürülmez."
      footer={
        <>
          Zaten hesabın var mı?{" "}
          <Link
            className="font-medium text-[var(--rs-ink)] underline underline-offset-4"
            href={routes.auth.login}
          >
            Giriş yap
          </Link>
        </>
      }
      title="Hesap oluştur"
    >
      <RegisterForm />
    </AuthShell>
  );
}
