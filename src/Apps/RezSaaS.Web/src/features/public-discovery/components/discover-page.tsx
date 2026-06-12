import Link from "next/link";
import { routes } from "@/shared/config/routes";
import { Button } from "@/shared/ui/button";
import { Card, CardDescription, CardTitle } from "@/shared/ui/card";
import type {
  PublicBusinessSearchParams,
  PublicBusinessSearchState,
  PublicBusinessSummary
} from "../api/public-businesses";

type DiscoverPageProps = {
  params: PublicBusinessSearchParams;
  state: PublicBusinessSearchState;
};

const categories: Record<string, string> = {
  barber: "Berber",
  beauty: "Güzellik",
  clinic: "Klinik",
  nail: "Nail Studio",
  spa: "Spa",
  studio: "Stüdyo"
};

export function DiscoverPage({ params, state }: DiscoverPageProps) {
  return (
    <main className="studio-grid min-h-screen px-4 py-6 sm:px-8">
      <div className="mx-auto max-w-7xl space-y-8">
        <PublicTopBar />

        <section className="fade-up grid gap-8 rounded-[2.5rem] border border-[var(--rs-border)] bg-white/74 p-6 shadow-[var(--rs-shadow-card)] backdrop-blur-xl lg:grid-cols-[1fr_28rem] lg:p-8">
          <div className="space-y-5">
            <p className="w-fit rounded-full bg-[var(--rs-accent-soft)] px-4 py-2 text-sm font-medium text-[var(--rs-accent-strong)]">
              Keşfet
            </p>
            <h1 className="max-w-4xl text-5xl font-semibold tracking-[-0.07em] text-[var(--rs-ink)] sm:text-7xl">
              İşletme profillerini güvenli rezervasyon akışıyla keşfet.
            </h1>
            <p className="max-w-2xl text-lg leading-8 text-[var(--rs-muted-strong)]">
              RezSaaS keşfi yalnızca public işletme verisiyle çalışır. Şehir,
              kategori veya işletme adıyla ara; rezervasyon süreci işletme onayıyla
              ilerler.
            </p>
          </div>

          <SearchCard params={params} />
        </section>

        {state.kind === "unavailable" ? (
          <Card className="border-[var(--rs-warning-border)] bg-[var(--rs-warning-soft)] p-6 shadow-none">
            <CardTitle>Arama tamamlanamadı</CardTitle>
            <CardDescription className="mt-2 text-[var(--rs-warning)]">
              {state.reason}
            </CardDescription>
          </Card>
        ) : (
          <SearchResults businesses={state.businesses} />
        )}
      </div>
    </main>
  );
}

function PublicTopBar() {
  return (
    <header className="flex items-center justify-between">
      <Link
        className="text-lg font-semibold tracking-[-0.04em] text-[var(--rs-ink)]"
        href={routes.public.home}
      >
        RezSaaS
      </Link>
      <div className="flex items-center gap-3">
        <Button asChild variant="ghost">
          <Link href={routes.public.home}>Ana sayfa</Link>
        </Button>
        <Button asChild>
          <Link href={routes.auth.login}>Giriş yap</Link>
        </Button>
      </div>
    </header>
  );
}

function SearchCard({ params }: { params: PublicBusinessSearchParams }) {
  return (
    <form
      action={routes.public.discover}
      className="rounded-[2rem] border border-[var(--rs-border)] bg-[var(--rs-surface)] p-5 shadow-[var(--rs-shadow-soft)]"
    >
      <div className="space-y-4">
        <SearchInput
          defaultValue={params.searchText}
          label="İşletme veya hizmet"
          name="searchText"
          placeholder="Saç, cilt bakımı, klinik..."
        />
        <div className="grid gap-4 sm:grid-cols-2">
          <SearchInput
            defaultValue={params.city}
            label="Şehir"
            name="city"
            placeholder="İstanbul"
          />
          <SearchInput
            defaultValue={params.district}
            label="İlçe"
            name="district"
            placeholder="Kadıköy"
          />
        </div>
        <SearchInput
          defaultValue={params.categoryKey}
          label="Kategori anahtarı"
          name="categoryKey"
          placeholder="spa, barber, clinic"
        />
      </div>
      <Button className="mt-5 w-full" type="submit">
        İşletme ara
      </Button>
    </form>
  );
}

function SearchInput({
  defaultValue,
  label,
  name,
  placeholder
}: {
  defaultValue?: string;
  label: string;
  name: string;
  placeholder: string;
}) {
  return (
    <label className="block space-y-2">
      <span className="text-sm font-medium text-[var(--rs-ink)]">{label}</span>
      <input
        className="min-h-12 w-full rounded-2xl border border-[var(--rs-border)] bg-white px-4 text-sm text-[var(--rs-ink)] shadow-[var(--rs-shadow-soft)] outline-none transition placeholder:text-[var(--rs-muted)] focus:border-[var(--rs-border-strong)] focus:ring-4 focus:ring-[rgb(5_26_36_/_0.08)]"
        defaultValue={defaultValue}
        name={name}
        placeholder={placeholder}
      />
    </label>
  );
}

function SearchResults({ businesses }: { businesses: PublicBusinessSummary[] }) {
  if (businesses.length === 0) {
    return (
      <Card className="border-dashed bg-white/60 p-10 text-center shadow-none">
        <CardTitle>Bu aramada işletme bulunamadı</CardTitle>
        <CardDescription className="mx-auto mt-2 max-w-lg">
          Arama terimini genişletebilir, şehir veya kategori alanlarını boşaltarak
          tekrar deneyebilirsin.
        </CardDescription>
      </Card>
    );
  }

  return (
    <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
      {businesses.map((business, index) => (
        <BusinessResultCard business={business} index={index} key={business.slug} />
      ))}
    </section>
  );
}

function BusinessResultCard({
  business,
  index
}: {
  business: PublicBusinessSummary;
  index: number;
}) {
  const slug = business.slug ?? "";

  return (
    <article
      className="fade-up rounded-[2rem] border border-[var(--rs-border)] bg-white/78 p-5 shadow-[var(--rs-shadow-soft)] transition duration-300 hover:-translate-y-0.5 hover:shadow-[var(--rs-shadow-card)]"
      style={{ animationDelay: `${index * 55}ms` }}
    >
      <p className="w-fit rounded-full bg-[var(--rs-neutral-soft)] px-3 py-1 text-xs font-medium text-[var(--rs-muted)]">
        {getCategoryLabel(business.categoryKey)}
      </p>
      <h2 className="mt-6 text-3xl font-semibold tracking-[-0.06em] text-[var(--rs-ink)]">
        {business.displayName ?? "İşletme"}
      </h2>
      <p className="mt-3 text-sm text-[var(--rs-muted)]">
        {[business.district, business.city].filter(Boolean).join(", ") ||
          "Konum bilgisi hazırlanıyor"}
      </p>
      <Button asChild className="mt-8 w-full" variant="secondary">
        <Link href={routes.public.businessProfile(slug)}>Profili gör</Link>
      </Button>
    </article>
  );
}

function getCategoryLabel(categoryKey?: string | null) {
  if (!categoryKey) {
    return "Kategori";
  }

  return categories[categoryKey] ?? categoryKey;
}
