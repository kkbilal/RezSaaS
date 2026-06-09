import Link from "next/link";
import { routes } from "@/shared/config/routes";
import { Button } from "@/shared/ui/button";

export default function HomePage() {
  return (
    <main className="studio-grid min-h-screen px-6 py-12">
      <div className="mx-auto flex min-h-[calc(100vh-6rem)] max-w-4xl items-center">
        <section className="fade-up max-w-2xl space-y-8">
          <p className="w-fit rounded-full border border-[var(--rs-border)] bg-white/70 px-4 py-2 text-sm text-[var(--rs-muted)] shadow-[var(--rs-shadow-soft)]">
            RezSaaS Business UI
          </p>
          <div className="space-y-5">
            <h1 className="text-5xl font-semibold tracking-[-0.06em] text-[var(--rs-ink)] sm:text-7xl">
              Operasyon paneli, sakin ama keskin.
            </h1>
            <p className="max-w-xl text-lg leading-8 text-[var(--rs-muted-strong)]">
              RezSaaS domain doğruluğu ile modern studio estetiğini birleştiren panel
              prototipi artık cookie session guard arkasında ilerliyor.
            </p>
          </div>
          <div className="flex flex-wrap gap-3">
            <Button asChild>
              <Link href={routes.auth.login}>Giriş yap</Link>
            </Button>
            <Button asChild variant="secondary">
              <Link href={routes.business.panel}>Paneli aç</Link>
            </Button>
          </div>
        </section>
      </div>
    </main>
  );
}
