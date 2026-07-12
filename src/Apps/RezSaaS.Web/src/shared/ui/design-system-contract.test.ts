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
  const requiredContracts = [
    "PanelTenantOption",
    "currentTenantId",
    "onTenantChange",
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
  const requiredContracts = [
    "routes.customer.dashboard",
    "routes.customer.requests",
    "routes.customer.appeals",
    "routes.customer.profile",
    "aria-expanded={mobileOpen}"
  ];

  for (const contract of requiredContracts) {
    assertIncludes(shell, contract);
  }
});

test("animated background exposes reference-style orbs and grid overlay", () => {
  const bg = readSource("animated-background.tsx");
  const requiredContracts = [
    "AnimatedBackground",
    "orb1 22s",
    "orb2 17s",
    "orb3 28s",
    "var(--rs-accent)",
    "var(--rs-accent-violet)",
    "var(--rs-chart-3)",
    "backgroundSize"
  ];

  for (const contract of requiredContracts) {
    assertIncludes(bg, contract);
  }
});

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

test("global tokens keep reference dark indigo/violet palette", () => {
  const css = readSource("../../app/globals.css");
  const requiredContracts = [
    "--rs-bg: #080c14",
    "--rs-accent: #6366f1",
    "--rs-accent-violet: #8b5cf6",
    "--rs-glass:",
    "rs-gradient-text",
    "rs-gradient-bg",
    "@keyframes orb1",
    "@keyframes orb2",
    "@keyframes orb3"
  ];

  for (const contract of requiredContracts) {
    assertIncludes(css, contract);
  }
});
