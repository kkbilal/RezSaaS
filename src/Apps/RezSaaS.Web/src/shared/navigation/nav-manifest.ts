import {
  Building2,
  CalendarDays,
  ClipboardList,
  Compass,
  Home,
  Inbox,
  LayoutDashboard,
  Scissors,
  Settings,
  ShieldAlert,
  Sofa,
  Tags,
  User,
  Users,
  type LucideIcon
} from "lucide-react";

// Goreli + .ts uzantili import: bu modul node --test ile UNIT TEST EDILEBILIR kalmali.
// (`@/` alias'ini node cozemiyor; menu budama mantiginin testsiz kalmasi kabul edilemez.)
import { can, type AccessContext } from "../auth/access-context.ts";
import { BUSINESS_CAPABILITIES, type Permission } from "../auth/capabilities.ts";
import { routes } from "../config/routes.ts";

/**
 * NAV MANIFEST -- menu agacinin ve rota->izin tablosunun TEK kaynagi.
 *
 * Bu dosyadan UC sey turetilir:
 *   1. Sidebar / nav agaci            (pruneNav)
 *   2. Rota -> izin cozum tablosu     (resolvePermission)  <-- URL ile atlatma buradan kapanir
 *   3. (Ileride) komut paleti aksiyonlari
 *
 * IKI KURAL:
 *
 * A) `permission` ZORUNLUDUR (opsiyonel degil, bir UNION). Yeni sayfa eklerken izin yazmayi
 *    unutmak DERLEME HATASI verir. Boylece "izin yazmayi unuttum -> sayfa herkese acildi"
 *    (fail-open) hatasi tip sistemi tarafindan engellenir.
 *
 * B) Menude GORUNMEYEN rotalar da (detay sayfalari, dinamik segmentler) buraya `hidden: true`
 *    ile YAZILIR. Aksi halde izin tablosunda hic bulunmazlar ve "menude yok ama URL'i elle
 *    yazinca giriliyor" deligi acilir. Urunun en yikici aksiyonlari tam olarak o detay
 *    sayfalarinda yasar.
 */

export type NavNode = {
  readonly id: string;
  readonly path: string;
  readonly label: string;
  readonly permission: Permission;
  readonly icon?: LucideIcon;
  /** Menude gorunmez ama izin tablosuna girer (detay/dinamik rotalar). */
  readonly hidden?: true;
  /** Rozet anahtari; sayiyi kabuk saglar. */
  readonly badge?: "pendingRequests";
  readonly children?: readonly NavNode[];
};

export type NavGroup = {
  readonly id: string;
  readonly label: string;
  /** Grubun tamami bu izne bagli. Izin yoksa BASLIK BILE render edilmez. */
  readonly permission: Permission;
  readonly items: readonly NavNode[];
};

/* ---------------------------------------------------------------------------
   PANEL (isletme kabugu)
   --------------------------------------------------------------------------- */

export const PANEL_NAV: readonly NavGroup[] = [
  {
    id: "gunluk-is",
    items: [
      {
        icon: LayoutDashboard,
        id: "panel-bugun",
        label: "Bugün",
        path: routes.business.panel,
        permission: BUSINESS_CAPABILITIES.manageAppointmentRequests
      },
      {
        badge: "pendingRequests",
        icon: Inbox,
        id: "panel-talepler",
        label: "Talepler",
        path: routes.business.requests,
        permission: BUSINESS_CAPABILITIES.manageAppointmentRequests
      },
      {
        icon: CalendarDays,
        id: "panel-takvim",
        label: "Takvim",
        path: routes.business.calendar,
        permission: BUSINESS_CAPABILITIES.manageAppointmentRequests
      },
      {
        icon: ClipboardList,
        id: "panel-randevular",
        label: "Randevular",
        path: routes.business.appointments,
        permission: BUSINESS_CAPABILITIES.manageAppointmentRequests
      }
    ],
    label: "Günlük iş",
    permission: BUSINESS_CAPABILITIES.manageAppointmentRequests
  },
  {
    // DIKKAT: Bu grubun TAMAMI business.settings.manage'e bagli ve o capability
    // backend'de YALNIZCA BusinessOwner'da var (11 composer da onu ariyor).
    // BranchManager'da bu grup HIC RENDER EDILMEZ -- baslik bile cikmaz.
    // Eskiden herkese gosteriliyordu ve sube muduru her ogede 403 yiyordu.
    id: "isletmem",
    items: [
      {
        icon: Users,
        id: "panel-ekip",
        label: "Ekip",
        path: routes.business.staff,
        permission: BUSINESS_CAPABILITIES.manageSettings
      },
      {
        icon: Scissors,
        id: "panel-hizmetler",
        label: "Hizmetler ve fiyatlar",
        path: routes.business.services,
        permission: BUSINESS_CAPABILITIES.manageSettings
      },
      {
        icon: CalendarDays,
        id: "panel-saatler",
        label: "Çalışma saatleri",
        path: routes.business.workingHours,
        permission: BUSINESS_CAPABILITIES.manageSettings
      },
      {
        // Kullanicinin dili: salon sahibi "kaynak" degil KOLTUK dusunur.
        // Adim 6'da /panel/kaynak-turleri ile TEK sayfaya (iki sekme) birlesecek.
        icon: Sofa,
        id: "panel-koltuklar",
        label: "Koltuklar ve ekipman",
        path: routes.business.resources,
        permission: BUSINESS_CAPABILITIES.manageSettings
      },
      {
        icon: Tags,
        id: "panel-kaynak-turleri",
        label: "Ekipman türleri",
        path: routes.business.resourceTypes,
        permission: BUSINESS_CAPABILITIES.manageSettings
      },
      {
        icon: Tags,
        id: "panel-yetenekler",
        label: "Yetkinlikler",
        path: routes.business.skills,
        permission: BUSINESS_CAPABILITIES.manageSettings
      },
      {
        icon: Building2,
        id: "panel-subeler",
        label: "Şubeler",
        path: routes.business.branches,
        permission: BUSINESS_CAPABILITIES.manageSettings
      },
      {
        icon: Settings,
        id: "panel-ayarlar",
        label: "İşletme ayarları",
        path: routes.business.settings,
        permission: BUSINESS_CAPABILITIES.manageSettings
      }
    ],
    label: "İşletmem",
    permission: BUSINESS_CAPABILITIES.manageSettings
  }
];

