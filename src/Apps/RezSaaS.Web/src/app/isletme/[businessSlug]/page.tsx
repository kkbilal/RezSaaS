import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { getPublicBusinessProfile } from "@/features/public-discovery/api/public-businesses";
import { BusinessProfilePage } from "@/features/public-discovery/components/business-profile-page";
import { Card, CardDescription, CardHeader, CardTitle } from "@/shared/ui/card";

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
    return {
      title: "İşletme"
    };
  }

  return {
    description:
      state.profile.metadata?.seoDescription ||
      state.profile.description ||
      "RezSaaS işletme profili.",
    title:
      state.profile.metadata?.seoTitle ||
      state.profile.displayName ||
      "İşletme"
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
      <main className="studio-grid grid min-h-screen place-items-center px-4 py-10">
        <Card className="fade-up w-full max-w-2xl p-7 sm:p-9">
          <CardHeader>
            <CardTitle className="text-4xl sm:text-5xl">
              Profil yüklenemedi
            </CardTitle>
            <CardDescription className="max-w-xl">{state.reason}</CardDescription>
          </CardHeader>
        </Card>
      </main>
    );
  }

  return <BusinessProfilePage profile={state.profile} />;
}
