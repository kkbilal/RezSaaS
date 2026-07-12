import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

import {
  buildServicePayload,
  buildVariantPayload,
  canArchiveService,
  CATALOG_CURRENCY,
  describeCatalogError,
  describeServiceStatus,
  formatDuration,
  formatPrice,
  formatPriceRange,
  parseDurationInput,
  parsePriceInput
} from "./service-catalog.ts";

function readSource(relativePath: string) {
  return readFileSync(new URL(relativePath, import.meta.url), "utf8");
}

/* ===========================================================================
   1) "PATCH ADINDA, PUT DAVRANISINDA" TUZAGI
   Bu blok serinin en pahali hatasini kilitler: eksik alanli varyant guncellemesi
   sureyi 0'a dusurur (400) veya kaynak turunu SESSIZCE siler.
   =========================================================================== */

test("varyant govdesi HER ZAMAN bes alanin hepsini tasir (kismi gonderim imkansiz)", () => {
  const payload = buildVariantPayload({
    durationMinutes: 30,
    name: "Kısa Saç",
    priceAmount: 400,
    requiredResourceTypeId: null
  });

  // Alan SAYISI da sabitlenir: backend'e yeni bir zorunlu alan eklenirse bu test kirilir
  // ve biri "unutulmus alan" ile sessizce veri bozmadan once fark eder.
  assert.deepEqual(Object.keys(payload).sort(), [
    "currencyCode",
    "durationMinutes",
    "name",
    "priceAmount",
    "requiredResourceTypeId"
  ]);
});

test("varyant govdesinde kaynak turu null ise alan yine de GONDERILIR", () => {
  const payload = buildVariantPayload({
    durationMinutes: 45,
    name: "Uzun Saç",
    priceAmount: 600,
    requiredResourceTypeId: null
  });

  // Alanin VARLIGI onemli: backend UpdateResourceType(null) cagirir ve degeri temizler.
  // Alani hic gondermemek ile null gondermek AYNI sonucu verir -- ama alani unutmak
  // digerlerini de unutmaya davettir. Sozlesme: alan her zaman var.
  assert.ok("requiredResourceTypeId" in payload);
  assert.equal(payload.requiredResourceTypeId, null);
});

test("varyant govdesi kaynak turunu korur (secili tur silinmez)", () => {
  const payload = buildVariantPayload({
    durationMinutes: 45,
    name: "Uzun Saç",
    priceAmount: 600,
    requiredResourceTypeId: "8f14e45f-ceea-467a-9575-000000000000"
  });

  assert.equal(
    payload.requiredResourceTypeId,
    "8f14e45f-ceea-467a-9575-000000000000"
  );
});

test("para birimi HER ZAMAN TRY -- cagiran taraf secemez", () => {
  const payload = buildVariantPayload({
    durationMinutes: 30,
    name: "Kısa Saç",
    priceAmount: 400,
    requiredResourceTypeId: null
  });

  assert.equal(payload.currencyCode, "TRY");
  assert.equal(payload.currencyCode, CATALOG_CURRENCY);
});

test("varyant adi kirpilir (backend de kirpiyor; ekranda tutarli gorunsun)", () => {
  const payload = buildVariantPayload({
    durationMinutes: 30,
    name: "  Kısa Saç  ",
    priceAmount: 400,
    requiredResourceTypeId: null
  });

  assert.equal(payload.name, "Kısa Saç");
});

test("hizmet govdesi ad ve kategoriyi BIRLIKTE tasir", () => {
  const payload = buildServicePayload({ categoryKey: " Saç ", name: " Saç Kesimi " });

  // Servis PATCH'i de kosulsuz uygular (Rename + UpdateCategory): kategoriyi yollamamak
  // onu bozar. Iki alan da her zaman gider.
  assert.deepEqual(payload, { categoryKey: "Saç", name: "Saç Kesimi" });
});

/* ===========================================================================
   2) HAM API CAGRISI SIZINTISI
   Kapsulleme ancak ekranlar onu ATLAYAMIYORSA ise yarar.
   =========================================================================== */

test("hizmetler ekrani ham API cagrisi YAPMAZ (tum mutasyonlar client modulunden)", () => {
  const page = readSource("../components/business-services-page.tsx");
  const form = readSource("../components/service-variant-form.tsx");

  for (const [label, source] of [
    ["business-services-page.tsx", page],
    ["service-variant-form.tsx", form]
  ] as const) {
    for (const forbidden of [
      "createTenantApiClient",
      "createApiClient",
      "apiClient",
      ".PATCH(",
      ".POST(",
      ".DELETE("
    ]) {
      assert.ok(
        !source.includes(forbidden),
        `${label} icinde ham API cagrisi var ("${forbidden}"). ` +
          "Varyant mutasyonlari YALNIZCA business-service-client.ts uzerinden gecmeli -- " +
          "aksi halde 'PATCH ama PUT' tuzagi ekran ekran tekrar acilir."
      );
    }
  }
});

/* ===========================================================================
   3) ARSIVLEME ON KOSULU (uc "archive" diyor, gercekte KALICI SILME)
   =========================================================================== */

