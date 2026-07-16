import {
  Armchair,
  Building2,
  CalendarClock,
  ChevronRight,
  Info,
  Scissors,
  Users
} from "lucide-react";
import Link from "next/link";
import type {
  BusinessProfileSettings,
  BusinessProfileSettingsState,
  BusinessSettingsOverview
} from "@/features/business/api/get-business-settings-overview";
import {
  BusinessProfileSettingsForm,
  type BusinessProfileSettingsDraft
} from "@/features/business/components/business-profile-settings-form";
import { Badge } from "@/components/ui/badge";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle
} from "@/components/ui/card";
import { routes, withTenant } from "@/shared/config/routes";

type BusinessSettingsPageProps = {
  overview: BusinessSettingsOverview;
};

export function BusinessSettingsPage({ overview }: BusinessSettingsPageProps) {
  const { profile, tenant } = overview;
  const branches = profile.branches ?? [];
  const services = profile.services ?? [];
  const staffCount = branches.reduce(
    (total, branch) => total + (branch.staffMembers?.length ?? 0),
    0
  );
  const tenantId = tenant.tenantId ?? "";
  const canManageSettings = (tenant.capabilities ?? []).includes(
    "business.settings.manage"
  );

  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-3xl font-semibold tracking-tight">İşletme ayarları</h1>
        <p className="mt-2 max-w-2xl text-sm text-muted-foreground">
          Salonunun public profilinde görünen bilgileri ve iptal politikasını buradan
          yönet. Şube, personel ve hizmet düzenlemeleri kendi panellerinden yapılır.
        </p>
      </header>

      <div className="grid gap-6 xl:grid-cols-[1fr_20rem]">
        <div className="space-y-6 xl:order-1">
          {overview.profileSettings.kind === "ready" ? (
            <BusinessProfileSettingsForm
              canManage={canManageSettings}
              initial={createBusinessProfileSettingsDraft(
                overview.profileSettings.profile
              )}
              tenantId={tenantId}
            />
          ) : (
            <ProfileSettingsClosedCard state={overview.profileSettings} />
          )}
        </div>

        <aside className="space-y-6 xl:order-2">
          <ContextCard overview={overview} />
          <QuickLinksCard
            branchCount={branches.length}
            serviceCount={services.length}
            staffCount={staffCount}
            tenantId={tenantId}
          />
        </aside>
      </div>
    </div>
  );
}

/* ---------------------------------------------------------------------------
   ISLETME BAGLAMI -- salt okunur
   --------------------------------------------------------------------------- */

function ContextCard({ overview }: { overview: BusinessSettingsOverview }) {
  const { tenant } = overview;
  const capabilities = tenant.capabilities ?? [];

  return (
    <Card>
      <CardHeader>
        <CardTitle>İşletme bağlamı</CardTitle>
        <CardDescription>
          Bu bilgiler hesabının işletme üyeliğinden üretilir; buradan değiştirilemez.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-3">
        <InfoRow
          label="İşletme"
          value={tenant.tenantDisplayName ?? tenant.tenantSlug ?? "İşletme"}
        />
        <InfoRow label="Rol" value={getRoleLabel(tenant.role)} />
        <InfoRow
          label="Kapsam"
          value={tenant.isTenantWide ? "Tüm işletme" : "Şube kapsamı"}
        />
        <div>
          <p className="mb-2 text-xs font-medium text-muted-foreground">Yetkiler</p>
          <div className="flex flex-wrap gap-1.5">
            {capabilities.length === 0 ? (
              <span className="text-sm text-muted-foreground">Kayıt yok</span>
            ) : (
              capabilities.map((capability) => (
                <Badge key={capability} variant="outline">
                  {capability}
                </Badge>
              ))
            )}
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-baseline justify-between gap-3">
      <span className="shrink-0 text-sm text-muted-foreground">{label}</span>
      <span className="min-w-0 break-words text-right text-sm font-medium">{value}</span>
    </div>
  );
}

/* ---------------------------------------------------------------------------
   YONETIM KAPILARI -- diger panellere kisayol
   --------------------------------------------------------------------------- */

