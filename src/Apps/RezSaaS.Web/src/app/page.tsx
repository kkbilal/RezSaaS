import Link from "next/link";
import { routes } from "@/shared/config/routes";
import { Button } from "@/shared/ui/button";

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
    <main className="studio-grid min-h-screen">
      <header className="mx-auto flex max-w-7xl items-center justify-between px-5 py-6 sm:px-8">
        <Link
          className="text-lg font-semibold tracking-[-0.04em] text-[var(--rs-ink)]"
          href={routes.public.home}
        >
          RezSaaS
        </Link>
        <nav className="hidden items-center gap-7 text-sm text-[var(--rs-muted)] md:flex">
          <Link className="transition hover:text-[var(--rs-ink)]" href={routes.public.discover}>
            Keşfet
          </Link>
          <a className="transition hover:text-[var(--rs-ink)]" href="#ozellikler">
            Özellikler
          </a>
          <a className="transition hover:text-[var(--rs-ink)]" href="#akis">
            İşleyiş
          </a>
          <a className="transition hover:text-[var(--rs-ink)]" href="#fiyatlar">
            Fiyatlar
          </a>
        </nav>
        <Button asChild>
          <Link href={routes.auth.login}>Giriş yap</Link>
        </Button>
      </header>

      <section className="mx-auto grid max-w-7xl gap-10 px-5 pb-20 pt-12 sm:px-8 lg:grid-cols-[1fr_28rem] lg:items-end lg:pt-24">
        <div className="fade-up max-w-4xl space-y-8">
          <p className="w-fit rounded-full border border-[var(--rs-border)] bg-white/72 px-4 py-2 text-sm text-[var(--rs-muted)] shadow-[var(--rs-shadow-soft)]">
            Salon, spa, klinik ve stüdyo ekipleri için
          </p>
          <div className="space-y-6">
            <h1 className="max-w-5xl text-6xl font-semibold tracking-[-0.075em] text-[var(--rs-ink)] sm:text-8xl">
              Rezervasyonu onaya, operasyonu netliğe bağla.
            </h1>
            <p className="max-w-2xl text-lg leading-8 text-[var(--rs-muted-strong)]">
              RezSaaS; müşteri keşfi, işletme onayı, personel ve kaynak kapasitesi,
              şube saati ve kötüye kullanım dayanımını aynı ürün akışında birleştiren
              modern rezervasyon platformudur.
            </p>
          </div>
          <div className="flex flex-wrap gap-3">
            <Button asChild>
              <Link href={routes.auth.login}>Giriş yap</Link>
            </Button>
            <Button asChild variant="secondary">
              <Link href={routes.public.discover}>İşletmeleri keşfet</Link>
            </Button>
          </div>
        </div>

        <aside className="fade-up rounded-[2.5rem] border border-[var(--rs-border)] bg-white/78 p-6 shadow-[var(--rs-shadow-card)] backdrop-blur-xl [animation-delay:100ms]">
          <p className="text-sm font-medium uppercase tracking-[0.24em] text-[var(--rs-muted)]">
            İşletme paneli
          </p>
          <div className="mt-8 space-y-4">
            {["Onay bekleyen talepler", "Maskeli müşteri bilgisi", "Şube bazlı yetki", "24 saat üst sınır"].map(
              (item) => (
                <div
                  className="rounded-[1.5rem] border border-[var(--rs-border)] bg-[var(--rs-surface)] p-4 text-sm font-medium text-[var(--rs-ink)]"
                  key={item}
                >
                  {item}
                </div>
              )
            )}
          </div>
          <p className="mt-6 text-sm leading-6 text-[var(--rs-muted)]">
            Tek giriş ekranından hesabına girersin; hangi paneli göreceğini rol ve
            işletme yetkilerin belirler.
          </p>
        </aside>
      </section>

      <section
        className="mx-auto grid max-w-7xl gap-4 px-5 py-10 sm:px-8 lg:grid-cols-3"
        id="ozellikler"
      >
        {productPillars.map((pillar, index) => (
          <article
            className="fade-up rounded-[2rem] border border-[var(--rs-border)] bg-white/72 p-6 shadow-[var(--rs-shadow-soft)] backdrop-blur"
            key={pillar.title}
            style={{ animationDelay: `${index * 70}ms` }}
          >
            <h2 className="text-2xl font-semibold tracking-[-0.05em] text-[var(--rs-ink)]">
              {pillar.title}
            </h2>
            <p className="mt-4 text-sm leading-7 text-[var(--rs-muted)]">
              {pillar.text}
            </p>
          </article>
        ))}
      </section>

      <section
        className="mx-auto grid max-w-7xl gap-8 px-5 py-16 sm:px-8 lg:grid-cols-[0.8fr_1fr]"
        id="akis"
      >
        <div className="space-y-4">
          <p className="text-sm font-medium uppercase tracking-[0.24em] text-[var(--rs-muted)]">
            İşleyiş
          </p>
          <h2 className="text-5xl font-semibold tracking-[-0.07em] text-[var(--rs-ink)]">
            Slotu hemen satmak yerine, doğru kararı hızlandırır.
          </h2>
        </div>
        <div className="grid gap-3">
          {flowSteps.map((step, index) => (
            <div
              className="rounded-[1.75rem] border border-[var(--rs-border)] bg-[var(--rs-surface)] p-5 shadow-[var(--rs-shadow-soft)]"
              key={step}
            >
              <p className="text-xs uppercase tracking-[0.2em] text-[var(--rs-muted)]">
                {String(index + 1).padStart(2, "0")}
              </p>
              <p className="mt-3 text-lg font-medium tracking-[-0.03em] text-[var(--rs-ink)]">
                {step}
              </p>
            </div>
          ))}
        </div>
      </section>

      <section className="mx-auto max-w-7xl px-5 py-16 sm:px-8" id="fiyatlar">
        <div className="mb-8 flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
          <div>
            <p className="text-sm font-medium uppercase tracking-[0.24em] text-[var(--rs-muted)]">
              Fiyatlar
            </p>
            <h2 className="mt-3 text-5xl font-semibold tracking-[-0.07em] text-[var(--rs-ink)]">
              Şube sayına ve operasyon yoğunluğuna göre paketler.
            </h2>
          </div>
          <p className="max-w-md text-sm leading-6 text-[var(--rs-muted)]">
            SMS ve bildirim maliyetleri gizlenmez; kota ve aşım yaklaşımı paket
            içinde şeffaf tutulur.
          </p>
        </div>

        <div className="grid gap-4 lg:grid-cols-3">
          {plans.map((plan) => (
            <article
              className={
                plan.highlighted
                  ? "rounded-[2.25rem] bg-[var(--rs-ink)] p-6 text-white shadow-[var(--rs-shadow-card)]"
                  : "rounded-[2.25rem] border border-[var(--rs-border)] bg-white/78 p-6 shadow-[var(--rs-shadow-soft)]"
              }
              key={plan.name}
            >
              <p
                className={
                  plan.highlighted
                    ? "text-sm text-white/58"
                    : "text-sm text-[var(--rs-muted)]"
                }
              >
                {plan.eyebrow}
              </p>
              <h3
                className={
                  plan.highlighted
                    ? "mt-3 text-3xl font-semibold tracking-[-0.06em] text-white"
                    : "mt-3 text-3xl font-semibold tracking-[-0.06em] text-[var(--rs-ink)]"
                }
              >
                {plan.name}
              </h3>
              <p className="mt-8 flex items-end gap-1">
                <span className="text-4xl font-semibold tracking-[-0.06em]">
                  {plan.price}
                </span>
                <span
                  className={plan.highlighted ? "pb-1 text-white/58" : "pb-1 text-[var(--rs-muted)]"}
                >
                  {plan.suffix}
                </span>
              </p>
              <ul className="mt-6 space-y-3 text-sm leading-6">
                {plan.features.map((feature) => (
                  <li
                    className={plan.highlighted ? "text-white/72" : "text-[var(--rs-muted)]"}
                    key={feature}
                  >
                    {feature}
                  </li>
                ))}
              </ul>
            </article>
          ))}
        </div>
      </section>
    </main>
  );
}
