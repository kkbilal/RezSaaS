import type { Metadata } from "next";
import { searchPublicBusinesses } from "@/features/public-discovery/api/public-businesses";
import { DiscoverPage } from "@/features/public-discovery/components/discover-page";
import {
  buildDiscoverHref,
  getDiscoveryMetadataCopy,
  normalizeDiscoverySearchParams,
  type RawDiscoverySearchParams
} from "@/features/public-discovery/lib/discovery-search";

type DiscoverRouteProps = {
  searchParams: Promise<RawDiscoverySearchParams>;
};

export async function generateMetadata({
  searchParams
}: DiscoverRouteProps): Promise<Metadata> {
  const params = normalizeDiscoverySearchParams(await searchParams);
  const copy = getDiscoveryMetadataCopy(params);

  return {
    alternates: {
      canonical: buildDiscoverHref(params)
    },
    description: copy.description,
    title: copy.title
  };
}

export default async function DiscoverRoute({ searchParams }: DiscoverRouteProps) {
  const params = normalizeDiscoverySearchParams(await searchParams);
  const state = await searchPublicBusinesses(params);

  return <DiscoverPage params={params} state={state} />;
}