function QuickLinksCard({
  branchCount,
  serviceCount,
  staffCount,
  tenantId
}: {
  branchCount: number;
  serviceCount: number;
  staffCount: number;
  tenantId: string;
}) {
  const links = [
    {
      count: `${branchCount} şube`,
      href: withTenant(routes.business.branches, tenantId),
      icon: Building2,
      label: "Şubeler"
    },
    {
      count: `${staffCount} personel`,
      href: withTenant(routes.business.staff, tenantId),
      icon: Users,
      label: "Personel"
    },
    {
      count: `${serviceCount} hizmet`,
      href: withTenant(routes.business.services, tenantId),
      icon: Scissors,
      label: "Hizmetler"
    },
    {
      count: "Haftalık plan",
      href: withTenant(routes.business.workingHours, tenantId),
      icon: CalendarClock,
      label: "Çalışma saatleri"
    },
    {
      count: "Koltuk / oda / cihaz",
      href: withTenant(routes.business.resources, tenantId),
      icon: Armchair,
      label: "Kaynaklar"
    }
  ] as const;

  return (
    <Card>
      <CardHeader>
        <CardTitle>Yönetim kapıları</CardTitle>
        <CardDescription>Diğer yönetim ekranlarına hızlı geçiş.</CardDescription>
      </CardHeader>
      <CardContent className="space-y-1">
        {links.map((link) => (
          <Link
            className="-mx-2 flex min-h-11 items-center gap-3 rounded-md px-2 py-2 transition-colors hover:bg-accent"
            href={link.href}
            key={link.label}
          >
            <span className="flex size-9 shrink-0 items-center justify-center rounded-md bg-muted">
              <link.icon aria-hidden className="size-4 text-muted-foreground" />
            </span>
            <span className="min-w-0 flex-1">
              <span className="block text-sm font-medium">{link.label}</span>
              <span className="block text-xs text-muted-foreground">{link.count}</span>
            </span>
            <ChevronRight aria-hidden className="size-4 shrink-0 text-muted-foreground" />
          </Link>
        ))}
      </CardContent>
    </Card>
  );
}

/* ---------------------------------------------------------------------------
   FORM KAPALI DURUMU
   --------------------------------------------------------------------------- */

function ProfileSettingsClosedCard({
  state
}: {
  state: Exclude<BusinessProfileSettingsState, { kind: "ready" }>;
}) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Profil ayarları şu anda açılamıyor</CardTitle>
        <CardDescription>{state.reason}</CardDescription>
      </CardHeader>
      <CardContent>
        <p className="flex items-start gap-2 rounded-md border bg-muted/40 p-4 text-sm text-muted-foreground">
          <Info aria-hidden className="mt-0.5 size-4 shrink-0" />
          Yetki ve işletme kapsamı doğrulanmadan ayar formu açılmaz. Gerekli yetki
          tanımlandığında bu form otomatik olarak kullanılabilir hâle gelir.
        </p>
      </CardContent>
    </Card>
  );
}

/* ---------------------------------------------------------------------------
   YARDIMCILAR
   --------------------------------------------------------------------------- */

function createBusinessProfileSettingsDraft(
  settings: BusinessProfileSettings
): BusinessProfileSettingsDraft {
  return {
    description: settings.description ?? "",
    displayName: settings.displayName ?? "",
    publicRules: settings.publicRules ?? "",
    seoDescription: settings.seoDescription ?? "",
    seoTitle: settings.seoTitle ?? "",
    staffDisplayPolicy: settings.staffDisplayPolicy ?? "ShowNames",
    // Response'ta cancellationCutoffHours non-nullable (varsayilan 0). String'e ceviririz.
    cancellationCutoffHours: String(settings.cancellationCutoffHours ?? 0)
  };
}

function getRoleLabel(role?: string | null) {
  if (role === "BusinessOwner") {
    return "İşletme sahibi";
  }
  if (role === "BranchManager") {
    return "Şube yöneticisi";
  }
  if (role === "Staff") {
    return "Personel";
  }
  return role ?? "Bilinmiyor";
}
