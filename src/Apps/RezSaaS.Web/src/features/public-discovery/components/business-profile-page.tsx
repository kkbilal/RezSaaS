  import Link from "next/link";
  import { PublicBookingPanel } from "@/features/public-booking/components/public-booking-panel";
  import { EnhancedGallery } from "./enhanced-gallery";
  import type { PublicBusinessProfile } from "@/features/public-discovery/api/public-businesses";
  import { routes } from "@/shared/config/routes";
  import { Button } from "@/shared/ui/button";
  import { Card, CardDescription, CardHeader, CardTitle } from "@/shared/ui/card";

type BusinessProfilePageProps = {
  profile: PublicBusinessProfile;
};

const dayLabels: Record<string, string> = {
  Friday: "Cuma",
  Monday: "Pazartesi",
  Saturday: "Cumartesi",
  Sunday: "Pazar",
  Thursday: "Perşembe",
  Tuesday: "Salı",
  Wednesday: "Çarşamba"
};

export function BusinessProfilePage({ profile }: BusinessProfilePageProps) {
  const metadata = profile.metadata;
  const branches = profile.branches ?? [];
  const services = profile.services ?? [];
  const gallery = (metadata?.galleryImages ?? [])
    .slice()
    .sort((left, right) => (left.sortOrder ?? 0) - (right.sortOrder ?? 0));

  return (
    <main className="studio-grid min-h-screen px-4 py-6 sm:px-8">
      <div className="mx-auto max-w-7xl space-y-8">
        <header className="flex items-center justify-between">
          <Link
            className="text-lg font-semibold tracking-[-0.04em] text-[var(--rs-ink)]"
            href={routes.public.home}
          >
            RezSaaS
          </Link>
          <div className="flex items-center gap-3">
            <Button asChild variant="ghost">
              <Link href={routes.public.discover}>Keşfet</Link>
            </Button>
            <Button asChild>
              <Link href={routes.auth.login}>Giriş yap</Link>
            </Button>
          </div>
        </header>

        <section className="fade-up overflow-hidden rounded-[2.5rem] border border-[var(--rs-border)] bg-white/76 shadow-[var(--rs-shadow-card)] backdrop-blur-xl">
          <div className="grid gap-8 p-6 lg:grid-cols-[1fr_25rem] lg:p-8">
            <div className="space-y-5">
              <p className="w-fit rounded-full bg-[var(--rs-accent-soft)] px-4 py-2 text-sm font-medium text-[var(--rs-accent-strong)]">
                {profile.categoryKey ?? "İşletme"}
              </p>
              <h1 className="text-6xl font-semibold tracking-[-0.08em] text-[var(--rs-ink)] sm:text-8xl">
                {profile.displayName ?? "İşletme"}
              </h1>
              <p className="max-w-3xl text-lg leading-8 text-[var(--rs-muted-strong)]">
                {profile.description ||
                  "Bu işletme RezSaaS üzerinden onaylı rezervasyon akışıyla çalışır."}
              </p>
            </div>

            <aside className="rounded-[2rem] bg-[var(--rs-ink)] p-6 text-white shadow-[var(--rs-shadow-card)]">
              <p className="text-xs uppercase tracking-[0.24em] text-white/50">
                Rezervasyon modeli
              </p>
              <h2 className="mt-8 text-3xl font-semibold tracking-[-0.06em]">
                Talep gönderilir, işletme onaylar.
              </h2>
              <p className="mt-5 text-sm leading-6 text-white/68">
                Onay bekleyen talepler kesin randevu değildir ve slotu tek başına
                bloklamaz. İşletme uygun talebi seçtiğinde randevu netleşir.
              </p>
            </aside>
          </div>
        </section>

        {gallery.length > 0 ? <EnhancedGallery images={gallery} /> : null}

        <PublicBookingPanel profile={profile} />

        <div className="grid gap-6 xl:grid-cols-[1fr_24rem]">
          <section className="space-y-6" id="hizmetler">
            <SectionHeading
              eyebrow="Hizmetler"
              title="Süre ve fiyatı net hizmet menüsü."
            />
            <div className="grid gap-4">
              {services.length === 0 ? (
                <EmptyCard text="Bu işletmenin public hizmet menüsü henüz yayınlanmadı." />
              ) : (
                services.map((service) => (
                  <ServiceCard key={service.id ?? service.name} service={service} />
                ))
              )}
            </div>
          </section>

          <aside className="space-y-6">
            <RulesCard metadata={metadata} />
            <BranchList branches={branches} />
          </aside>
        </div>
      </div>
    </main>
  );
}

