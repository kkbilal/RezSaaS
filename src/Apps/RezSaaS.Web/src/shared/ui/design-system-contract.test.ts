import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

function readSource(relativePath: string) {
  return readFileSync(new URL(relativePath, import.meta.url), "utf8");
}

function assertIncludes(source: string, expected: string) {
  assert.ok(source.includes(expected), `${expected} is missing`);
}

test("global design tokens expose the frontend foundation", () => {
  const css = readSource("../../app/globals.css");
  const requiredTokens = [
    "--rs-bg:",
    "--rs-surface:",
    "--rs-surface-muted:",
    "--rs-border:",
    "--rs-border-strong:",
    "--rs-ink:",
    "--rs-muted:",
    "--rs-accent:",
    "--rs-danger:",
    "--rs-focus:",
    "--rs-radius:",
    "--rs-radius-lg:",
    "--rs-radius-xl:",
    "--rs-shadow-card:",
    "--rs-shadow-button:"
  ];

  for (const token of requiredTokens) {
    assertIncludes(css, token);
  }

  assertIncludes(css, "@media (prefers-reduced-motion: reduce)");
  assertIncludes(css, ".fade-up");
  assertIncludes(css, ".slide-in");
  assertIncludes(css, ".pulse-warning");
});

test("button primitive keeps variants and keyboard focus states", () => {
  const button = readSource("button.tsx");
  const requiredContracts = [
    '"primary"',
    '"secondary"',
    '"outline"',
    '"ghost"',
    '"danger"',
    '"success"',
    "focus-visible:ring",
    "disabled:pointer-events-none",
    "disabled:opacity-40",
    "rs-gradient-bg"
  ];

  for (const contract of requiredContracts) {
    assertIncludes(button, contract);
  }
});

test("dialog primitive keeps modal semantics and escape handling", () => {
  const dialog = readSource("dialog.tsx");
  const requiredContracts = [
    "DialogOverlay",
    "DialogPanel",
    "DialogFormPanel",
    "window.addEventListener(\"keydown\"",
    "event.key === \"Escape\"",
    "aria-labelledby={titleId}",
    "aria-describedby={descriptionId}",
    "aria-modal=\"true\"",
    "role=\"dialog\""
  ];

  for (const contract of requiredContracts) {
    assertIncludes(dialog, contract);
  }
});


// KALDIRILDI: "progress primitive renders step states"
//
// progress.tsx SILINDI. Tek kullanicisi public-booking-panel'di; panel shadcn'e tasinirken
// adim gostergesi panelin kendi icinde (BookingSteps) yeniden yazildi -- shadcn'de stepper
// primitifi YOK ve tek cagiran icin paylasilan bir primitif tutmak gereksizdi.
// aria-current="step" sozlesmesi BookingSteps'te KORUNDU.
//
// skeleton.tsx da SILINDI: TextSkeleton/CardSkeleton/ButtonSkeleton'in tek "kullanicisi"
// ayni paneldi ve import KULLANILMIYORDU (olu import). Yerine @/components/ui/skeleton.
// (Zaten bu testte skeleton assertion'i hic yoktu.)

test("avatar primitive exposes sizes and accessible labelling", () => {
  const avatar = readSource("avatar.tsx");
  const requiredContracts = [
    '"xs"',
    '"sm"',
    '"md"',
    '"lg"',
    "aria-label={name}",
    "role=\"img\"",
    "var(--rs-accent-soft)"
  ];

  for (const contract of requiredContracts) {
    assertIncludes(avatar, contract);
  }
});


test("panel shell keeps tenant switcher behind controlled props", () => {
  const shell = readSource("../../features/business/components/panel-shell.tsx");
  // NOT: "onTenantChange" diye bir prop hic olmadi -- assertion BAYATTI ve test bu yuzden
  // kirmizi kaliyordu. Gercek sozlesme: TenantSwitcher'a onSelect={handleTenantChange}
  // geciliyor ve secim URL query param'ina (?tenantId=) yaziliyor.
  const requiredContracts = [
    "PanelTenantOption",
    "currentTenantId",
    "handleTenantChange",
    "TenantSwitcher",
    "pendingRequestCount",
    "routes.business.panel",
    "routes.business.settings"
  ];

  for (const contract of requiredContracts) {
    assertIncludes(shell, contract);
  }
});

