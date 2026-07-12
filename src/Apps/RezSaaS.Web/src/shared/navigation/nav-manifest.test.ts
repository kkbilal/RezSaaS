import assert from "node:assert/strict";
import { readdirSync, statSync } from "node:fs";
import { dirname, join, relative, sep } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

import {
  buildAccessContext,
  can,
  type AccessContext
} from "../auth/access-context.ts";
import { BUSINESS_CAPABILITIES } from "../auth/capabilities.ts";
import {
  allManifestNodes,
  CUSTOMER_NAV,
  PANEL_NAV,
  PLATFORM_NAV,
  pruneNav,
  resolvePermission
} from "./nav-manifest.ts";

const here = dirname(fileURLToPath(import.meta.url));
const appDir = join(here, "..", "..", "app");

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
      .filter((segment) => !(segment.startsWith("(") && segment.endsWith(")")))
      .map((segment) =>
        segment.startsWith("[") && segment.endsWith("]") ? ":param" : segment
      );

    acc.add("/" + segments.join("/"));
  }

  return acc;
}

/* ===========================================================================
   1) MANIFEST <-> SAYFA: CIFT YONLU
   Tek yonlu kontrol yetmez. Ters yon (her sayfanin manifest kaydi var mi)
   olmadan, izin tanimlanmamis bir sayfa sessizce merge edilebilir.
   =========================================================================== */

test("manifestteki her rotanin bir page.tsx'i vardir", () => {
  const pages = collectPageRoutes(appDir, new Set<string>());
  const missing = allManifestNodes()
    .filter((node) => !pages.has(node.path))
    .map((node) => `${node.id} -> ${node.path}`);

  assert.deepEqual(
    missing,
    [],
    "Manifestte olup sayfasi olmayan rota(lar) -- canli 404 uretirler:\n  " +
      missing.join("\n  ")
  );
});

test("her page.tsx'in bir manifest kaydi ve izni vardir (izinsiz sayfa merge edilemez)", () => {
  const pages = collectPageRoutes(appDir, new Set<string>());
  const declared = new Set(allManifestNodes().map((node) => node.path));

  const unguarded = [...pages].filter(
    (page) => page !== "/_not-found" && !declared.has(page)
  );

  assert.deepEqual(
    unguarded,
    [],
    "Manifestte KAYITLI OLMAYAN sayfa(lar) var. Izin tablosunda yoklar, yani\n" +
      "resolvePermission() onlar icin null doner ve yetki kontrolu YAPILAMAZ:\n  " +
      unguarded.join("\n  ") +
      "\n\nnav-manifest.ts'e ekle (menude gorunmeyecekse hidden: true ile)."
  );
});

/* ===========================================================================
   2) RESOLVEPERMISSION: FAIL-CLOSED
   =========================================================================== */

test("resolvePermission bilinmeyen yol icin null doner (fail-closed)", () => {
  assert.equal(resolvePermission("/panel/uydurma-sayfa"), null);
  assert.equal(resolvePermission("/platform/gizli"), null);
});

test("resolvePermission dinamik segmentleri cozer", () => {
  assert.equal(resolvePermission("/isletme/guzellik-merkezi"), "public");
  assert.equal(
    resolvePermission("/platform/tenantlar/8f14e45f-ceea-467a-9575-000000000000/uyeler"),
    "platform.admin"
  );
});

test("panel detay rotalari izin tablosunda KAYITLI (menude yok ama korumali)", () => {
  // Menude gorunmeyen ama URL ile girilebilen sayfalar korumasiz kalmamali.
  assert.equal(resolvePermission("/platform/abuse"), "platform.admin");
  assert.equal(resolvePermission("/platform/tenantlar/yeni"), "platform.admin");
});

/* ===========================================================================
   3) CAN(): FAIL-CLOSED
   =========================================================================== */

const ownerContext: AccessContext = buildAccessContext({
  session: { account: {}, platformRoles: [] },
  tenants: [
    {
      capabilities: [
        BUSINESS_CAPABILITIES.manageAppointmentRequests,
        BUSINESS_CAPABILITIES.manageSettings,
        BUSINESS_CAPABILITIES.reportAbuse
      ],
      isTenantWide: true,
      role: "BusinessOwner",
      tenantDisplayName: "Güzellik Merkezi",
      tenantId: "t-1"
    }
  ]
});

const branchManagerContext: AccessContext = buildAccessContext({
  session: { account: {}, platformRoles: [] },
  tenants: [
    {
      branchId: "b-1",
      capabilities: [
        BUSINESS_CAPABILITIES.manageAppointmentRequests,
        BUSINESS_CAPABILITIES.reportAbuse
      ],
      isTenantWide: false,
      role: "BranchManager",
      tenantDisplayName: "Güzellik Merkezi",
      tenantId: "t-1"
    }
  ]
});

/** Backend'de capability listesi BOS DIZI. Panele girse her cagri 403 doner. */
const staffContext: AccessContext = buildAccessContext({
  session: { account: {}, platformRoles: [] },
  tenants: [
    {
      capabilities: [],
      isTenantWide: false,
      role: "Staff",
      tenantDisplayName: "Güzellik Merkezi",
      tenantId: "t-1"
    }
  ]
});

