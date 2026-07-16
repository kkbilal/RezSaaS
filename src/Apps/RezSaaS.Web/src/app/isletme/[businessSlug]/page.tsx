import type { Metadata } from "next";
import { notFound } from "next/navigation";
import {
  getPublicBusinessProfile,
  getPublicBusinessReviews
} from "@/features/public-discovery/api/public-businesses";
import { BusinessProfilePage } from "@/features/public-discovery/components/business-profile-page";
import { Card, CardContent } from "@/components/ui/card";
import { routes } from "@/shared/config/routes";

type BusinessProfileRouteProps = {
  params: Promise<{
    businessSlug: string;
  }>;
};

export async function generateMetadata({
  params
}: BusinessProfileRouteProps): Promise<Metadata> {
  const { businessSlug } = await params;
  const state = await getPublicBusinessProfile(businessSlug);

  if (state.kind !== "ready") {
    // Profil yoksa/gelmiyorsa INDEXLETME: bos ya da hatali bir sayfanin aramaya dusmesi,
    // urunun edinim kanalini kirletir.
    return {
      robots: {
        index: false
      },
      title: "İşletme"
    };
  }

  const { profile } = state;
  const title =
    profile.metadata?.seoTitle || profile.displayName || "İşletme";
  const description =
    profile.metadata?.seoDescription ||
    profile.description ||
    `${profile.displayName ?? "Bu işletme"} için uygun saatleri gör, randevu talebi gönder.`;

  return {
    alternates: {
      canonical: routes.public.businessProfile(businessSlug)
    },
    description,
    // PUBLIC yuzey: panelin aksine INDEXLENEBILIR olmali (docs: edinim kanali).
    openGraph: {
      description,
      title,
      type: "website"
    },
    robots: {
      follow: true,
      index: true
    },
    title
  };
}

export default async function BusinessProfileRoute({
  params
}: BusinessProfileRouteProps) {
  const { businessSlug } = await params;
  const state = await getPublicBusinessProfile(businessSlug);

  if (state.kind === "not-found") {
    notFound();
  }

  if (state.kind === "unavailable") {
    return (
      <main className="grid min-h-screen place-items-center bg-background px-4 py-10">
        <Card className="w-full max-w-md">
          <CardContent className="space-y-2">
            <h1 className="text-lg font-semibold tracking-tight text-foreground">
              Profil yüklenemedi
            </h1>
            <p className="text-sm text-muted-foreground">{state.reason}</p>
          </CardContent>
        </Card>
      </main>
    );
  }

  // Yorumlar profil ICIN zorunlu degil -- ayri uc, ayri hata yuzeyi. Yine de SSR'de
  // cekiliyor ki sosyal kanit indexlenebilir olsun ve ilk boyada gelsin.
  const reviewSummary = await getPublicBusinessReviews(businessSlug);

  return (
    <BusinessProfilePage profile={state.profile} reviewSummary={reviewSummary} />
  );
}
