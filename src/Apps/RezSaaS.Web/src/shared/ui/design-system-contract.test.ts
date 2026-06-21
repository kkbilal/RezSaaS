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
    "--rs-shadow-card:",
    "--rs-shadow-button:"
  ];

  for (const token of requiredTokens) {
    assertIncludes(css, token);
  }

  assertIncludes(css, "@media (prefers-reduced-motion: reduce)");
  assertIncludes(css, ".fade-up");
});

test("button primitive keeps variants and keyboard focus states", () => {
  const button = readSource("button.tsx");
  const requiredContracts = [
    '"primary"',
    '"secondary"',
    '"ghost"',
    '"danger"',
    "focus-visible:outline",
    "focus-visible:outline-[var(--rs-focus)]",
    "disabled:pointer-events-none",
    "disabled:opacity-55"
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