test("platform shell surfaces step-up status indicator", () => {
  const shell = readSource("../../features/platform/components/platform-shell.tsx");
  const requiredContracts = [
    "stepUpExpiresAtUtc",
    "Step-up aktif",
    "pulse-warning",
    "routes.platform.abuse",
    "routes.platform.tenants"
  ];

  for (const contract of requiredContracts) {
    assertIncludes(shell, contract);
  }
});

test("customer shell keeps navigation scopes and responsive toggle", () => {
  const shell = readSource("../../features/customer/components/customer-shell.tsx");
  // routes.customer.dashboard SILINDI: /hesabim'in sayfasi yoktu, nav ogesi canli 404 uretiyordu.
  //   Artik /hesabim dogrudan randevu listesine yonleniyor, nav ogesi degil.
  // routes.customer.appeals SILINDI: itiraz/moderasyon akisi MVP disi (docs/29).
  // routes.customer.requests -> routes.customer.appointments: musterinin zihninde "talep"
  //   diye bir nesne yok. Gercek sayfa /hesabim/randevular; /hesabim/talepler redirect oldu.
  const requiredContracts = [
    "routes.customer.appointments",
    "routes.customer.profile",
    "aria-expanded={mobileOpen}"
  ];

  for (const contract of requiredContracts) {
    assertIncludes(shell, contract);
  }
});

// KALDIRILDI: "animated background exposes reference-style orbs and grid overlay"
//
// animated-background.tsx SILINDI. O component dark-glassmorphism temasinin animasyonlu
// orb'lariydi; light-first karariyla (docs/29) tema degisti ve component olu koda dondu.
// Testi de birlikte gitti -- artik var olmayan bir sozlesmeyi assert ediyordu.

// KALDIRILDI: "public navbar keeps scroll-blur and gradient logo"
//
// shared/ui/public-navbar.tsx SILINDI; yerine components/public-header.tsx (shadcn) geldi.
// Testin assert ettigi sozlesmenin her maddesi artik bir KARARIN TERSI:
// - "setScrolled"/"backdrop-blur-2xl": scroll-blur bir client state'iydi; light-first
//   karariyla (docs/29) glass/blur birakildi ve baslik server component'e dondu.
// - "rs-gradient-bg": gradient logo ayni kararla kaldirildi.
// - "routes.auth.register": navbar'daki "Uye Ol" CTA'si SILINDI -- /kayit self-servis
//   ISLETME kaydi DEGIL, musteri hesabi aciyor (docs/29 K3: onboarding elle). Bkz. app/page.tsx.

test("global tokens are LIGHT-FIRST and expose the shadcn token layer", () => {
  const css = readSource("../../app/globals.css");

  // Eski sozlesme dark-only bir paleti (--rs-bg: #080c14, color-scheme: dark) assert ediyordu.
  // docs/29 karari: LIGHT-FIRST + cift tema. Hedef kullanici salon isletmecisi -- gun isiginda,
  // camekan onunde, tablette calisiyor.
  const requiredContracts = [
    "color-scheme: light",
    "--rs-bg: #f6f7fa", // acik zemin (eskiden #080c14)
    "--rs-accent: #4f46e5",
    // shadcn token katmani Tailwind utility'lerine bagli olmali (bg-background vb.)
    "@theme inline",
    "--color-background: var(--background)",
    "--color-sidebar: var(--sidebar)",
    // Dark tema kaybolmadi: acik tercih + isletim sistemi tercihi
    '[data-theme="dark"]',
    "@media (prefers-color-scheme: dark)",
    // Korunan yardimci siniflar (mevcut componentler kullaniyor)
    "rs-gradient-text",
    "rs-gradient-bg",
    "pulse-warning"
  ];

  for (const contract of requiredContracts) {
    assertIncludes(css, contract);
  }
});

test("dark glassmorphism kaliplari geri gelmemis olmali", () => {
  const css = readSource("../../app/globals.css");

  // docs/29: gradient/blur/glass kaliplarindan kacin. Bunlarin geri sizmasini engelle.
  for (const forbidden of ["@keyframes orb1", "#080c14", "color-scheme: dark;\n  --rs-bg: #080c14"]) {
    assert.ok(
      !css.includes(forbidden),
      `Dark glassmorphism kalibi geri gelmis: "${forbidden}"`
    );
  }

  // Harici Google Fonts @import'u: render-blocking + her ziyaretcinin IP'sini Google'a sizdirir.
  // Fontlar next/font ile self-host ediliyor (layout.tsx).
  assert.ok(
    !css.includes("fonts.googleapis.com"),
    "globals.css'te harici Google Fonts @import'u var -- next/font kullanilmali."
  );
});
