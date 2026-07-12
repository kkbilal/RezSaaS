import assert from "node:assert/strict";
import { readdirSync, statSync } from "node:fs";
import { dirname, join, relative, sep } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

import { routes } from "./routes.ts";

// routes.ts TEK DOGRULUK KAYNAGIDIR: buradaki her rotanin src/app altinda bir page.tsx'i olmali.
//
// Bu test, uc CANLI 404'un (/panel/talepler, /panel/randevular, /hesabim) tekrar olusmasini
// yapisal olarak engeller. Ucu de sidebar/nav'da linkliydi; kullanici tek tiklamada 404 goruyordu.
// routes.ts'te ayrica sayfasi hic olmayan 14 "hayalet" rota vardi -- onlar da temizlendi.

const here = dirname(fileURLToPath(import.meta.url));
const appDir = join(here, "..", "..", "app");

/** src/app altindaki her page.tsx'i URL desenine cevirir. */
function collectPageRoutes(dir: string, acc: Set<string>): Set<string> {
  for (const entry of readdirSync(dir)) {
    const full = join(dir, entry);

    if (statSync(full).isDirectory()) {
      collectPageRoutes(full, acc);
      continue;
    }

    if (entry !== "page.tsx") {
      continue;
    }

    const segments = relative(appDir, dirname(full))
      .split(sep)
      .filter((segment) => segment.length > 0)
      // Route group'lar URL'de gorunmez: src/app/(auth)/giris -> /giris
      .filter((segment) => !(segment.startsWith("(") && segment.endsWith(")")))
      // Dinamik segmentleri normalize et: [tenantId] -> :param
      .map((segment) =>
        segment.startsWith("[") && segment.endsWith("]") ? ":param" : segment
      );

    acc.add("/" + segments.join("/"));
  }

  return acc;
}

/** routes objesindeki tum rotalari duzlestirir; fonksiyonlari ornek arguman ile cagirir. */
function collectDeclaredRoutes(
  node: unknown,
  path: string,
  acc: Array<{ key: string; route: string }>
): Array<{ key: string; route: string }> {
  if (typeof node === "string") {
    acc.push({ key: path, route: node });
    return acc;
  }

  if (typeof node === "function") {
    // Dinamik rota: ornek bir deger vererek deseni cikar, sonra :param'a normalize et.
    const produced = (node as (value: string) => string)("ornek");
    acc.push({ key: path, route: produced.replace(/\/ornek(?=\/|$)/g, "/:param") });
    return acc;
  }

  if (node && typeof node === "object") {
    for (const [key, value] of Object.entries(node)) {
      collectDeclaredRoutes(value, path ? `${path}.${key}` : key, acc);
    }
  }

  return acc;
}

test("routes.ts'teki her rotanin bir page.tsx'i vardir (canli 404 yasak)", () => {
  const pages = collectPageRoutes(appDir, new Set<string>());
  const declared = collectDeclaredRoutes(routes, "", []);

  const missing = declared.filter(({ route }) => !pages.has(route));

  assert.deepEqual(
    missing,
    [],
    "Sayfasi olmayan rota(lar) var -- bunlar canli 404 uretir:\n" +
      missing.map(({ key, route }) => `  routes.${key} -> ${route}`).join("\n") +
      "\n\nYa sayfayi yaz, ya rotayi routes.ts'ten sil."
  );
});
