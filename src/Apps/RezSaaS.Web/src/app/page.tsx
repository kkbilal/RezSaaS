import Link from "next/link";
import { routes } from "@/shared/config/routes";
import { Button } from "@/shared/ui/button";
import { Card } from "@/shared/ui/card";
import { PublicNavbar } from "@/shared/ui/public-navbar";

const productPillars = [
  {
    title: "Onaylı rezervasyon akışı",
    text: "Müşteri talebi önce işletmeye düşer; kesin randevu ancak yetkili onayından sonra oluşur."
  },
  {
    title: "Personel + kaynak planlama",
    text: "Her randevu uygun personel ve fiziksel kapasiteyle birlikte değerlendirilir; koltuk, oda, yatak veya cihaz aynı modelde yönetilir."
  },
  {
    title: "Şube saatine sadık deneyim",
    text: "Randevu saatleri işletmenin şube zaman dilimiyle gösterilir; kullanıcıya sessizce dönüştürülmüş saat sunulmaz."
  }
];

const flowSteps = [
  "Müşteri hizmeti ve uygun zamanı seçer.",
  "Talep işletmenin onay kutusuna düşer.",
  "Yetkili kişi uygun talebi onaylar veya reddeder.",
  "Çakışan talepler ve süresi dolan istekler net durumlarla ayrışır."
];

const categories = [
  { key: "hair", label: "Saç & Güzellik", hint: "Kuaför, berber, güzellik salonu" },
  { key: "spa", label: "Spa & Masaj", hint: "Masaj, bakım, wellness" },
  { key: "fitness", label: "Spor & Fitness", hint: "Stüdyo, PT, ders" },
  { key: "nail", label: "Tırnak & Bakım", hint: "Manikür, pedikür, atölye" },
  { key: "dental", label: "Diş Sağlığı", hint: "Klinik, muayene, kontrol" },
  { key: "grooming", label: "Erkek Bakımı", hint: "Berber, tıraş, cilt" }
];

const plans = [
  {
    eyebrow: "Tek şube",
    features: ["1 şube", "5 personele kadar", "Sınırsız rezervasyon talebi", "Temel rol yönetimi"],
    name: "Starter",
    price: "₺990-1.290",
    suffix: "/ay"
  },
  {
    eyebrow: "Büyüyen ekipler",
    features: ["Gelişmiş yetki filtreleri", "Kaynak analitiği", "Paket ve üyelik hazırlığı", "Daha yüksek SMS kotası"],
    highlighted: true,
    name: "Growth",
    price: "₺1.790-2.390",
    suffix: "/ay"
  },
  {
    eyebrow: "Çoklu şube",
    features: ["Çoklu şube operasyonu", "Onboarding desteği", "API ve webhook hazırlığı", "Öncelikli destek"],
    name: "Scale",
    price: "₺3.290-4.490",
    suffix: "/ay"
  }
];

