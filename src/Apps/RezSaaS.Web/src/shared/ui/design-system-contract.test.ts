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

test("badge primitive exposes semantic variants backed by tokens", () => {
  const badge = readSource("badge.tsx");
  const requiredContracts = [
    '"default"',
    '"success"',
    '"warning"',
    '"danger"',
    '"info"',
    '"purple"',
    '"orange"',
    '"accent"',
    "font-mono",
    "var(--rs-success-soft)",
    "var(--rs-warning-soft)",
    "var(--rs-danger-soft)",
    "var(--rs-accent-soft)",
    "var(--rs-accent-violet-soft)"
  ];

  for (const contract of requiredContracts) {
    assertIncludes(badge, contract);
  }
});

test("tabs primitive keeps tablist semantics and keyboard focus", () => {
  const tabs = readSource("tabs.tsx");
  const requiredContracts = [
    "role=\"tablist\"",
    "role=\"tab\"",
    "aria-selected={selected}",
    "aria-controls=",
    "focus-visible:outline",
    "disabled:pointer-events-none"
  ];

  for (const contract of requiredContracts) {
    assertIncludes(tabs, contract);
  }
});

test("progress primitive renders step states", () => {
  const progress = readSource("progress.tsx");
  const requiredContracts = [
    '"complete"',
    '"current"',
    '"upcoming"',
    "aria-current={step.state === \"current\" ? \"step\" : undefined}",
    "var(--rs-ink)",
    "var(--rs-accent-soft)"
  ];

  for (const contract of requiredContracts) {
    assertIncludes(progress, contract);
  }
});

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

test("separator primitive keeps separator role for both orientations", () => {
  const separator = readSource("separator.tsx");
  assertIncludes(separator, "role=\"separator\"");
  assertIncludes(separator, '"horizontal"');
  assertIncludes(separator, '"vertical"');
});

test("calendar-grid primitive keeps branch-timezone-aware scheduling contract", () => {
  const calendar = readSource("calendar-grid.tsx");
  const requiredContracts = [
    "CalendarEvent",
    '"day"',
    '"week"',
    "branchTimeZoneId",
    "getBranchTimeParts",
    "formatBranchTimeLabel",
    "formatBranchDateLabel"
  ];

  for (const contract of requiredContracts) {
    assertIncludes(calendar, contract);
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
  //   Artik /hesabim dogrudan randevu/talep listesine yonleniyor, nav ogesi degil.
  // routes.customer.appeals SILINDI: itiraz/moderasyon akisi MVP disi (docs/29).
  const requiredContracts = [
    "routes.customer.requests",
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

test("public navbar keeps scroll-blur and gradient logo", () => {
  const nav = readSource("public-navbar.tsx");
  const requiredContracts = [
    "PublicNavbar",
    "setScrolled",
    "backdrop-blur-2xl",
    "rs-gradient-bg",
    "routes.public.discover",
    "routes.auth.login",
    "routes.auth.register"
  ];

  for (const contract of requiredContracts) {
    assertIncludes(nav, contract);
  }
});

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