/* ---------------------------------------------------------------------------
   MUSTERI kabugu
   --------------------------------------------------------------------------- */

export const CUSTOMER_NAV: readonly NavGroup[] = [
  {
    id: "hesabim",
    items: [
      {
        icon: ClipboardList,
        id: "hesabim-randevular",
        label: "Randevularım",
        path: routes.customer.appointments,
        permission: "auth"
      },
      {
        icon: User,
        id: "hesabim-profil",
        label: "Profil",
        path: routes.customer.profile,
        permission: "auth"
      }
    ],
    label: "Hesabım",
    permission: "auth"
  }
];

/* ---------------------------------------------------------------------------
   PLATFORM kabugu (yalnizca PlatformAdmin)
   --------------------------------------------------------------------------- */

export const PLATFORM_NAV: readonly NavGroup[] = [
  {
    id: "platform",
    items: [
      {
        icon: LayoutDashboard,
        id: "platform-kontrol",
        label: "Kontrol merkezi",
        path: routes.platform.dashboard,
        permission: "platform.admin"
      },
      {
        // MVP'de platform admin'in TEK isi bu: yeni salon ac + BusinessOwner uyeligi ver.
        // (Self-servis isletme kaydi backend'de YOK -- onboarding elle yapiliyor, Karar K3.)
        icon: Building2,
        id: "platform-tenantlar",
        label: "İşletmeler",
        path: routes.platform.tenants,
        permission: "platform.admin"
      }
    ],
    label: "Platform",
    permission: "platform.admin"
  }
];

/* ---------------------------------------------------------------------------
   MENUDE GORUNMEYEN ROTALAR -- izin tablosuna yine de girerler.
   Bunlar olmasa "menude yok ama URL ile girilebiliyor" deligi acik kalirdi.
   --------------------------------------------------------------------------- */

/**
 * Dinamik segment yer tutucusu.
 *
 * DIKKAT: routes.* fonksiyonlari (or. routes.public.businessProfile) argumanlarini
 * encodeURIComponent'ten gecirir; ":param" verirsek "%3Aparam" olur ve hicbir sayfayla
 * eslesmez. Bu yuzden dinamik desenler burada LITERAL yazilir, fonksiyonla uretilmez.
 */
