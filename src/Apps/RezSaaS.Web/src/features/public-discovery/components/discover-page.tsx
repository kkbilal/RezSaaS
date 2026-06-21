import Link from "next/link";
import { routes } from "@/shared/config/routes";
import { Button } from "@/shared/ui/button";
import { Card, CardDescription, CardTitle } from "@/shared/ui/card";
import {
  buildDiscoverHref,
  getActiveDiscoveryFilterCount
} from "../lib/discovery-search";
import type {
  PublicBusinessSearchParams,
  PublicBusinessSearchState,
  PublicBusinessSummary
} from "../api/public-businesses";

type DiscoverPageProps = {
  params: PublicBusinessSearchParams;
  state: PublicBusinessSearchState;
};

type FacetOption = {
  count: number;
  href: string;
  isActive: boolean;
  label: string;
  value: string;
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

        {state.kind === "ready" ? (
          <SearchRefinement businesses={state.businesses} params={params} />
        ) : null}

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

function SearchRefinement({
  businesses,
  params
}: {
  businesses: PublicBusinessSummary[];
  params: PublicBusinessSearchParams;
}) {
  const activeFilterCount = getActiveDiscoveryFilterCount(params);
  const activeFilters = getActiveFilters(params);
  const categoryFacets = getFacetOptions(businesses, params, "categoryKey");
  const cityFacets = getFacetOptions(businesses, params, "city");
  const districtFacets = getFacetOptions(businesses, params, "district");

  return (
    <section className="fade-up rounded-[2rem] border border-[var(--rs-border)] bg-white/70 p-5 shadow-[var(--rs-shadow-soft)] [animation-delay:90ms]">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div>
          <p className="text-xs uppercase tracking-[0.22em] text-[var(--rs-muted)]">
            Arama özeti
          </p>
          <h2 className="mt-2 text-3xl font-semibold tracking-[-0.06em] text-[var(--rs-ink)]">
            {businesses.length} public işletme profili
          </h2>
          <p className="mt-2 max-w-2xl text-sm leading-6 text-[var(--rs-muted)]">
            Filtreler URL üzerinde taşınır; kartlar tek discovery endpointinden
            gelir ve profil başına ek çağrı üretmez.
          </p>
        </div>

        {activeFilterCount > 0 ? (
          <Button asChild variant="secondary">
            <Link href={routes.public.discover}>Filtreleri temizle</Link>
          </Button>
        ) : null}
      </div>

      {activeFilters.length > 0 ? (
        <div className="mt-5 flex flex-wrap gap-2">
          {activeFilters.map((filter) => (
            <Link
              className="rounded-full border border-[var(--rs-border)] bg-white px-4 py-2 text-sm text-[var(--rs-muted-strong)] shadow-[var(--rs-shadow-soft)] transition hover:border-[var(--rs-border-strong)] hover:text-[var(--rs-ink)]"
              href={filter.href}
              key={filter.label}
            >
              <span className="font-medium text-[var(--rs-ink)]">
                {filter.label}:
              </span>{" "}
              {filter.value}
              <span className="ml-2 text-xs text-[var(--rs-muted)]">sil</span>
            </Link>
          ))}
        </div>
      ) : null}

      <div className="mt-5 grid gap-4 lg:grid-cols-3">
        <FacetGroup options={categoryFacets} title="Kategori" />
        <FacetGroup options={cityFacets} title="Şehir" />
        <FacetGroup options={districtFacets} title="İlçe" />
      </div>
    </section>
  );
}

function FacetGroup({
  options,
  title
}: {
  options: FacetOption[];
  title: string;
}) {
  if (options.length === 0) {
    return (
      <div className="rounded-[1.5rem] border border-dashed border-[var(--rs-border)] bg-white/50 p-4 text-sm text-[var(--rs-muted)]">
        {title} filtresi için bu aramada veri yok.
      </div>
    );
  }

  return (
    <div className="rounded-[1.5rem] border border-[var(--rs-border)] bg-[var(--rs-surface)] p-4">
      <p className="text-xs uppercase tracking-[0.2em] text-[var(--rs-muted)]">
        {title}
      </p>
      <div className="mt-3 flex flex-wrap gap-2">
        {options.slice(0, 8).map((option) => (
          <Link
            className={
              option.isActive
                ? "rounded-full bg-[var(--rs-ink)] px-3 py-2 text-xs font-medium text-white shadow-[var(--rs-shadow-soft)]"
                : "rounded-full border border-[var(--rs-border)] bg-white px-3 py-2 text-xs font-medium text-[var(--rs-muted-strong)] transition hover:border-[var(--rs-border-strong)] hover:text-[var(--rs-ink)]"
            }
            href={option.href}
            key={option.value}
          >
            {option.label}
            <span
              className={
                option.isActive ? "ml-2 text-white/60" : "ml-2 text-[var(--rs-muted)]"
              }
            >
              {option.count}
            </span>
          </Link>
        ))}
      </div>
    </div>
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
        <BusinessResultCard
          business={business}
          index={index}
          key={business.slug ?? `${business.displayName}-${index}`}
        />
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
      {slug ? (
        <Button asChild className="mt-8 w-full" variant="secondary">
          <Link href={routes.public.businessProfile(slug)}>Profili gör</Link>
        </Button>
      ) : (
        <Button className="mt-8 w-full" disabled type="button" variant="secondary">
          Profil hazırlanıyor
        </Button>
      )}
    </article>
  );
}

function getActiveFilters(params: PublicBusinessSearchParams) {
  return [
    {
      href: buildDiscoverHref(params, { searchText: undefined }),
      label: "Arama",
      value: params.searchText
    },
    {
      href: buildDiscoverHref(params, { city: undefined, district: undefined }),
      label: "Şehir",
      value: params.city
    },
    {
      href: buildDiscoverHref(params, { district: undefined }),
      label: "İlçe",
      value: params.district
    },
    {
      href: buildDiscoverHref(params, { categoryKey: undefined }),
      label: "Kategori",
      value: params.categoryKey ? getCategoryLabel(params.categoryKey) : undefined
    }
  ].filter(
    (filter): filter is { href: string; label: string; value: string } =>
      Boolean(filter.value)
  );
}

function getFacetOptions(
  businesses: PublicBusinessSummary[],
  params: PublicBusinessSearchParams,
  field: "categoryKey" | "city" | "district"
): FacetOption[] {
  const counts = new Map<string, number>();

  for (const business of businesses) {
    const value = business[field]?.trim();

    if (value) {
      counts.set(value, (counts.get(value) ?? 0) + 1);
    }
  }

  return Array.from(counts.entries())
    .map(([value, count]) => {
      const currentValue = params[field];
      const isActive =
        currentValue?.toLocaleLowerCase("tr-TR") ===
        value.toLocaleLowerCase("tr-TR");

      return {
        count,
        href: buildDiscoverHref(
          params,
          isActive ? getFacetClearPatch(field) : getFacetApplyPatch(field, value)
        ),
        isActive,
        label: field === "categoryKey" ? getCategoryLabel(value) : value,
        value
      };
    })
    .sort(
      (left, right) =>
        right.count - left.count || left.label.localeCompare(right.label, "tr-TR")
    );
}

function getFacetApplyPatch(
  field: "categoryKey" | "city" | "district",
  value: string
): Partial<PublicBusinessSearchParams> {
  if (field === "categoryKey") {
    return { categoryKey: value };
  }

  if (field === "city") {
    return { city: value, district: undefined };
  }

  return { district: value };
}

function getFacetClearPatch(
  field: "categoryKey" | "city" | "district"
): Partial<PublicBusinessSearchParams> {
  if (field === "city") {
    return { city: undefined, district: undefined };
  }

  if (field === "categoryKey") {
    return { categoryKey: undefined };
  }

  return { district: undefined };
}

function getCategoryLabel(categoryKey?: string | null) {
  if (!categoryKey) {
    return "Kategori";
  }

  return categories[categoryKey] ?? categoryKey;
}
