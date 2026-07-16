import Link from "next/link";

import { PublicHeader } from "@/components/public-header";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { routes } from "@/shared/config/routes";
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

type FacetField = "categoryKey" | "city" | "district";

type FacetOption = {
  count: number;
  href: string;
  isActive: boolean;
  label: string;
  value: string;
};

// categoryKey backend'de KAPALI bir enum DEGIL -- serbest metin (e2e-smoke.py "hair" yaziyor,
// domain tarafinda sabit liste yok). Bu yuzden burasi bir SECENEK LISTESI degil, yalnizca
// bilinen anahtarlar icin bir GORUNEN AD sozlugu. Bilinmeyen anahtar ham haliyle gosterilir.
// (Ana sayfada bu listeden uretilen bir kategori izgarasi VARDI: anahtarlari -- hair, fitness,
// dental -- buradakilerle bile ortusmuyordu, yani 0 sonuca goturebiliyordu. Kaldirildi.)
const categoryLabels: Record<string, string> = {
  barber: "Berber",
  beauty: "Güzellik",
  clinic: "Klinik",
  hair: "Saç & Güzellik",
  nail: "Nail Studio",
  spa: "Spa",
  studio: "Stüdyo"
};

export function DiscoverPage({ params, state }: DiscoverPageProps) {
  const activeFilterCount = getActiveDiscoveryFilterCount(params);

  return (
    <main className="min-h-screen bg-background">
      <PublicHeader />

      <div className="mx-auto max-w-6xl px-4 py-8 sm:px-6 sm:py-10">
        <header className="max-w-2xl">
          <h1 className="text-2xl font-semibold tracking-tight text-foreground sm:text-4xl">
            İşletme keşfet
          </h1>
          <p className="mt-3 text-sm leading-6 text-muted-foreground sm:text-base">
            Şehir, ilçe veya işletme adıyla ara. Uygun saati seçip talebini
            gönderdiğinde randevun işletmenin onayıyla kesinleşir.
          </p>
        </header>

        <SearchForm params={params} />

        {/* Filtre seridi mobilde YAPISKAN: sonuclari kaydirirken filtre degistirmek
            telefonda birincil hareket (kural 4). */}
        <div className="sticky top-0 z-10 -mx-4 mt-6 border-b border-border bg-background/95 px-4 py-3 backdrop-blur-none sm:-mx-6 sm:px-6">
          <FilterStrip
            activeFilterCount={activeFilterCount}
            businesses={state.businesses}
            params={params}
          />
        </div>

        <div className="mt-6">
          {state.kind === "unavailable" ? (
            <SearchUnavailable reason={state.reason} />
          ) : (
            <SearchResults businesses={state.businesses} params={params} />
          )}
        </div>
      </div>
    </main>
  );
}

function SearchForm({ params }: { params: PublicBusinessSearchParams }) {
  return (
    // Native GET form: JS'siz calisir, filtreleri URL'e yazar (paylasilabilir link) ve
    // sayfayi SSR/indexlenebilir birakir.
    <form
      action={routes.public.discover}
      className="mt-6 grid gap-3 sm:grid-cols-[2fr_1fr_1fr_auto]"
      method="get"
    >
      {/* categoryKey bir GIZLI alan: kullanicidan "spa, barber, clinic" gibi bir anahtari
          YAZMASINI istemek yerine asagidaki veriye dayali cip'lerden secilir. Boylece
          formu gonderince aktif kategori kaybolmaz. */}
      {params.categoryKey ? (
        <input name="categoryKey" type="hidden" value={params.categoryKey} />
      ) : null}

      <SearchField
        defaultValue={params.searchText}
        label="İşletme veya hizmet"
        name="searchText"
        placeholder="Saç kesimi, cilt bakımı..."
      />
      <SearchField
        defaultValue={params.city}
        label="Şehir"
        name="city"
        placeholder="İstanbul"
      />
      <SearchField
        defaultValue={params.district}
        label="İlçe"
        name="district"
        placeholder="Kadıköy"
      />
      <Button className="min-h-11 sm:mt-[1.625rem]" type="submit">
        Ara
      </Button>
    </form>
  );
}

