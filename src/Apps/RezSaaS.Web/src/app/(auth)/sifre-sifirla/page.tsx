import type { Metadata } from "next";
import Link from "next/link";
import { AuthShell } from "@/features/auth/components/auth-shell";
import { ResetPasswordForm } from "@/features/auth/components/reset-password-form";
import { routes } from "@/shared/config/routes";

type ResetPasswordPageProps = {
  searchParams: Promise<{
    code?: string | string[];
    email?: string | string[];
    resetCode?: string | string[];
  }>;
};

function first(value?: string | string[]) {
  return Array.isArray(value) ? value[0] : value;
}

export const metadata: Metadata = {
  robots: {
    index: false
  },
  title: "Şifre Sıfırla"
};

export default async function ResetPasswordPage({
  searchParams
}: ResetPasswordPageProps) {
  const params = await searchParams;

  return (
    <AuthShell
      description="Sana iletilen kodla yeni parolanı belirle."
      footer={
        <Link
          className="font-medium text-[var(--rs-ink)] underline underline-offset-4"
          href={routes.auth.login}
        >
          Girişe dön
        </Link>
      }
      title="Yeni parola belirle"
    >
      <ResetPasswordForm
        defaultCode={first(params.code) ?? first(params.resetCode)}
        defaultEmail={first(params.email)}
      />
    </AuthShell>
  );
}
