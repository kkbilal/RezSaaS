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
  // Only pass returnTo to the login form when it was explicitly provided by
  // the caller (e.g. redirect after trying to access a protected page).
  // When absent, the form will determine the destination from the session.
  const returnTo = params.returnTo
    ? normalizeReturnTo(params.returnTo, undefined)
    : undefined;

  return (
    <AuthShell
      description="Tek hesapla giriş yap; yetkili olduğun işletme ve panel alanları girişten sonra açılır."
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