function Gallery({
  images
}: {
  images: NonNullable<
    NonNullable<PublicBusinessProfile["metadata"]>["galleryImages"]
  >;
}) {
  return (
    <section className="grid gap-4 lg:grid-cols-3">
      {images.slice(0, 3).map((image, index) => {
        const imageUrl = getSafeImageUrl(image.imageUrl);

        return (
          <div
            className="fade-up overflow-hidden rounded-[2rem] border border-[var(--rs-border)] bg-[var(--rs-surface)] shadow-[var(--rs-shadow-soft)]"
            key={`${image.imageUrl}-${index}`}
            style={{ animationDelay: `${index * 70}ms` }}
          >
            {imageUrl ? (
              // eslint-disable-next-line @next/next/no-img-element
              <img
                alt={image.altText ?? "İşletme galerisi"}
                className="h-64 w-full object-cover"
                decoding="async"
                fetchPriority={index === 0 ? "high" : "auto"}
                height={256}
                loading={index === 0 ? "eager" : "lazy"}
                referrerPolicy="no-referrer"
                sizes="(min-width: 1024px) 33vw, 100vw"
                src={imageUrl}
                width={640}
              />
            ) : (
              <div className="grid h-64 place-items-center bg-[var(--rs-surface-muted)] px-6 text-center text-sm text-[var(--rs-muted)]">
                {image.altText ?? "Galeri görseli"}
              </div>
            )}
          </div>
        );
      })}
    </section>
  );
}

function SectionHeading({ eyebrow, title }: { eyebrow: string; title: string }) {
  return (
    <div>
      <p className="text-sm font-medium uppercase tracking-[0.24em] text-[var(--rs-muted)]">
        {eyebrow}
      </p>
      <h2 className="mt-3 text-4xl font-semibold tracking-[-0.06em] text-[var(--rs-ink)]">
        {title}
      </h2>
    </div>
  );
}

function ServiceCard({
  service
}: {
  service: NonNullable<PublicBusinessProfile["services"]>[number];
}) {
  const variants = service.variants ?? [];

  return (
    <Card className="p-5">
      <CardHeader>
        <CardTitle>{service.name ?? "Hizmet"}</CardTitle>
        <CardDescription>{service.categoryKey ?? "Kategori"}</CardDescription>
      </CardHeader>
      <div className="mt-5 grid gap-3">
        {variants.length === 0 ? (
          <p className="rounded-2xl border border-dashed border-[var(--rs-border)] p-4 text-sm text-[var(--rs-muted)]">
            Bu hizmet için public varyant bilgisi henüz yok.
          </p>
        ) : (
          variants.map((variant) => (
            <div
              className="rounded-2xl border border-[var(--rs-border)] bg-white/72 p-4"
              key={variant.id ?? variant.name}
            >
              <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <p className="font-medium text-[var(--rs-ink)]">
                    {variant.name ?? "Varyant"}
                  </p>
                  <p className="mt-1 text-sm text-[var(--rs-muted)]">
                    {variant.durationMinutes ?? 0} dk
                  </p>
                </div>
                <p className="text-lg font-semibold tracking-[-0.04em] text-[var(--rs-accent-strong)]">
                  {formatMoney(variant.priceAmount, variant.currencyCode)}
                </p>
              </div>
            </div>
          ))
        )}
      </div>
    </Card>
  );
}

function RulesCard({
  metadata
}: {
  metadata: PublicBusinessProfile["metadata"];
}) {
  const hasReviews = (metadata?.reviewCount ?? 0) > 0;

  return (
    <Card className="p-6">
      <CardHeader>
        <CardTitle>Profil bilgileri</CardTitle>
        <CardDescription>
          {metadata?.seoDescription || "İşletmenin public profil detayları."}
        </CardDescription>
      </CardHeader>

      <div className="mt-6 space-y-3 text-sm leading-6">
        <InfoLine
          label="Değerlendirme"
          value={
            hasReviews
              ? `${metadata?.ratingAverage?.toFixed(1) ?? "0.0"} / 5, ${metadata?.reviewCount} yorum`
              : "Yorumlar henüz oluşmadı"
          }
        />
        <InfoLine
          label="Personel gösterimi"
          value={getStaffPolicyCopy(metadata?.staffDisplayPolicy)}
        />
        <InfoLine
          label="Kurallar"
          value={metadata?.publicRules || "İşletme kuralları yakında netleşir."}
        />
      </div>
    </Card>
  );
}