test("varyanti olan hizmet arsivlenemez (backend 409 SERVICE_HAS_VARIANTS)", () => {
  assert.equal(canArchiveService(0), true);
  assert.equal(canArchiveService(1), false);
  assert.equal(canArchiveService(7), false);
});

/* ===========================================================================
   4) BICIMLEME
   =========================================================================== */

test("sure dakikadan okunabilir metne cevrilir", () => {
  assert.equal(formatDuration(30), "30 dk");
  assert.equal(formatDuration(60), "1 sa");
  assert.equal(formatDuration(90), "1 sa 30 dk");
  assert.equal(formatDuration(0), "—");
  assert.equal(formatDuration(-5), "—");
});

test("gecersiz para birimi kodu Intl'i patlatmaz, ham yazilir", () => {
  // CurrencyCode serbest metin -> "XX" gibi bir kod gelebilir. Yanlis sembol basmaktansa
  // kodu oldugu gibi gosteririz.
  assert.equal(formatPrice(400, "XX"), "400.00 XX");
  assert.ok(formatPrice(400, "TRY").includes("400"));
});

test("fiyat araligi en dusuk-en yuksek olarak yazilir", () => {
  const range = formatPriceRange([
    { currencyCode: "TRY", priceAmount: 400 },
    { currencyCode: "TRY", priceAmount: 600 }
  ]);

  assert.ok(range.includes("400"));
  assert.ok(range.includes("600"));
  assert.ok(range.includes("–"), "aralik ayraci yok");
});

test("tek varyantta aralik degil TEK fiyat yazilir", () => {
  const range = formatPriceRange([{ currencyCode: "TRY", priceAmount: 400 }]);

  assert.ok(!range.includes("–"), `tek fiyat aralik gibi yazilmis: ${range}`);
});

test("varyant yoksa fiyat araligi 'Fiyat yok' der", () => {
  assert.equal(formatPriceRange([]), "Fiyat yok");
});

test("karisik para biriminde TEK aralik cizilmez (yanlis cumle kurar)", () => {
  // "400 TL - 600 TL" demek USD varyanti TL sanmak demektir. Anomaliyi gizlemeyiz.
  const range = formatPriceRange([
    { currencyCode: "TRY", priceAmount: 400 },
    { currencyCode: "USD", priceAmount: 600 }
  ]);

  assert.ok(!range.includes("–"), `karisik para biriminde aralik cizilmis: ${range}`);
  assert.ok(range.includes("·"), "fiyatlar tek tek listelenmemis");
});

test("statu rozeti METIN tasir (renk tek sinyal degil)", () => {
  assert.equal(describeServiceStatus("Active").label, "Aktif");
  assert.equal(describeServiceStatus("Archived").label, "Arşivlendi");
  assert.equal(describeServiceStatus("").label, "Bilinmiyor");
  // Backend yeni bir statu eklerse ham degeri gosteririz -- bos rozet basmayiz.
  assert.equal(describeServiceStatus("Draft").label, "Draft");
});

/* ===========================================================================
   5) GIRDI AYRISTIRMA
   =========================================================================== */

test("fiyat girdisi hem virgul hem nokta ondaligi kabul eder", () => {
  assert.equal(parsePriceInput("400"), 400);
  assert.equal(parsePriceInput("400,50"), 400.5);
  assert.equal(parsePriceInput("400.50"), 400.5);
  assert.equal(parsePriceInput(" 400,50 "), 400.5);
  assert.equal(parsePriceInput("0"), 0);
});

test("gecersiz/belirsiz fiyat girdisi reddedilir", () => {
  assert.equal(parsePriceInput(""), null);
  assert.equal(parsePriceInput("abc"), null);
  assert.equal(parsePriceInput("-5"), null);
  assert.equal(parsePriceInput("400,555"), null, "kurustan fazla ondalik kabul edilmis");
  // Binlik ayirici BELIRSIZ: 1.250 -> 1250 mi 1.25 mi? Sessizce yorumlamak yerine reddet.
  assert.equal(parsePriceInput("1.250,00"), null);
});

test("sure girdisi backend sinirlarini uygular (1..1440)", () => {
  assert.equal(parseDurationInput("30"), 30);
  assert.equal(parseDurationInput("1440"), 1440);
  assert.equal(parseDurationInput("0"), null);
  assert.equal(parseDurationInput("1441"), null);
  assert.equal(parseDurationInput("30,5"), null);
  assert.equal(parseDurationInput(""), null);
});

/* ===========================================================================
   6) HATA METINLERI
   =========================================================================== */

test("backend errorCode'lari salon sahibinin dilinde karsilik bulur", () => {
  assert.match(describeCatalogError("SERVICE_NAME_CONFLICT", 409), /zaten var/);
  assert.match(describeCatalogError("VARIANT_NAME_CONFLICT", 409), /zaten var/);
  assert.match(describeCatalogError("SERVICE_HAS_VARIANTS", 409), /önce tüm seçenekleri sil/i);
  assert.match(describeCatalogError(null, 403), /yetkin yok/);
  // Bilinmeyen kod sessiz kalmaz.
  assert.ok(describeCatalogError("YENI_KOD", 400).length > 0);
});