export const HIDDEN_ROUTES: readonly NavNode[] = [
  // Public
  { hidden: true, icon: Home, id: "public-home", label: "Ana sayfa", path: routes.public.home, permission: "public" },
  { hidden: true, icon: Compass, id: "public-kesfet", label: "Keşfet", path: routes.public.discover, permission: "public" },
  { hidden: true, id: "public-isletme", label: "İşletme profili", path: "/isletme/:param", permission: "public" },

  // Auth
  { hidden: true, id: "auth-giris", label: "Giriş", path: routes.auth.login, permission: "public" },
  { hidden: true, id: "auth-kayit", label: "Kayıt", path: routes.auth.register, permission: "public" },
  { hidden: true, id: "auth-sifremi-unuttum", label: "Şifremi unuttum", path: routes.auth.forgotPassword, permission: "public" },
  { hidden: true, id: "auth-sifre-sifirla", label: "Şifre sıfırla", path: routes.auth.resetPassword, permission: "public" },
  { hidden: true, id: "auth-gelis", label: "Rol dağıtıcı", path: routes.auth.dispatch, permission: "auth" },

  // Musteri -- /hesabim randevu listesine yonlenen bir redirect.
  { hidden: true, id: "hesabim-kok", label: "Hesabım", path: routes.customer.dashboard, permission: "auth" },
  // YON TERSINE CEVRILDI: gercek sayfa artik /hesabim/randevular. /hesabim/talepler
  // yalnizca ESKI LINKLER icin oraya yonlenen bir redirect. Sayfa dosyasi durdugu icin
  // izin tablosunda KAYITLI olmak zorunda (yoksa nav-manifest testi haklı olarak kirilir).
  { hidden: true, id: "hesabim-talepler-stub", label: "Randevularım", path: "/hesabim/talepler", permission: "auth" },
  // Itiraz/moderasyon MVP disi: sayfa duruyor ama menude YOK. Yine de izin tablosunda olmali.
  { hidden: true, id: "hesabim-itirazlar", label: "İtirazlar", path: routes.customer.appeals, permission: "auth" },

  // Platform -- detay ve suistimal sayfalari. Kod duruyor, menude YOK, ama URL ile
  // girilebildigi icin izin tablosunda platform.admin olarak KAYITLI olmalari SART.
  // (Kullanici karari: "abuselar kod olarak dursun" -- ama UI'da menuye girmezler.)
  { hidden: true, id: "platform-tenant-yeni", label: "Yeni işletme", path: routes.platform.newTenant, permission: "platform.admin" },
  { hidden: true, id: "platform-tenant-uyeler", label: "İşletme üyeleri", path: "/platform/tenantlar/:param/uyeler", permission: "platform.admin" },
  { hidden: true, icon: ShieldAlert, id: "platform-abuse", label: "Suistimal", path: routes.platform.abuse, permission: "platform.admin" },
  { hidden: true, id: "platform-abuse-kullanici", label: "Kullanıcı risk kartı", path: "/platform/abuse/kullanici/:param", permission: "platform.admin" },
  { hidden: true, id: "platform-itirazlar", label: "İtirazlar", path: routes.platform.appeals, permission: "platform.admin" }
];

/* ---------------------------------------------------------------------------
   TURETILENLER
   --------------------------------------------------------------------------- */

export const ALL_NAV_GROUPS: readonly NavGroup[] = [
  ...PANEL_NAV,
  ...CUSTOMER_NAV,
  ...PLATFORM_NAV
];

/** Manifestteki HER rota (gorunur + gizli). Rota -> izin tablosunun kaynagi. */
export function allManifestNodes(): NavNode[] {
  const nodes: NavNode[] = [...HIDDEN_ROUTES];

  for (const group of ALL_NAV_GROUPS) {
    for (const item of group.items) {
      nodes.push(item);
      if (item.children) {
        nodes.push(...item.children);
      }
    }
  }

  return nodes;
}

/**
 * Bir URL yolunun gerektirdigi izni bulur.
 *
 * FAIL-CLOSED: manifestte KAYITLI OLMAYAN bir yol icin `null` doner. Cagiran taraf
 * bunu "erisim yok" olarak yorumlamalidir -- "izin tanimlanmamis, o halde serbest" DEGIL.
 */
export function resolvePermission(pathname: string): Permission | null {
  const nodes = allManifestNodes();

  // Once birebir eslesme.
  const exact = nodes.find((node) => node.path === pathname);
  if (exact) {
    return exact.permission;
  }

  // Sonra dinamik segment eslesmesi (/isletme/:param gibi).
  for (const node of nodes) {
    if (!node.path.includes(":param")) {
      continue;
    }

    const pattern = new RegExp(
      "^" +
        node.path
          .split("/")
          .map((segment) => (segment === ":param" ? "[^/]+" : escapeRegExp(segment)))
          .join("/") +
        "$"
    );

    if (pattern.test(pathname)) {
      return node.permission;
    }
  }

  return null;
}

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

/**
 * Menu agacini kullanicinin yetkisine gore BUDAR. Saf fonksiyon.
 *
 * - Yetkisiz yaprak SILINIR (disabled gosterilmez -- tiklanabilir bir tuzak birakmayiz).
 * - Tum cocuklari elenen grup TAMAMEN gizlenir; BOS GRUP BASLIGI render edilmez.
 * - context null/hatali ise HER SEY elenir (FAIL-CLOSED). Statik tam menuye ASLA dusulmez:
 *   referans projede API cokunce en dusuk yetkili kullanici tam yonetici menusunu goruyordu.
 */
export function pruneNav(
  groups: readonly NavGroup[],
  context: AccessContext | null | undefined
): NavGroup[] {
  const visible: NavGroup[] = [];

  for (const group of groups) {
    if (!can(context, group.permission)) {
      continue;
    }

    const items = group.items.filter(
      (item) => !item.hidden && can(context, item.permission)
    );

    if (items.length === 0) {
      continue;
    }

    visible.push({ ...group, items });
  }

  return visible;
}
