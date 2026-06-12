import type { Metadata } from "next";
import {
  searchPublicBusinesses,
  type PublicBusinessSearchParams
} from "@/features/public-discovery/api/public-businesses";
import { DiscoverPage } from "@/features/public-discovery/components/discover-page";

type DiscoverRouteProps = {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

export const metadata: Metadata = {
  description:
    "RezSaaS üzerinde salon, spa, klinik ve stüdyo işletmelerini keşfet.",
  title: "Keşfet"
};

export default async function DiscoverRoute({ searchParams }: DiscoverRouteProps) {
  const params = normalizeSearchParams(await searchParams);
  const state = await searchPublicBusinesses(params);

  return <DiscoverPage params={params} state={state} />;
}

function normalizeSearchParams(
  params: Record<string, string | string[] | undefined>
): PublicBusinessSearchParams {
  return {
    categoryKey: first(params.categoryKey),
    city: first(params.city),
    district: first(params.district),
    searchText: first(params.searchText)
  };
}

function first(value?: string | string[]) {
  return Array.isArray(value) ? value[0] : value;
}
