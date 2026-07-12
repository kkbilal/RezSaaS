import type { Metadata } from "next";
import { CustomerShell } from "@/features/customer/components/customer-shell";
import { PrivateRouteState } from "@/features/session/components/private-route-state";
import { requireSession } from "@/features/session/lib/guards";
import { routes } from "@/shared/config/routes";
import { Card, CardDescription, CardHeader, CardTitle } from "@/shared/ui/card";
import { EmptyState } from "@/shared/ui/empty-state";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  robots: {
    index: false
  },
  title: "Profilim"
};

export default async function CustomerProfileRoute() {
  const sessionState = await requireSession(routes.customer.profile);

  if (sessionState.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.auth.login}
        actionLabel="Giriş ekranına git"
        description={`${sessionState.reason} Lütfen yeniden giriş yapmayı dene.`}
        title="Oturum doğrulanamadı"
      />
    );
  }

  const account = sessionState.session.account;
  const email = account?.email ?? "Hesap";
  const displayName =
    typeof account === "object" && account && "displayName" in account
      ? String((account as { displayName?: unknown }).displayName ?? "")
      : "";

  return (
    <CustomerShell
      activeNav="profile"
      sessionDisplayName={displayName || undefined}
      sessionEmail={email}
    >
      <div className="space-y-6">
        <section className="fade-up">
          <p className="w-fit rounded-full bg-[var(--rs-accent-soft)] px-4 py-2 text-xs font-medium uppercase tracking-[0.18em] text-[var(--rs-accent-strong)]">
            Hesabım
          </p>
          <h1 className="mt-4 text-4xl font-semibold tracking-[-0.07em] text-[var(--rs-ink)] sm:text-5xl">
            Profil
          </h1>
          <p className="mt-3 max-w-2xl text-base leading-7 text-[var(--rs-muted-strong)]">
            Hesap bilgilerin ve kimliğin burada görünür. Profil yönetimi ve hesap
            kapatma akışı henüz bekleyen API yüzeyleriyle birlikte açılacak.
          </p>
        </section>

        <Card className="p-6 sm:p-8">
          <CardHeader>
            <CardTitle>Hesap özeti</CardTitle>
            <CardDescription>
              Bilgiler doğrulanmış oturumundan okunur; mock veri taşımaz.
            </CardDescription>
          </CardHeader>
          <dl className="mt-5 grid gap-4 sm:grid-cols-2">
            <div className="rounded-[var(--rs-radius-lg)] border border-[var(--rs-border)] bg-[var(--rs-surface-muted)] px-4 py-3">
              <dt className="text-[0.65rem] font-semibold uppercase tracking-[0.16em] text-[var(--rs-muted)]">
                E-posta
              </dt>
              <dd className="mt-1 text-sm font-medium text-[var(--rs-ink)]">
                {email}
              </dd>
            </div>
            <div className="rounded-[var(--rs-radius-lg)] border border-[var(--rs-border)] bg-[var(--rs-surface-muted)] px-4 py-3">
              <dt className="text-[0.65rem] font-semibold uppercase tracking-[0.16em] text-[var(--rs-muted)]">
                Görünen ad
              </dt>
              <dd className="mt-1 text-sm font-medium text-[var(--rs-ink)]">
                {displayName || "Belirtilmedi"}
              </dd>
            </div>
          </dl>
        </Card>

        <Card className="p-6 sm:p-8">
          <EmptyState
            title="Profil yönetimi yakında"
            description="Ad, telefon ve hesap kapatma gibi işlemler, ilgili backend endpoint'leri hazır olduğunda bu ekranda açılacak. Şimdilik bilgilerin doğrulanmış oturumundan okunuyor."
          />
        </Card>
      </div>
    </CustomerShell>
  );
}
