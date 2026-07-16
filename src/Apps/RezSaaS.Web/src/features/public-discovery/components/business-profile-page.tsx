import Link from "next/link";
import { Clock, MapPin, Star } from "lucide-react";
import { PublicBookingPanel } from "@/features/public-booking/components/public-booking-panel";
import { BusinessReviews } from "./business-reviews";
import type {
  PublicBusinessProfile,
  PublicReviewSummary
} from "@/features/public-discovery/api/public-businesses";
import { showStaffNames } from "@/features/public-discovery/lib/staff-display";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle
} from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { routes } from "@/shared/config/routes";

type BusinessProfilePageProps = {
  profile: PublicBusinessProfile;
  reviewSummary: PublicReviewSummary | null;
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

// Haftanin gunleri sozlesmede sirali GELMEZ (Sunday=0 kaynakli). Musteri "Pazartesi'den"
// baslayan bir liste bekler.
const dayOrder = [
  "Monday",
  "Tuesday",
  "Wednesday",
  "Thursday",
  "Friday",
  "Saturday",
  "Sunday"
];

export function BusinessProfilePage({
  profile,
  reviewSummary
}: BusinessProfilePageProps) {
  const metadata = profile.metadata;
  const branches = profile.branches ?? [];
  const services = profile.services ?? [];
  const staffNamesVisible = showStaffNames(metadata?.staffDisplayPolicy);

  // GALERI GOSTERILMIYOR: BusinessGalleryImage entity'si ve profil yanitindaki gallery alani
  // VAR, ama o veriyi dolduran hicbir yonetim ucu YOK -- galeri pratikte HER ZAMAN BOS.
  // Backend ucu olmayan ekran uretmiyoruz (enhanced-gallery.tsx bu yuzden silindi).

  return (
    <div className="min-h-screen bg-background pb-24 lg:pb-0">
      <header className="border-b bg-background">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-3 sm:px-6">
          <Link
            className="text-base font-semibold tracking-tight text-foreground"
            href={routes.public.home}
          >
            RezSaaS
          </Link>
          <div className="flex items-center gap-1">
            <Button asChild className="min-h-11" variant="ghost">
              <Link href={routes.public.discover}>Keşfet</Link>
            </Button>
            <Button asChild className="min-h-11" variant="outline">
              <Link href={routes.auth.login}>Giriş yap</Link>
            </Button>
          </div>
        </div>
      </header>

      <main className="mx-auto max-w-6xl space-y-8 px-4 py-6 sm:px-6 sm:py-8">
        <ProfileHero
          branches={branches}
          metadata={metadata}
          profile={profile}
          reviewSummary={reviewSummary}
        />

        <PublicBookingPanel profile={profile} />

        <div className="grid gap-8 lg:grid-cols-[1fr_20rem] lg:items-start">
          <div className="space-y-8">
            <ServiceMenu services={services} />
            {reviewSummary ? <BusinessReviews summary={reviewSummary} /> : null}
          </div>

          <aside className="space-y-6">
            {metadata?.publicRules ? (
              <Card>
                <CardHeader>
                  <CardTitle className="text-base">İşletme kuralları</CardTitle>
                </CardHeader>
                <CardContent>
                  <p className="text-sm leading-6 whitespace-pre-line text-muted-foreground">
                    {metadata.publicRules}
                  </p>
                </CardContent>
              </Card>
            ) : null}

            <BranchList
              branches={branches}
              staffNamesVisible={staffNamesVisible}
            />
          </aside>
        </div>
      </main>
    </div>
  );
}

function ProfileHero({
  branches,
  metadata,
  profile,
  reviewSummary
}: {
  branches: NonNullable<PublicBusinessProfile["branches"]>;
  metadata: PublicBusinessProfile["metadata"];
  profile: PublicBusinessProfile;
  reviewSummary: PublicReviewSummary | null;
}) {
  // Puan ozeti: /reviews ucu varsa ORADAN oku. Profil metadata'si ayni sayilari tasir ama
  // iki kaynagi karistirmak "ustte 4.8, altta 4.6" gibi celiskiler uretir.
  const ratingAverage = reviewSummary?.averageRating ?? metadata?.ratingAverage;
  const reviewCount = reviewSummary?.totalCount ?? metadata?.reviewCount ?? 0;
  const hasRating = reviewCount > 0 && typeof ratingAverage === "number";
  const primaryBranch = branches[0];
  const location = primaryBranch
    ? [primaryBranch.district, primaryBranch.city].filter(Boolean).join(", ")
    : "";

  return (
    <section className="space-y-4">
      <div className="space-y-3">
        {profile.categoryKey ? (
          <Badge variant="secondary">{profile.categoryKey}</Badge>
        ) : null}
        <h1 className="text-3xl font-semibold tracking-tight text-foreground sm:text-4xl">
          {profile.displayName ?? "İşletme"}
        </h1>

        <div className="flex flex-wrap items-center gap-x-4 gap-y-2 text-sm">
          {hasRating ? (
            <span className="flex items-center gap-1.5 text-foreground">
              <Star aria-hidden="true" className="size-4 fill-amber-500 text-amber-500" />
              <span className="font-medium">{ratingAverage.toFixed(1)}</span>
              <span className="text-muted-foreground">({reviewCount} yorum)</span>
            </span>
          ) : null}
          {location ? (
            <span className="flex items-center gap-1.5 text-muted-foreground">
              <MapPin aria-hidden="true" className="size-4" />
              {location}
            </span>
          ) : null}
          {branches.length > 1 ? (
            <span className="text-muted-foreground">{branches.length} şube</span>
          ) : null}
        </div>

        {profile.description ? (
          <p className="max-w-3xl text-sm leading-6 text-muted-foreground">
            {profile.description}
          </p>
        ) : null}
      </div>

      {/* Urunun EN COK YANLIS ANLASILAN mekanigi. Rezervasyon panelinin USTUNDE, tooltip
          DEGIL gorunur metin olarak duruyor -- musteri "randevum kesinlesti" sanmasin. */}
      <Card className="border-amber-500/30 bg-amber-50/60 dark:bg-amber-950/20">
        <CardContent className="flex gap-3">
          <Clock
            aria-hidden="true"
            className="mt-0.5 size-5 shrink-0 text-amber-600 dark:text-amber-500"
          />
          <div className="space-y-1">
            <p className="text-sm font-medium text-foreground">
              Burada randevu anında onaylanmaz.
            </p>
            <p className="text-sm leading-6 text-muted-foreground">
              Seçtiğin saat için işletmeye bir <strong>talep</strong> gönderirsin.
              İşletme onaylarsa randevun kesinleşir. Aynı saate başka talepler de
              gelebilir; işletme birini seçer.
            </p>
          </div>
        </CardContent>
      </Card>
    </section>
  );
}

function ServiceMenu({
  services
}: {
  services: NonNullable<PublicBusinessProfile["services"]>;
}) {
  return (
    <section aria-labelledby="hizmetler-basligi" className="space-y-4" id="hizmetler">
      <div>
        <h2
          className="text-xl font-semibold tracking-tight text-foreground"
          id="hizmetler-basligi"
        >
          Hizmetler
        </h2>
        <p className="mt-1 text-sm text-muted-foreground">
          Süre ve fiyat her seçenekte ayrı ayrı yazılıdır.
        </p>
      </div>

      {services.length === 0 ? (
        <EmptyNote text="Bu işletmenin hizmet menüsü henüz yayınlanmadı." />
      ) : (
        <div className="grid gap-3">
          {services.map((service) => (
            <ServiceCard key={service.id ?? service.name} service={service} />
          ))}
        </div>
      )}
    </section>
  );
}

function ServiceCard({
  service
}: {
  service: NonNullable<PublicBusinessProfile["services"]>[number];
}) {
  const variants = service.variants ?? [];

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">{service.name ?? "Hizmet"}</CardTitle>
        {service.categoryKey ? (
          <CardDescription>{service.categoryKey}</CardDescription>
        ) : null}
      </CardHeader>
      <CardContent>
        {variants.length === 0 ? (
          <EmptyNote text="Bu hizmet için seçenek bilgisi henüz yok." />
        ) : (
          <ul className="divide-y">
            {variants.map((variant) => (
              <li
                className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1 py-3 first:pt-0 last:pb-0"
                key={variant.id ?? variant.name}
              >
                <div>
                  <p className="text-sm font-medium text-foreground">
                    {variant.name ?? "Seçenek"}
                  </p>
                  <p className="text-sm text-muted-foreground">
                    {variant.durationMinutes ?? 0} dk
                  </p>
                </div>
                <p className="text-sm font-semibold text-foreground">
                  {formatMoney(variant.priceAmount, variant.currencyCode)}
                </p>
              </li>
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}

function BranchList({
  branches,
  staffNamesVisible
}: {
  branches: NonNullable<PublicBusinessProfile["branches"]>;
  staffNamesVisible: boolean;
}) {
  return (
    <section aria-labelledby="subeler-basligi" className="space-y-4" id="subeler">
      <h2
        className="text-xl font-semibold tracking-tight text-foreground"
        id="subeler-basligi"
      >
        {branches.length > 1 ? "Şubeler" : "Şube"}
      </h2>

      {branches.length === 0 ? (
        <EmptyNote text="Şube bilgisi henüz yayınlanmadı." />
      ) : (
        branches.map((branch) => (
          <BranchCard
            branch={branch}
            key={branch.slug}
            staffNamesVisible={staffNamesVisible}
          />
        ))
      )}
    </section>
  );
}

function BranchCard({
  branch,
  staffNamesVisible
}: {
  branch: NonNullable<PublicBusinessProfile["branches"]>[number];
  staffNamesVisible: boolean;
}) {
  const workingHours = sortWorkingHours(branch.workingHours ?? []);
  const staffMembers = branch.staffMembers ?? [];

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">{branch.displayName ?? "Şube"}</CardTitle>
        <CardDescription>
          {[branch.district, branch.city].filter(Boolean).join(", ") ||
            "Konum bilgisi yok"}
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {branch.addressLine ? (
          <p className="text-sm leading-6 text-muted-foreground">
            {branch.addressLine}
          </p>
        ) : null}

        {workingHours.length > 0 ? (
          <div className="space-y-1">
            <p className="text-sm font-medium text-foreground">Çalışma saatleri</p>
            <dl className="text-sm">
              {workingHours.map((hours) => (
                <div
                  className="flex justify-between gap-4 py-1"
                  key={hours.dayOfWeek}
                >
                  <dt className="text-muted-foreground">
                    {getDayLabel(hours.dayOfWeek)}
                  </dt>
                  <dd
                    className={
                      hours.isClosed
                        ? "text-muted-foreground"
                        : "font-medium text-foreground"
                    }
                  >
                    {hours.isClosed
                      ? "Kapalı"
                      : `${formatTime(hours.opensAt)} - ${formatTime(hours.closesAt)}`}
                  </dd>
                </div>
              ))}
            </dl>
          </div>
        ) : null}

        {/* Personel isimleri: isletme "gizle" secmisse backend zaten staffMembers'i BOS
            dondurur (PublicBusinessProfileComposer). Policy'yi burada da kontrol etmek
            ikinci bir kilit -- yanit sekli degisirse isim yine sizmaz. */}
        {staffNamesVisible && staffMembers.length > 0 ? (
          <>
            <Separator />
            <div className="space-y-2">
              <p className="text-sm font-medium text-foreground">Ekip</p>
              <div className="flex flex-wrap gap-1.5">
                {staffMembers.map((staff) => (
                  <Badge key={staff.id ?? staff.displayName} variant="outline">
                    {staff.displayName ?? "Personel"}
                  </Badge>
                ))}
              </div>
            </div>
          </>
        ) : null}
      </CardContent>
    </Card>
  );
}

function EmptyNote({ text }: { text: string }) {
  return (
    <p className="rounded-md border border-dashed p-4 text-sm text-muted-foreground">
      {text}
    </p>
  );
}

function sortWorkingHours(
  workingHours: NonNullable<
    NonNullable<PublicBusinessProfile["branches"]>[number]["workingHours"]
  >
) {
  return workingHours
    .slice()
    .sort(
      (left, right) =>
        dayOrder.indexOf(left.dayOfWeek ?? "") -
        dayOrder.indexOf(right.dayOfWeek ?? "")
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

// Sube saati "09:00:00" gelir; saniye musteriye bir sey anlatmaz.
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
