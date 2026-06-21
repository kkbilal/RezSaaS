import type { PublicBusinessSearchParams } from "../api/public-businesses";
import { routes } from "../../../shared/config/routes.ts";

const textMaxLength = 80;
const categoryMaxLength = 48;

export type RawDiscoverySearchParams = Record<
  string,
  string | string[] | undefined
>;

export function normalizeDiscoverySearchParams(
  params: RawDiscoverySearchParams
): PublicBusinessSearchParams {
  return {
    categoryKey: normalizeCategoryKey(first(params.categoryKey)),
    city: normalizeTextParam(first(params.city)),
    district: normalizeTextParam(first(params.district)),
    searchText: normalizeTextParam(first(params.searchText))
  };
}

export function buildDiscoverHref(
  params: PublicBusinessSearchParams,
  patch: Partial<PublicBusinessSearchParams> = {}
) {
  const nextParams = normalizeDiscoverySearchParams({
    ...params,
    ...patch
  });
  const query = new URLSearchParams();

  appendQuery(query, "searchText", nextParams.searchText);
  appendQuery(query, "city", nextParams.city);
  appendQuery(query, "district", nextParams.district);
  appendQuery(query, "categoryKey", nextParams.categoryKey);

  const queryString = query.toString();

  return queryString
    ? `${routes.public.discover}?${queryString}`
    : routes.public.discover;
}

export function getActiveDiscoveryFilterCount(params: PublicBusinessSearchParams) {
  return [
    params.searchText,
    params.city,
    params.district,
    params.categoryKey
  ].filter(Boolean).length;
}

export function getDiscoveryMetadataCopy(params: PublicBusinessSearchParams) {
  const activeParts = [
    params.searchText,
    params.district,
    params.city,
    params.categoryKey
  ].filter(Boolean);

  if (activeParts.length === 0) {
    return {
      description:
        "RezSaaS üzerinde salon, spa, klinik ve stüdyo işletmelerini keşfet.",
      title: "Keşfet"
    };
  }

  const summary = activeParts.join(", ");

  return {
    description: `${summary} için RezSaaS üzerindeki public işletme profillerini ve onaylı rezervasyon akışını keşfet.`,
    title: `${summary} işletmeleri`
  };
}

function appendQuery(
  query: URLSearchParams,
  key: keyof PublicBusinessSearchParams,
  value?: string
) {
  if (value) {
    query.set(key, value);
  }
}

function first(value?: string | string[]) {
  return Array.isArray(value) ? value[0] : value;
}

function normalizeTextParam(value?: string) {
  const normalized = value?.trim().replace(/\s+/g, " ");

  if (!normalized) {
    return undefined;
  }

  return normalized.slice(0, textMaxLength);
}

function normalizeCategoryKey(value?: string) {
  const normalized = value
    ?.trim()
    .toLocaleLowerCase("en-US")
    .replace(/\s+/g, "-")
    .replace(/[^a-z0-9._-]/g, "")
    .replace(/-+/g, "-")
    .replace(/^-|-$/g, "");

  if (!normalized) {
    return undefined;
  }

  return normalized.slice(0, categoryMaxLength);
}