function SearchField({
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
  const fieldId = `discover-${name}`;

  return (
    <div className="space-y-1.5">
      <label
        className="block text-sm font-medium text-foreground"
        htmlFor={fieldId}
      >
        {label}
      </label>
      <Input
        autoComplete="off"
        className="min-h-11"
        defaultValue={defaultValue}
        id={fieldId}
        name={name}
        placeholder={placeholder}
      />
    </div>
  );
}

function FilterStrip({
  activeFilterCount,
  businesses,
  params
}: {
  activeFilterCount: number;
  businesses: PublicBusinessSummary[];
  params: PublicBusinessSearchParams;
}) {
  const activeFilters = getActiveFilters(params);
  // Cip'ler MEVCUT sonuc kumesinden turer -- uydurma kategori/sehir listesi yok,
  // her cip en az 1 sonuc getirmeyi garanti eder.
  const facets = [
    ...getFacetOptions(businesses, params, "categoryKey"),
    ...getFacetOptions(businesses, params, "city"),
    ...getFacetOptions(businesses, params, "district")
  ].filter((facet) => !facet.isActive);

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between gap-3">
        <p className="text-sm font-medium text-foreground">
          {businesses.length} işletme
          {activeFilterCount > 0 ? (
            <span className="font-normal text-muted-foreground">
              {" "}
              · {activeFilterCount} filtre
            </span>
          ) : null}
        </p>
        {activeFilterCount > 0 ? (
          <Button asChild className="min-h-11" size="sm" variant="ghost">
            <Link href={routes.public.discover}>Filtreleri temizle</Link>
          </Button>
        ) : null}
      </div>

      {activeFilters.length > 0 || facets.length > 0 ? (
        // Mobilde yatay kaydirma; sayfa govdesi yatay kaymaz.
        <div className="-mx-4 overflow-x-auto px-4 sm:mx-0 sm:px-0">
          <div className="flex w-max gap-2 sm:w-auto sm:flex-wrap">
            {activeFilters.map((filter) => (
              <Link
                className="inline-flex min-h-11 items-center gap-2 rounded-full border border-transparent bg-primary px-4 text-sm font-medium whitespace-nowrap text-primary-foreground focus-visible:ring-[3px] focus-visible:ring-ring/50 focus-visible:outline-none"
                href={filter.href}
                key={`${filter.label}-${filter.value}`}
              >
                {/* Etiket GORUNUR metin: hangi alan oldugu tooltip'e saklanmaz (kural 3). */}
                <span>
                  {filter.label}: {filter.value}
                </span>
                <span aria-hidden className="text-base leading-none">
                  ×
                </span>
                <span className="sr-only">filtresini kaldır</span>
              </Link>
            ))}

            {facets.slice(0, 12).map((facet) => (
              <Link
                className="inline-flex min-h-11 items-center gap-2 rounded-full border border-border bg-background px-4 text-sm font-medium whitespace-nowrap text-foreground hover:bg-accent focus-visible:ring-[3px] focus-visible:ring-ring/50 focus-visible:outline-none"
                href={facet.href}
                key={`${facet.label}-${facet.value}`}
              >
                {facet.label}
                <span className="text-muted-foreground">{facet.count}</span>
              </Link>
            ))}
          </div>
        </div>
      ) : null}
    </div>
  );
}

function SearchUnavailable({ reason }: { reason: string }) {
  return (
    <div className="rounded-xl border border-destructive/40 bg-destructive/5 p-6">
      <h2 className="text-base font-semibold text-foreground">
        Arama tamamlanamadı
      </h2>
      <p className="mt-2 text-sm leading-6 text-muted-foreground">{reason}</p>
      <Button asChild className="mt-4 min-h-11" variant="outline">
        <Link href={routes.public.discover}>Tekrar dene</Link>
      </Button>
    </div>
  );
}