function BranchList({
  branches
}: {
  branches: NonNullable<PublicBusinessProfile["branches"]>;
}) {
  return (
    <Card className="p-6">
      <CardHeader>
        <CardTitle>Şubeler</CardTitle>
        <CardDescription>
          Saatler işletmenin şube zaman dilimine göre gösterilir.
        </CardDescription>
      </CardHeader>
      <div className="mt-6 space-y-4">
        {branches.length === 0 ? (
          <EmptyCard text="Public şube bilgisi henüz yayınlanmadı." />
        ) : (
          branches.map((branch) => <BranchCard branch={branch} key={branch.slug} />)
        )}
      </div>
    </Card>
  );
}

function BranchCard({
  branch
}: {
  branch: NonNullable<PublicBusinessProfile["branches"]>[number];
}) {
  return (
    <div className="rounded-[1.5rem] border border-[var(--rs-border)] bg-white/72 p-4">
      <p className="font-medium text-[var(--rs-ink)]">
        {branch.displayName ?? "Şube"}
      </p>
      <p className="mt-1 text-sm text-[var(--rs-muted)]">
        {[branch.district, branch.city].filter(Boolean).join(", ") ||
          "Konum bilgisi yok"}
      </p>
      <p className="mt-1 text-sm text-[var(--rs-muted)]">
        {branch.addressLine ?? "Adres bilgisi yok"}
      </p>
      <div className="mt-4 space-y-2">
        {(branch.workingHours ?? []).slice(0, 7).map((hours) => (
          <div
            className="flex items-center justify-between rounded-2xl bg-[var(--rs-surface-muted)] px-3 py-2 text-xs text-[var(--rs-muted)]"
            key={hours.dayOfWeek}
          >
            <span>{getDayLabel(hours.dayOfWeek)}</span>
            <span>
              {hours.isClosed
                ? "Kapalı"
                : `${formatTime(hours.opensAt)} - ${formatTime(hours.closesAt)}`}
            </span>
          </div>
        ))}
      </div>
      {branch.staffMembers?.length ? (
        <div className="mt-4 flex flex-wrap gap-2">
          {branch.staffMembers.map((staff) => (
            <span
              className="rounded-full bg-[var(--rs-neutral-soft)] px-3 py-1 text-xs text-[var(--rs-muted)]"
              key={staff.id ?? staff.displayName}
            >
              {staff.displayName ?? "Personel"}
            </span>
          ))}
        </div>
      ) : null}
    </div>
  );
}

function InfoLine({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-[var(--rs-border)] bg-white/72 p-4">
      <p className="text-xs text-[var(--rs-muted)]">{label}</p>
      <p className="mt-1 font-medium text-[var(--rs-ink)]">{value}</p>
    </div>
  );
}

function EmptyCard({ text }: { text: string }) {
  return (
    <div className="rounded-2xl border border-dashed border-[var(--rs-border)] bg-white/55 p-4 text-sm text-[var(--rs-muted)]">
      {text}
    </div>
  );
}

function formatMoney(amount?: number, currencyCode?: string | null) {
  if (amount === undefined) {
    return "Fiyat bilgisi yok";
  }

  try {
    return new Intl.NumberFormat("tr-TR", {
      currency: currencyCode ?? "TRY",
      maximumFractionDigits: 0,
      style: "currency"
    }).format(amount);
  } catch {
    return `${amount} ${currencyCode ?? "TRY"}`;
  }
}

function formatTime(value?: string) {
  if (!value) {
    return "--:--";
  }

  return value.slice(0, 5);
}

function getDayLabel(day?: string | null) {
  if (!day) {
    return "Gün";
  }

  return dayLabels[day] ?? day;
}

function getSafeImageUrl(value?: string | null) {
  if (!value) {
    return null;
  }

  if (value.startsWith("/") || value.startsWith("https://")) {
    return value;
  }

  return null;
}

function getStaffPolicyCopy(policy?: string | null) {
  if (policy === "DisplayNames") {
    return "Personel isimleri gösterilir";
  }

  if (policy === "Anonymous") {
    return "Personel bilgisi işletme onayında netleşir";
  }

  return policy ?? "İşletme politikasına göre";
}
