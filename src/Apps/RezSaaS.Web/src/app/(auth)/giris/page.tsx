import type { Metadata } from "next";
import Link from "next/link";
import { AuthShell } from "@/features/auth/components/auth-shell";
import { LoginForm } from "@/features/auth/components/login-form";
import { normalizeReturnTo, routes } from "@/shared/config/routes";

type LoginPageProps = {
  searchParams: Promise<{
    returnTo?: string | string[];
  }>;
};

export const metadata: Metadata = {
  robots: {
    index: false
  },
  title: "Giriş"
};

export default async function LoginPage({ searchParams }: LoginPageProps) {
  const params = await searchParams;
  const returnTo = normalizeReturnTo(params.returnTo, routes.business.panel);

  return (
    <AuthShell
      description="Cookie tabanlı oturum açılır; bearer/access token browser storage içine yazılmaz."
      footer={
        <>
          Hesabın yok mu?{" "}
          <Link
            className="font-medium text-[var(--rs-ink)] underline underline-offset-4"
            href={routes.auth.register}
          >
            Kayıt ol
          </Link>
          {" · "}
          <Link
            className="font-medium text-[var(--rs-ink)] underline underline-offset-4"
            href={routes.auth.forgotPassword}
          >
            Şifremi unuttum
          </Link>
        </>
      }
      title="Giriş yap"
    >
      <LoginForm returnTo={returnTo} />
    </AuthShell>
  );
}