function SearchResults({
  businesses,
  params
}: {
  businesses: PublicBusinessSummary[];
  params: PublicBusinessSearchParams;
}) {
  if (businesses.length === 0) {
    const hasFilters = getActiveDiscoveryFilterCount(params) > 0;

    return (
      <div className="rounded-xl border border-dashed border-border p-8 text-center sm:p-12">
        <h2 className="text-base font-semibold text-foreground">
          Bu aramada işletme bulunamadı
        </h2>
        <p className="mx-auto mt-2 max-w-md text-sm leading-6 text-muted-foreground">
          {hasFilters
            ? "Filtreleri gevşetmeyi dene: ilçeyi kaldır, şehri genişlet veya arama terimini kısalt."
            : "Şu anda listelenecek public işletme profili yok. Kısa süre sonra tekrar bak."}
        </p>
        {hasFilters ? (
          <Button asChild className="mt-5 min-h-11" variant="outline">
            <Link href={routes.public.discover}>Filtreleri temizle</Link>
          </Button>
        ) : null}
      </div>
    );
  }

  return (
    // <768px tek sutun (kural 4)
    <section className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
      {businesses.map((business, index) => (
        <BusinessResultCard
          business={business}
          key={business.slug ?? `${business.displayName}-${index}`}
        />
      ))}
    </section>
  );
}

function BusinessResultCard({ business }: { business: PublicBusinessSummary }) {
  const slug = business.slug ?? "";
  const location =
    [business.district, business.city].filter(Boolean).join(", ") || null;

  // PUAN OZETI YOK -- bilerek.
  // PublicBusinessSummaryView yalnizca tenantId/slug/displayName/categoryKey/city/district
  // doner; ratingAverage/reviewCount ALANLARI SOZLESMEDE YOK. Puan gostermek isletme basina
  // ayri bir /reviews cagrisi (N+1) gerektirirdi. "Backend ucu yoksa ekran yok" (kural 1).
  // Puan ozeti tek cagrida gerekiyorsa: PublicBusinessSummaryView'a alan eklenmeli.

  const card = (
    <>
      <Badge variant="secondary">{getCategoryLabel(business.categoryKey)}</Badge>
      <h2 className="mt-3 text-lg font-semibold tracking-tight text-foreground">
        {business.displayName ?? "İşletme"}
      </h2>
      <p className="mt-1 text-sm text-muted-foreground">
        {location ?? "Konum belirtilmemiş"}
      </p>
    </>
  );

  if (!slug) {
    // Slug yoksa profile gidilemez; tiklanabilir gibi gorunmesin.
    return (
      <article className="rounded-xl border border-border bg-card p-5 text-card-foreground">
        {card}
        <p className="mt-4 text-sm text-muted-foreground">
          Bu işletmenin profili henüz yayında değil.
        </p>
      </article>
    );
  }

  return (
    // Kartin TAMAMI tek dokunma hedefi -- telefonda "Profili gör" dugmesini
    // nisan almaktan daha guvenilir (kural 4).
    <Link
      className="block rounded-xl border border-border bg-card p-5 text-card-foreground transition-colors hover:bg-accent/40 focus-visible:ring-[3px] focus-visible:ring-ring/50 focus-visible:outline-none"
      href={routes.public.businessProfile(slug)}
    >
      {card}
      <span className="mt-4 inline-block text-sm font-medium text-foreground underline underline-offset-4">
        Profili gör
      </span>
    </Link>
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
  field: FacetField
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
  field: FacetField,
  value: string
): Partial<PublicBusinessSearchParams> {
  if (field === "categoryKey") {
    return { categoryKey: value };
  }

  // Sehir degisince ilce dusurulur: baska sehrin ilcesi 0 sonuc verirdi.
  if (field === "city") {
    return { city: value, district: undefined };
  }

  return { district: value };
}

function getFacetClearPatch(field: FacetField): Partial<PublicBusinessSearchParams> {
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

  return categoryLabels[categoryKey] ?? categoryKey;
}
