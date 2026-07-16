import type { Metadata } from "next";
import Link from "next/link";

import { PublicHeader } from "@/components/public-header";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { routes } from "@/shared/config/routes";

// Isletme onboarding'i ELLE yapiliyor (docs/29 K3: "Self-servis isletme kaydi YAPILMAYACAK").
// Bu yuzden isletme CTA'si ancak GERCEK bir iletisim kanali TANIMLIYSA gosterilir.
// Env yoksa CTA hic render EDILMEZ -- var olmayan bir adrese link vermek de cikmaz sokaktir.
const contactEmail = process.env.NEXT_PUBLIC_REZSAAS_CONTACT_EMAIL?.trim();

// Musterinin akisi, urunun GERCEK davranisiyla birebir:
// talep -> isletme onayi -> kesin randevu. "Aninda rezervasyon" YOK; slot talebi bloklamaz.
const howItWorks = [
  {
    step: "1",
    title: "Ara",
    text: "Şehir, ilçe veya işletme adıyla sana yakın salon, spa, klinik ve stüdyoları bul."
  },
  {
    step: "2",
    title: "Uygun saati seç",
    text: "İşletmenin takvimindeki boş saatleri gör, sana uyanı seç ve talebini gönder."
  },
  {
    step: "3",
    title: "İşletme onaylasın",
    text: "Talebin işletmeye düşer. Yetkili onayladığında randevun kesinleşir ve haberin olur."
  }
];

export const metadata: Metadata = {
  alternates: {
    canonical: routes.public.home
  },
  description:
    "Sana yakın salon, spa, klinik ve stüdyoları keşfet; uygun saati seç, randevu talebini gönder. Randevun işletme onayıyla kesinleşir.",
  title: "Salon, spa ve klinik randevusu"
};

export default function HomePage() {
  return (
    <main className="min-h-screen bg-background">
      <PublicHeader />

      {/* Hero + arama: sayfanin TEK birincil isi musteriyi /kesfet'e sokmak. */}
      <section className="mx-auto max-w-6xl px-4 py-12 sm:px-6 sm:py-20">
        <div className="mx-auto max-w-2xl text-center">
          <h1 className="text-3xl font-semibold tracking-tight text-balance text-foreground sm:text-5xl">
            Randevunu al, işletme onaylasın.
          </h1>
          <p className="mx-auto mt-4 max-w-xl text-base leading-7 text-muted-foreground sm:text-lg">
            Sana yakın salon, spa, klinik ve stüdyoları keşfet. Uygun saati seç,
            talebini gönder; randevun işletmenin onayıyla kesinleşir.
          </p>
        </div>

        {/* Native GET form: JS olmadan da calisir ve /kesfet'in okudugu query'yi (searchText,
            city) dogrudan uretir -> SSR + indexlenebilirlik korunur. */}
        <form
          action={routes.public.discover}
          className="mx-auto mt-8 flex max-w-2xl flex-col gap-3 sm:flex-row"
          method="get"
        >
          <div className="flex-1 space-y-1.5">
            <label
              className="block text-sm font-medium text-foreground sm:sr-only"
              htmlFor="home-search-text"
            >
              İşletme veya hizmet
            </label>
            <Input
              autoComplete="off"
              className="min-h-11"
              id="home-search-text"
              name="searchText"
              placeholder="Saç kesimi, cilt bakımı, masaj..."
            />
          </div>
          <div className="flex-1 space-y-1.5 sm:max-w-[13rem]">
            <label
              className="block text-sm font-medium text-foreground sm:sr-only"
              htmlFor="home-search-city"
            >
              Şehir
            </label>
            <Input
              autoComplete="address-level2"
              className="min-h-11"
              id="home-search-city"
              name="city"
              placeholder="İstanbul"
            />
          </div>
          <Button className="min-h-11 sm:mt-[1.625rem] sm:w-auto" type="submit">
            İşletme ara
          </Button>
        </form>

        <p className="mt-4 text-center text-sm text-muted-foreground">
          Aklında bir yer yok mu?{" "}
          <Link
            className="font-medium text-foreground underline underline-offset-4"
            href={routes.public.discover}
          >
            Tüm işletmeleri gör
          </Link>
        </p>
      </section>

      {/* Nasil calisir */}
      <section className="border-t border-border bg-muted/40">
        <div className="mx-auto max-w-6xl px-4 py-12 sm:px-6 sm:py-16">
          <h2 className="text-center text-2xl font-semibold tracking-tight text-foreground sm:text-3xl">
            Nasıl çalışır?
          </h2>
          {/* <768px tek sutun (kural 4) */}
          <ol className="mt-8 grid gap-4 md:grid-cols-3">
            {howItWorks.map((item) => (
              <li
                className="rounded-xl border border-border bg-card p-6 text-card-foreground"
                key={item.step}
              >
                {/* Adim numarasi GORUNUR bir etiket -- renk/sira tek sinyal degil (kural 3). */}
                <span className="flex h-8 w-8 items-center justify-center rounded-full bg-primary text-sm font-semibold text-primary-foreground">
                  {item.step}
                </span>
                <h3 className="mt-4 text-lg font-semibold tracking-tight">
                  {item.title}
                </h3>
                <p className="mt-2 text-sm leading-6 text-muted-foreground">
                  {item.text}
                </p>
              </li>
            ))}
          </ol>
        </div>
      </section>

      {/* Isletme sahibi bolumu -- yalnizca gercek bir iletisim kanali tanimliysa. */}
      {contactEmail ? (
        <section className="border-t border-border">
          <div className="mx-auto max-w-6xl px-4 py-12 sm:px-6">
            <div className="rounded-xl border border-border bg-card p-6 text-card-foreground sm:p-8">
              <h2 className="text-xl font-semibold tracking-tight sm:text-2xl">
                İşletmenizi RezSaaS&apos;a eklemek ister misiniz?
              </h2>
              <p className="mt-2 max-w-2xl text-sm leading-6 text-muted-foreground">
                Kurulumu sizinle birlikte biz yapıyoruz: şubelerinizi, hizmetlerinizi
                ve personelinizi tanımlayıp hesabınızı açıyoruz. Bize yazın, dönelim.
              </p>
              <Button asChild className="mt-5 min-h-11" variant="outline">
                <a href={`mailto:${contactEmail}`}>Bize ulaşın</a>
              </Button>
            </div>
          </div>
        </section>
      ) : null}

      <footer className="border-t border-border">
        <div className="mx-auto flex max-w-6xl flex-col items-center justify-between gap-3 px-4 py-8 text-sm text-muted-foreground sm:flex-row sm:px-6">
          <p>© {new Date().getFullYear()} RezSaaS</p>
          <div className="flex items-center gap-4">
            <Link className="hover:text-foreground" href={routes.public.discover}>
              Keşfet
            </Link>
            <Link className="hover:text-foreground" href={routes.auth.login}>
              Giriş yap
            </Link>
          </div>
        </div>
      </footer>
    </main>
  );
}