export default function HomePage() {
  return (
    <main className="min-h-screen">
      <PublicNavbar />

      {/* Hero */}
      <section className="mx-auto max-w-7xl px-4 pb-16 pt-28 sm:px-6 sm:pt-32">
        <div className="fade-up mx-auto max-w-4xl text-center">
          <p className="mx-auto w-fit rounded-full border border-[var(--rs-border)] bg-[var(--rs-glass)] px-4 py-1.5 font-mono text-[11px] uppercase tracking-[0.18em] text-[var(--rs-accent-strong)] backdrop-blur-xl">
            Salon, spa, klinik ve stüdyo ekipleri için
          </p>
          <h1
            className="mt-6 text-5xl font-bold leading-[1.05] tracking-[-0.04em] text-[var(--rs-ink)] sm:text-7xl"
            style={{ fontFamily: "var(--rs-font-display)" }}
          >
            Rezervasyonu onaya,
            <br />
            <span className="rs-gradient-text">operasyonu netliğe</span> bağla.
          </h1>
          <p className="mx-auto mt-6 max-w-2xl text-base leading-7 text-[var(--rs-muted-strong)] sm:text-lg">
            RezSaaS; müşteri keşfi, işletme onayı, personel ve kaynak kapasitesi,
            şube saati ve kötüye kullanım dayanımını aynı ürün akışında birleştiren
            modern rezervasyon platformudur.
          </p>
          <div className="mt-8 flex flex-wrap justify-center gap-3">
            <Button asChild size="lg">
              <Link href={routes.auth.register}>Ücretsiz başla</Link>
            </Button>
            <Button asChild size="lg" variant="secondary">
              <Link href={routes.public.discover}>İşletmeleri keşfet</Link>
            </Button>
          </div>
        </div>
      </section>

      {/* Categories */}
      <section className="mx-auto max-w-7xl px-4 py-12 sm:px-6">
        <div className="mb-8 flex items-end justify-between">
          <div>
            <p className="font-mono text-[11px] uppercase tracking-[0.2em] text-[var(--rs-muted)]">
              Kategoriler
            </p>
            <h2
              className="mt-2 text-3xl font-semibold tracking-[-0.03em] text-[var(--rs-ink)] sm:text-4xl"
              style={{ fontFamily: "var(--rs-font-display)" }}
            >
              Her kategoriye genişleyen model
            </h2>
          </div>
          <Link
            className="hidden text-sm text-[var(--rs-accent-strong)] hover:underline sm:inline"
            href={routes.public.discover}
          >
            Tümünü gör →
          </Link>
        </div>
        <div className="grid grid-cols-2 gap-3 md:grid-cols-3 lg:grid-cols-6">
          {categories.map((category) => (
            <Link
              key={category.key}
              href={`${routes.public.discover}?categoryKey=${category.key}`}
              className="group rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-glass)] p-4 backdrop-blur-xl transition-all duration-200 hover:-translate-y-0.5 hover:border-[var(--rs-border-strong)] hover:bg-[var(--rs-glass-strong)]"
            >
              <span className="rs-gradient-bg mb-3 flex h-9 w-9 items-center justify-center rounded-xl text-white shadow-[rgba(99,102,241,0.3)]">
                <CategoryIcon categoryKey={category.key} />
              </span>
              <p className="text-sm font-medium text-[var(--rs-ink)]">{category.label}</p>
              <p className="mt-0.5 text-[11px] text-[var(--rs-muted)]">{category.hint}</p>
            </Link>
          ))}
        </div>
      </section>

      {/* Product pillars */}
      <section className="mx-auto max-w-7xl px-4 py-12 sm:px-6" id="ozellikler">
        <div className="grid gap-3 lg:grid-cols-3">
          {productPillars.map((pillar, index) => (
            <Card
              className="fade-up p-6"
              key={pillar.title}
              style={{ animationDelay: `${index * 70}ms` }}
            >
              <h3
                className="text-xl font-semibold tracking-[-0.03em] text-[var(--rs-ink)]"
                style={{ fontFamily: "var(--rs-font-display)" }}
              >
                {pillar.title}
              </h3>
              <p className="mt-3 text-sm leading-7 text-[var(--rs-muted-strong)]">
                {pillar.text}
              </p>
            </Card>
          ))}
        </div>
      </section>

      {/* Flow */}
      <section className="mx-auto max-w-7xl px-4 py-16 sm:px-6" id="akis">
        <div className="grid gap-8 lg:grid-cols-[0.8fr_1fr]">
          <div className="space-y-4">
            <p className="font-mono text-[11px] uppercase tracking-[0.2em] text-[var(--rs-muted)]">
              İşleyiş
            </p>
            <h2
              className="text-4xl font-semibold tracking-[-0.04em] text-[var(--rs-ink)] sm:text-5xl"
              style={{ fontFamily: "var(--rs-font-display)" }}
            >
              Slotu hemen satmak yerine, doğru kararı hızlandırır.
            </h2>
            <p className="max-w-md text-sm leading-7 text-[var(--rs-muted-strong)]">
              PendingApproval talepler slotu bloklamaz; işletme birden fazla istek
              arasından en uygununu seçer. 24 saat üst sınır, süresi dolan talepleri
              otomatik kapatır.
            </p>
          </div>
          <div className="grid gap-3">
            {flowSteps.map((step, index) => (
              <div
                key={step}
                className="rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-glass)] p-5 backdrop-blur-xl"
              >
                <p className="font-mono text-[11px] uppercase tracking-[0.2em] text-[var(--rs-accent-strong)]">
                  {String(index + 1).padStart(2, "0")}
                </p>
                <p className="mt-2 text-base font-medium tracking-[-0.02em] text-[var(--rs-ink)]">
                  {step}
                </p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Pricing */}
      <section className="mx-auto max-w-7xl px-4 py-16 sm:px-6" id="fiyatlar">
        <div className="mb-8 text-center">
          <p className="font-mono text-[11px] uppercase tracking-[0.2em] text-[var(--rs-muted)]">
            Fiyatlar
          </p>
          <h2
            className="mt-2 text-4xl font-semibold tracking-[-0.04em] text-[var(--rs-ink)] sm:text-5xl"
            style={{ fontFamily: "var(--rs-font-display)" }}
          >
            Şube sayına ve operasyon yoğunluğuna göre
          </h2>
        </div>

        <div className="grid gap-4 lg:grid-cols-3">
          {plans.map((plan) => (
            <article
              key={plan.name}
              className={
                plan.highlighted
                  ? "rs-gradient-bg relative overflow-hidden rounded-3xl p-6 text-white shadow-[0_24px_80px_rgba(99,102,241,0.28)]"
                  : "rounded-3xl border border-[var(--rs-border)] bg-[var(--rs-glass)] p-6 backdrop-blur-xl"
              }
            >
              {plan.highlighted ? (
                <span className="absolute right-4 top-4 rounded-full bg-white/20 px-2.5 py-0.5 font-mono text-[10px] font-medium uppercase tracking-wider text-white backdrop-blur-xl">
                  Popüler
                </span>
              ) : null}
              <p className={plan.highlighted ? "text-sm text-white/70" : "text-sm text-[var(--rs-muted)]"}>
                {plan.eyebrow}
              </p>
              <h3
                className={
                  plan.highlighted
                    ? "mt-2 text-3xl font-semibold tracking-[-0.04em] text-white"
                    : "mt-2 text-3xl font-semibold tracking-[-0.04em] text-[var(--rs-ink)]"
                }
                style={{ fontFamily: "var(--rs-font-display)" }}
              >
                {plan.name}
              </h3>
              <p className="mt-6 flex items-end gap-1">
                <span className="text-4xl font-semibold tracking-[-0.04em]">{plan.price}</span>
                <span className={plan.highlighted ? "pb-1 text-white/60" : "pb-1 text-[var(--rs-muted)]"}>
                  {plan.suffix}
                </span>
              </p>
              <ul className="mt-6 space-y-2.5 text-sm leading-6">
                {plan.features.map((feature) => (
                  <li
                    className={plan.highlighted ? "flex items-center gap-2 text-white/85" : "flex items-center gap-2 text-[var(--rs-muted-strong)]"}
                    key={feature}
                  >
                    <CheckIcon highlighted={plan.highlighted} />
                    <span>{feature}</span>
                  </li>
                ))}
              </ul>
              <div className="mt-6">
                <Button
                  asChild
                  variant={plan.highlighted ? "secondary" : "outline"}
                  className="w-full"
                >
                  <Link href={routes.auth.register}>{plan.name} ile başla</Link>
                </Button>
              </div>
            </article>
          ))}
        </div>
      </section>

      {/* CTA */}
      <section className="mx-auto max-w-7xl px-4 py-16 sm:px-6">
        <div className="rs-gradient-bg relative overflow-hidden rounded-[2rem] p-8 text-center shadow-[0_24px_80px_rgba(99,102,241,0.28)] sm:p-14">
          <h2
            className="text-3xl font-semibold tracking-[-0.04em] text-white sm:text-5xl"
            style={{ fontFamily: "var(--rs-font-display)" }}
          >
            Bugün operasyonunu onay akışına taşı.
          </h2>
          <p className="mx-auto mt-3 max-w-xl text-sm leading-7 text-white/80 sm:text-base">
            Ücretsiz başla, mevcut şube ve personel ayarlarını adım adım ekle.
            Onay akışı ve şube saati sadakati ilk günden hazır.
          </p>
          <div className="mt-7 flex flex-wrap justify-center gap-3">
            <Button asChild size="lg" variant="secondary">
              <Link href={routes.auth.register}>Ücretsiz başla</Link>
            </Button>
            <Button
              asChild
              size="lg"
              className="border border-white/20 bg-white/10 text-white backdrop-blur-xl hover:bg-white/20"
            >
              <Link href={routes.public.discover}>Keşfet</Link>
            </Button>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="border-t border-[var(--rs-border)] bg-[var(--rs-glass)] backdrop-blur-xl">
        <div className="mx-auto flex max-w-7xl flex-col items-center justify-between gap-4 px-4 py-8 sm:flex-row sm:px-6">
          <div className="flex items-center gap-2">
            <span className="rs-gradient-bg flex h-7 w-7 items-center justify-center rounded-lg">
              <span className="text-xs font-bold text-white">R</span>
            </span>
            <span
              className="text-base font-semibold text-[var(--rs-ink)]"
              style={{ fontFamily: "var(--rs-font-display)" }}
            >
              RezSaaS
            </span>
          </div>
          <p className="text-xs text-[var(--rs-muted)]">
            © {new Date().getFullYear()} RezSaaS. Tüm hakları saklıdır.
          </p>
          <div className="flex items-center gap-5 text-xs text-[var(--rs-muted)]">
            <Link href={routes.public.discover} className="hover:text-[var(--rs-ink)]">
              Keşfet
            </Link>
            <Link href={routes.auth.login} className="hover:text-[var(--rs-ink)]">
              Giriş
            </Link>
          </div>
        </div>
      </footer>
    </main>
  );
}

function CategoryIcon({ categoryKey }: { categoryKey: string }) {
  const common = "h-4 w-4 text-white";
  switch (categoryKey) {
    case "hair":
      return (
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} className={common} aria-hidden>
          <path d="M5 21v-7a7 7 0 0 1 14 0v7" strokeLinecap="round" />
          <path d="M5 14h14" strokeLinecap="round" />
        </svg>
      );
    case "spa":
      return (
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} className={common} aria-hidden>
          <path d="M12 22V8" strokeLinecap="round" />
          <path d="M12 8c0-3 2-5 5-5 0 3-2 5-5 5z" strokeLinejoin="round" />
          <path d="M12 8c0-3-2-5-5-5 0 3 2 5 5 5z" strokeLinejoin="round" />
        </svg>
      );
    case "fitness":
      return (
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} className={common} aria-hidden>
          <path d="M6 4v16M18 4v16M3 9v6M21 9v6M6 12h12" strokeLinecap="round" />
        </svg>
      );
    case "nail":
      return (
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} className={common} aria-hidden>
          <path d="M9 11V5a2 2 0 0 1 4 0v6" strokeLinecap="round" />
          <path d="M13 11v6a3 3 0 0 1-6 0v-1" strokeLinecap="round" />
          <path d="M9 11h.01" strokeLinecap="round" />
        </svg>
      );
    case "dental":
      return (
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} className={common} aria-hidden>
          <path d="M12 5c-2-1-4-1-6 0-2 1-2 4-1 7 1 2 1 6 3 6 2 0 1-5 4-5s2 5 4 5c2 0 2-4 3-6 1-3 1-6-1-7-2-1-4-1-6 0z" strokeLinejoin="round" />
        </svg>
      );
    case "grooming":
      return (
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} className={common} aria-hidden>
          <circle cx="12" cy="8" r="4" />
          <path d="M5 21v-1a7 7 0 0 1 14 0v1" strokeLinecap="round" />
        </svg>
      );
    default:
      return null;
  }
}

function CheckIcon({ highlighted }: { highlighted?: boolean }) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={3}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={highlighted ? "h-3.5 w-3.5 text-white" : "h-3.5 w-3.5 text-[var(--rs-accent-strong)]"}
      aria-hidden
    >
      <path d="M20 6L9 17l-5-5" />
    </svg>
  );
}
