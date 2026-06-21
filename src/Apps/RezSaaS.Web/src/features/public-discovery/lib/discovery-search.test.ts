import assert from "node:assert/strict";
import test from "node:test";
import {
  buildDiscoverHref,
  getActiveDiscoveryFilterCount,
  getDiscoveryMetadataCopy,
  normalizeDiscoverySearchParams
} from "./discovery-search.ts";

test("normalizeDiscoverySearchParams trims, collapses and bounds public filters", () => {
  const params = normalizeDiscoverySearchParams({
    categoryKey: " Spa & Wellness ",
    city: "  İstanbul   Avrupa  ",
    district: [" Kadıköy ", "ignored"],
    searchText: "  saç    bakım  "
  });

  assert.deepEqual(params, {
    categoryKey: "spa-wellness",
    city: "İstanbul Avrupa",
    district: "Kadıköy",
    searchText: "saç bakım"
  });
});

test("buildDiscoverHref keeps shareable URL filters compact", () => {
  const href = buildDiscoverHref(
    {
      categoryKey: "spa",
      city: "İstanbul",
      district: "Kadıköy",
      searchText: "cilt bakımı"
    },
    {
      district: undefined
    }
  );

  assert.equal(
    href,
    "/kesfet?searchText=cilt+bak%C4%B1m%C4%B1&city=%C4%B0stanbul&categoryKey=spa"
  );
});

test("metadata and active count reflect selected public filters", () => {
  const params = normalizeDiscoverySearchParams({
    categoryKey: "clinic",
    city: "Ankara"
  });
  const metadata = getDiscoveryMetadataCopy(params);

  assert.equal(getActiveDiscoveryFilterCount(params), 2);
  assert.equal(metadata.title, "Ankara, clinic işletmeleri");
});