test("can() context yoksa her zaman false doner -- 'public' haric (fail-closed)", () => {
  assert.equal(can(null, "public"), true);
  assert.equal(can(null, "auth"), false);
  assert.equal(can(null, "platform.admin"), false);
  assert.equal(can(null, BUSINESS_CAPABILITIES.manageSettings), false);
  assert.equal(can(undefined, BUSINESS_CAPABILITIES.manageAppointmentRequests), false);
});

test("can() taninmayan capability'yi backend gonderse bile kabul etmez", () => {
  const context = buildAccessContext({
    session: { account: {}, platformRoles: [] },
    tenants: [
      {
        capabilities: ["business.uydurma.capability", "business.settings.manage"],
        isTenantWide: true,
        role: "BusinessOwner",
        tenantId: "t-1"
      }
    ]
  });

  // Bilinen capability gecti...
  assert.equal(can(context, BUSINESS_CAPABILITIES.manageSettings), true);
  // ...ama uydurma olan sete HIC girmedi.
  assert.equal(context.activeTenant?.capabilities.size, 1);
});

test("BusinessOwner uc capability'ye de sahiptir", () => {
  assert.equal(can(ownerContext, BUSINESS_CAPABILITIES.manageAppointmentRequests), true);
  assert.equal(can(ownerContext, BUSINESS_CAPABILITIES.manageSettings), true);
  assert.equal(can(ownerContext, BUSINESS_CAPABILITIES.reportAbuse), true);
});

test("BranchManager'da business.settings.manage YOKTUR (backend: 11 composer Owner-only)", () => {
  assert.equal(
    can(branchManagerContext, BUSINESS_CAPABILITIES.manageAppointmentRequests),
    true
  );
  assert.equal(can(branchManagerContext, BUSINESS_CAPABILITIES.manageSettings), false);
});

/* ===========================================================================
   4) PRUNENAV: menu gercekten budaniyor mu
   =========================================================================== */

test("BusinessOwner panel menusunun TAMAMINI gorur", () => {
  const nav = pruneNav(PANEL_NAV, ownerContext);
  assert.equal(nav.length, 2);
  assert.deepEqual(
    nav.map((group) => group.id),
    ["gunluk-is", "isletmem"]
  );
});

test("BranchManager 'Isletmem' grubunu HIC GORMEZ -- baslik bile render edilmez", () => {
  const nav = pruneNav(PANEL_NAV, branchManagerContext);

  assert.equal(nav.length, 1);
  assert.equal(nav[0]?.id, "gunluk-is");
  assert.equal(
    nav.some((group) => group.id === "isletmem"),
    false,
    "BranchManager 'Isletmem' grubunu goruyor -- her ogesinde 403 yiyecek."
  );
});

test("Staff panelde HICBIR SEY gormez (capability listesi bos)", () => {
  const nav = pruneNav(PANEL_NAV, staffContext);
  assert.deepEqual(nav, [], "Staff'a panel menusu gosteriliyor -- hepsi 403 tuzagi.");
});

test("yetki verisi cekilemezse BOS menu doner -- statik tam menuye ASLA dusulmez", () => {
  // Referans projedeki gercek acik: API cokunce buildFallbackNavigation() devreye giriyor
  // ve en dusuk yetkili kullanici TAM YONETICI menusunu goruyordu. Burada olmayacak.
  assert.deepEqual(pruneNav(PANEL_NAV, null), []);
  assert.deepEqual(pruneNav(PLATFORM_NAV, null), []);
  assert.deepEqual(pruneNav(CUSTOMER_NAV, null), []);
});

test("kimligi dogrulanmis ama tenant uyeligi olmayan kullanici panel gormez, hesabini gorur", () => {
  const plainCustomer = buildAccessContext({
    session: { account: {}, platformRoles: [] },
    tenants: []
  });

  assert.deepEqual(pruneNav(PANEL_NAV, plainCustomer), []);
  assert.deepEqual(pruneNav(PLATFORM_NAV, plainCustomer), []);
  assert.equal(pruneNav(CUSTOMER_NAV, plainCustomer).length, 1);
});

test("PlatformAdmin platform menusunu gorur, isletme paneli gormez", () => {
  const admin = buildAccessContext({
    session: { account: {}, platformRoles: ["PlatformAdmin"] },
    tenants: []
  });

  assert.equal(pruneNav(PLATFORM_NAV, admin).length, 1);
  assert.deepEqual(pruneNav(PANEL_NAV, admin), []);
});

test("PlatformSupport platform gormez (hicbir endpoint'e bagli degil)", () => {
  const support = buildAccessContext({
    session: { account: {}, platformRoles: ["PlatformSupport"] },
    tenants: []
  });

  assert.equal(can(support, "platform.admin"), false);
  assert.deepEqual(pruneNav(PLATFORM_NAV, support), []);
});

/* ===========================================================================
   5) SUBE KAPSAMI SOZLESMESI
   =========================================================================== */

test("sube-scoped uyelikte branchId tasinir (gonderilmezse backend 403 doner)", () => {
  assert.equal(branchManagerContext.activeTenant?.branchId, "b-1");
  assert.equal(ownerContext.activeTenant?.branchId, null);
  assert.equal(ownerContext.activeTenant?.isTenantWide, true);
});
