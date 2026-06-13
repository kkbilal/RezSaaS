import Link from "next/link";
import type { BusinessTenantContext } from "@/features/business/api/get-business-context";
import type {
  BusinessProfileSettings,
  BusinessProfileSettingsState,
  BusinessSettingsOverview
} from "@/features/business/api/get-business-settings-overview";
import {
  BusinessProfileSettingsForm,
  type BusinessProfileSettingsDraft
} from "@/features/business/components/business-profile-settings-form";
import type { PublicBusinessProfile } from "@/features/public-discovery/api/public-businesses";
import { routes, withTenant } from "@/shared/config/routes";
import { Button } from "@/shared/ui/button";
import { Card, CardDescription, CardHeader, CardTitle } from "@/shared/ui/card";
import { StatusBadge } from "@/shared/ui/status-badge";

type BusinessSettingsPageProps = {
  overview: BusinessSettingsOverview;
  sessionEmail: string;
  tenantOptions: BusinessTenantContext[];
};

const dayLabels: Record<string, string> = {
  Friday: "Cuma",
  Monday: "Pazartesi",
  Saturday: "Cumartesi",
  Sunday: "Pazar",
  Thursday: "Perşembe",
  Tuesday: "Salı",
  Wednesday: "Çarşamba"
};

export function BusinessSettingsPage({
  overview,
  sessionEmail,
  tenantOptions
}: BusinessSettingsPageProps) {
  const { profile, tenant } = overview;
  const branches = profile.branches ?? [];
  const services = profile.services ?? [];
  const staffCount = branches.reduce(
    (total, branch) => total + (branch.staffMembers?.length ?? 0),
    0
  );
  const variantCount = services.reduce(
    (total, service) => total + (service.variants?.length ?? 0),
    0
  );
  const workingHoursCount = branches.reduce(
    (total, branch) => total + (branch.workingHours?.length ?? 0),
    0
  );
  const galleryCount = profile.metadata?.galleryImages?.length ?? 0;
  const canManageSettings = (tenant.capabilities ?? []).includes(
    "business.settings.manage"
  );

  return (
    <main className="studio-grid min-h-screen px-4 py-6 sm:px-8">
      <div className="mx-auto max-w-7xl space-y-8">
        <SettingsHeader
          profileSlug={profile.slug ?? tenant.tenantSlug}
          sessionEmail={sessionEmail}
          tenantId={tenant.tenantId}
        />

        <section className="fade-up rounded-[2.5rem] border border-[var(--rs-border)] bg-white/76 p-6 shadow-[var(--rs-shadow-card)] backdrop-blur-xl sm:p-8">
          <div className="flex flex-col gap-8 lg:flex-row lg:items-end lg:justify-between">
            <div className="max-w-4xl space-y-5">
              <p className="w-fit rounded-full bg-[var(--rs-accent-soft)] px-4 py-2 text-sm font-medium text-[var(--rs-accent-strong)]">
                İşletme yönetimi
              </p>
              <h1 className="text-5xl font-semibold tracking-[-0.07em] text-[var(--rs-ink)] sm:text-7xl">
                Yayındaki salon vitrininin canlı yönetim snapshot&apos;ı.
              </h1>
              <p className="max-w-2xl text-lg leading-8 text-[var(--rs-muted-strong)]">
                Bu F6 dilimi public profil metnini gerçek business settings API
                ile kaydeder. Şube, personel, hizmet ve çalışma saati
                düzenlemeleri ise ilgili CRUD endpointleri tamamlanmadan açılmaz.
              </p>
            </div>

            <div className="grid min-w-80 grid-cols-2 gap-3">
              <MetricCard label="Şube" value={branches.length} />
              <MetricCard label="Personel" value={staffCount} />
              <MetricCard label="Hizmet" value={services.length} />
              <MetricCard label="Varyant" value={variantCount} />
            </div>
          </div>
        </section>

        <div className="grid gap-6 xl:grid-cols-[24rem_1fr]">
          <aside className="space-y-6">
            <TenantSwitcherCard
              activeTenantId={tenant.tenantId}
              routePath={routes.business.settings}
              tenants={tenantOptions}
            />
            <TenantScopeCard overview={overview} />
            <CapabilityMapCard />
          </aside>

          <section className="space-y-6">
            <ProfileReadinessCard
              galleryCount={galleryCount}
              profile={profile}
              workingHoursCount={workingHoursCount}
            />
            {overview.profileSettings.kind === "ready" ? (
              <BusinessProfileSettingsForm
                canManage={canManageSettings}
                initial={createBusinessProfileSettingsDraft(
                  overview.profileSettings.profile
                )}
                tenantId={tenant.tenantId}
              />
            ) : (
              <ProfileSettingsStateCard state={overview.profileSettings} />
            )}
            <div className="grid gap-6 xl:grid-cols-2">
              <BranchSnapshotCard branches={branches} />
              <ServiceSnapshotCard services={services} />
            </div>
          </section>
        </div>
      </div>
    </main>
  );
}

function SettingsHeader({
  profileSlug,
  sessionEmail,
  tenantId
}: {
  profileSlug?: string | null;
  sessionEmail: string;
  tenantId?: string | null;
}) {
  return (
    <header className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <Link
        className="text-lg font-semibold tracking-[-0.04em] text-[var(--rs-ink)]"
        href={routes.public.home}
      >
        RezSaaS
      </Link>
      <div className="flex flex-wrap items-center gap-3">
        <span className="rounded-full border border-[var(--rs-border)] bg-white px-4 py-2 text-sm text-[var(--rs-muted)]">
          {sessionEmail}
        </span>
        <Button asChild variant="secondary">
          <Link href={withTenant(routes.business.panel, tenantId)}>
            Operasyon paneli
          </Link>
        </Button>
        {profileSlug ? (
          <Button asChild variant="ghost">
            <Link href={routes.public.businessProfile(profileSlug)}>
              Public profili gör
            </Link>
          </Button>
        ) : null}
      </div>
    </header>
  );
}

function TenantSwitcherCard({
  activeTenantId,
  routePath,
  tenants
}: {
  activeTenantId?: string | null;
  routePath: string;
  tenants: BusinessTenantContext[];
}) {
  if (tenants.length <= 1) {
    return null;
  }

  return (
    <Card className="p-6">
      <CardHeader>
        <CardTitle>İşletme seçimi</CardTitle>
        <CardDescription>
          Yalnızca hesabına bağlı işletmeler arasında geçiş yapılır.
        </CardDescription>
      </CardHeader>

      <div className="mt-5 space-y-2">
        {tenants.map((tenant) => {
          const isActive = tenant.tenantId === activeTenantId;

          return (
            <Link
              className={
                isActive
                  ? "block rounded-2xl border border-[var(--rs-border-strong)] bg-white px-4 py-3 text-sm font-medium text-[var(--rs-ink)] shadow-[var(--rs-shadow-soft)]"
                  : "block rounded-2xl border border-[var(--rs-border)] bg-white/62 px-4 py-3 text-sm text-[var(--rs-muted)] transition hover:border-[var(--rs-border-strong)] hover:text-[var(--rs-ink)]"
              }
              href={withTenant(routePath, tenant.tenantId)}
              key={tenant.tenantId ?? tenant.membershipId}
            >
              <span className="block">
                {tenant.tenantDisplayName ?? tenant.tenantSlug ?? "İşletme"}
              </span>
              <span className="mt-1 block text-xs opacity-70">
                {getRoleLabel(tenant.role)}
              </span>
            </Link>
          );
        })}
      </div>
    </Card>
  );
}

function MetricCard({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-[1.5rem] bg-[var(--rs-ink)] p-4 text-white shadow-[var(--rs-shadow-card)]">
      <p className="text-[0.65rem] uppercase tracking-[0.18em] text-white/50">
        {label}
      </p>
      <p className="mt-5 text-4xl font-semibold tracking-[-0.07em]">{value}</p>
    </div>
  );
}

function TenantScopeCard({
  overview
}: {
  overview: BusinessSettingsOverview;
}) {
  const { tenant } = overview;

  return (
    <Card className="p-6">
      <CardHeader>
        <CardTitle>Yetki bağlamı</CardTitle>
        <CardDescription>
          Business context backend tarafından hesap üyeliğinden üretilir.
        </CardDescription>
      </CardHeader>

      <div className="mt-6 space-y-3">
        <InfoLine
          label="İşletme"
          value={tenant.tenantDisplayName ?? tenant.tenantSlug ?? "İşletme"}
        />
        <InfoLine label="Rol" value={getRoleLabel(tenant.role)} />
        <InfoLine
          label="Kapsam"
          value={
            tenant.isTenantWide
              ? "Tüm işletme"
              : tenant.branchId
                ? `Şube ${shortGuid(tenant.branchId)}`
                : "Kapsam yok"
          }
        />
        <div className="rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface-muted)] p-4">
          <p className="text-xs text-[var(--rs-muted)]">Capability</p>
          <div className="mt-3 flex flex-wrap gap-2">
            {(tenant.capabilities ?? []).length === 0 ? (
              <span className="text-sm text-[var(--rs-muted)]">Kayıt yok</span>
            ) : (
              tenant.capabilities?.map((capability) => (
                <span
                  className="rounded-full bg-white px-3 py-1 text-xs text-[var(--rs-muted)]"
                  key={capability}
                >
                  {capability}
                </span>
              ))
            )}
          </div>
        </div>
      </div>
    </Card>
  );
}

function CapabilityMapCard() {
  const items = [
    {
      description: "GET /api/business/context ile aktif tenant ve branch scope.",
      label: "İşletme bağlamı",
      status: "Healthy"
    },
    {
      description:
        "Public profil read model'iyle şube, personel, hizmet ve çalışma saati görünür.",
      label: "Vitrin snapshot",
      status: "Healthy"
    },
    {
      description:
        "Public profil metni BusinessOwner için gerçek settings API ile kaydedilir.",
      label: "Profil ayarı",
      status: "Healthy"
    },
    {
      description:
        "Personel, hizmet, şube, çalışma saati ve galeri mutation endpointleri ayrı dilimde bekliyor.",
      label: "Operasyon formları",
      status: "Degraded"
    },
    {
      description:
        "Randevu operasyonları ayrı panelde gerçek tenant header ve idempotency ile çalışır.",
      label: "Operasyon aksiyonları",
      status: "Healthy"
    }
  ] as const;

  return (
    <Card className="p-6">
      <CardHeader>
        <CardTitle>Yönetim kapıları</CardTitle>
        <CardDescription>
          Backend kontratı olmayan ayar formu bu ekranda üretilmez.
        </CardDescription>
      </CardHeader>

      <div className="mt-6 space-y-3">
        {items.map((item) => (
          <div
            className="rounded-2xl border border-[var(--rs-border)] bg-white p-4"
            key={item.label}
          >
            <div className="flex items-start justify-between gap-3">
              <p className="font-medium text-[var(--rs-ink)]">{item.label}</p>
              <StatusBadge status={item.status} />
            </div>
            <p className="mt-2 text-sm leading-6 text-[var(--rs-muted)]">
              {item.description}
            </p>
          </div>
        ))}
      </div>
    </Card>
  );
}

function ProfileSettingsStateCard({
  state
}: {
  state: Exclude<BusinessProfileSettingsState, { kind: "ready" }>;
}) {
  return (
    <Card className="p-5">
      <CardHeader>
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <CardTitle>Public profil formu kapalı</CardTitle>
            <CardDescription className="mt-2">{state.reason}</CardDescription>
          </div>
          <StatusBadge
            status={state.kind === "unsupported" ? "Degraded" : "PendingReview"}
          />
        </div>
      </CardHeader>
      <p className="mt-5 rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface-muted)] p-4 text-sm leading-6 text-[var(--rs-muted)]">
        Backend yetki ve tenant scope doğrulanmadan mutation formu açılmaz. Şube,
        personel, hizmet, çalışma saati ve galeri ayarları için hâlâ ayrı endpoint
        kontratları gereklidir.
      </p>
    </Card>
  );
}

function createBusinessProfileSettingsDraft(
  settings: BusinessProfileSettings
): BusinessProfileSettingsDraft {
  return {
    description: settings.description ?? "",
    displayName: settings.displayName ?? "",
    publicRules: settings.publicRules ?? "",
    seoDescription: settings.seoDescription ?? "",
    seoTitle: settings.seoTitle ?? "",
    staffDisplayPolicy: settings.staffDisplayPolicy ?? "ShowNames"
  };
}

function ProfileReadinessCard({
  galleryCount,
  profile,
  workingHoursCount
}: {
  galleryCount: number;
  profile: PublicBusinessProfile;
  workingHoursCount: number;
}) {
  return (
    <Card className="p-5">
      <CardHeader>
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <CardTitle>{profile.displayName ?? "İşletme vitrini"}</CardTitle>
            <CardDescription className="mt-2">
              {profile.slug ?? "Slug yok"} · {profile.categoryKey ?? "Kategori yok"}
            </CardDescription>
          </div>
          <StatusBadge status="Healthy" />
        </div>
      </CardHeader>

      <div className="mt-6 grid gap-3 md:grid-cols-4">
        <InfoBox label="Galeri" value={`${galleryCount} görsel`} />
        <InfoBox
          label="Yorum"
          value={
            (profile.metadata?.reviewCount ?? 0) > 0
              ? `${profile.metadata?.ratingAverage?.toFixed(1) ?? "0.0"} / 5`
              : "Henüz yok"
          }
        />
        <InfoBox
          label="Personel policy"
          value={getStaffPolicyCopy(profile.metadata?.staffDisplayPolicy)}
        />
        <InfoBox label="Çalışma saati" value={`${workingHoursCount} kayıt`} />
      </div>

      <TextBlock
        label="Açıklama"
        value={
          profile.description ||
          "Public açıklama henüz yok. BusinessOwner profil ayar formundan bu metni güncelleyebilir."
        }
      />
      <TextBlock
        label="Public kurallar"
        value={
          profile.metadata?.publicRules ||
          "Public kural metni henüz yayınlanmadı."
        }
      />
    </Card>
  );
}

function BranchSnapshotCard({
  branches
}: {
  branches: NonNullable<PublicBusinessProfile["branches"]>;
}) {
  return (
    <Card className="p-5">
      <CardHeader>
        <CardTitle>Şube ve personel snapshot</CardTitle>
        <CardDescription>
          Public profilde görünen branch/staff verisi. Internal resource bilgisi
          burada gösterilmez.
        </CardDescription>
      </CardHeader>

      <div className="mt-5 grid gap-3">
        {branches.length === 0 ? (
          <EmptyState text="Public profilde şube bilgisi yok." />
        ) : (
          branches.map((branch) => (
            <article
              className="rounded-[1.5rem] border border-[var(--rs-border)] bg-white/74 p-4"
              key={branch.slug ?? branch.displayName}
            >
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <h2 className="text-xl font-semibold tracking-[-0.04em] text-[var(--rs-ink)]">
                    {branch.displayName ?? "Şube"}
                  </h2>
                  <p className="mt-1 text-sm text-[var(--rs-muted)]">
                    {[branch.district, branch.city].filter(Boolean).join(", ") ||
                      "Konum bilgisi yok"}
                  </p>
                </div>
                <span className="rounded-full bg-[var(--rs-neutral-soft)] px-3 py-1 text-xs text-[var(--rs-muted)]">
                  {branch.timeZoneId ?? "Timezone yok"}
                </span>
              </div>

              <div className="mt-4 grid gap-2 text-xs text-[var(--rs-muted)]">
                {(branch.workingHours ?? []).slice(0, 7).map((hours) => (
                  <div
                    className="flex items-center justify-between rounded-2xl bg-[var(--rs-surface-muted)] px-3 py-2"
                    key={hours.dayOfWeek}
                  >
                    <span>{getDayLabel(hours.dayOfWeek)}</span>
                    <span>
                      {hours.isClosed
                        ? "Kapalı"
                        : `${formatTime(hours.opensAt)} - ${formatTime(hours.closesAt)}`}
                    </span>
                  </div>
                ))}
              </div>

              <div className="mt-4 flex flex-wrap gap-2">
                {(branch.staffMembers ?? []).length === 0 ? (
                  <span className="rounded-full border border-dashed border-[var(--rs-border)] px-3 py-1 text-xs text-[var(--rs-muted)]">
                    Personel görünmüyor
                  </span>
                ) : (
                  branch.staffMembers?.map((staff) => (
                    <span
                      className="rounded-full bg-[var(--rs-accent-soft)] px-3 py-1 text-xs text-[var(--rs-accent-strong)]"
                      key={staff.id ?? staff.displayName}
                    >
                      {staff.displayName ?? "Personel"}
                    </span>
                  ))
                )}
              </div>
            </article>
          ))
        )}
      </div>
    </Card>
  );
}

function ServiceSnapshotCard({
  services
}: {
  services: NonNullable<PublicBusinessProfile["services"]>;
}) {
  return (
    <Card className="p-5">
      <CardHeader>
        <CardTitle>Hizmet menüsü snapshot</CardTitle>
        <CardDescription>
          Public profilde görünen hizmet/varyant fiyat ve süre bilgisi.
        </CardDescription>
      </CardHeader>

      <div className="mt-5 grid gap-3">
        {services.length === 0 ? (
          <EmptyState text="Public profilde hizmet menüsü yok." />
        ) : (
          services.map((service) => (
            <article
              className="rounded-[1.5rem] border border-[var(--rs-border)] bg-white/74 p-4"
              key={service.id ?? service.name}
            >
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <h2 className="text-xl font-semibold tracking-[-0.04em] text-[var(--rs-ink)]">
                    {service.name ?? "Hizmet"}
                  </h2>
                  <p className="mt-1 text-sm text-[var(--rs-muted)]">
                    {service.categoryKey ?? "Kategori yok"}
                  </p>
                </div>
                <span className="rounded-full bg-[var(--rs-neutral-soft)] px-3 py-1 text-xs text-[var(--rs-muted)]">
                  {service.variants?.length ?? 0} varyant
                </span>
              </div>

              <div className="mt-4 grid gap-2">
                {(service.variants ?? []).length === 0 ? (
                  <EmptyState text="Bu hizmet için varyant görünmüyor." />
                ) : (
                  service.variants?.map((variant) => (
                    <div
                      className="flex flex-wrap items-center justify-between gap-2 rounded-2xl bg-[var(--rs-surface-muted)] px-3 py-2 text-sm"
                      key={variant.id ?? variant.name}
                    >
                      <span className="font-medium text-[var(--rs-ink)]">
                        {variant.name ?? "Varyant"}
                      </span>
                      <span className="text-[var(--rs-muted)]">
                        {variant.durationMinutes ?? 0} dk ·{" "}
                        {formatMoney(variant.priceAmount, variant.currencyCode)}
                      </span>
                    </div>
                  ))
                )}
              </div>
            </article>
          ))
        )}
      </div>
    </Card>
  );
}

function InfoLine({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-[var(--rs-border)] bg-white p-4">
      <p className="text-xs text-[var(--rs-muted)]">{label}</p>
      <p className="mt-2 break-all font-medium text-[var(--rs-ink)]">{value}</p>
    </div>
  );
}

function InfoBox({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-[var(--rs-border)] bg-white p-4">
      <p className="text-xs text-[var(--rs-muted)]">{label}</p>
      <p className="mt-2 font-medium text-[var(--rs-ink)]">{value}</p>
    </div>
  );
}

function TextBlock({ label, value }: { label: string; value: string }) {
  return (
    <div className="mt-5 rounded-[1.5rem] border border-[var(--rs-border)] bg-white p-4">
      <p className="text-xs font-semibold uppercase tracking-[0.16em] text-[var(--rs-muted)]">
        {label}
      </p>
      <p className="mt-3 text-sm leading-6 text-[var(--rs-muted-strong)]">
        {value}
      </p>
    </div>
  );
}

function EmptyState({ text }: { text: string }) {
  return (
    <p className="rounded-2xl border border-dashed border-[var(--rs-border)] bg-white/60 p-4 text-sm text-[var(--rs-muted)]">
      {text}
    </p>
  );
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

function getStaffPolicyCopy(policy?: string | null) {
  if (policy === "ShowNames" || policy === "DisplayNames") {
    return "Personel isimleri gösterilir";
  }

  if (policy === "HideNames" || policy === "Anonymous") {
    return "Personel bilgisi işletme onayında netleşir";
  }

  return policy ?? "İşletme politikasına göre";
}

function getDayLabel(day?: string | null) {
  if (!day) {
    return "Gün";
  }

  return dayLabels[day] ?? day;
}

function formatTime(value?: string) {
  if (!value) {
    return "--:--";
  }

  return value.slice(0, 5);
}

function formatMoney(amount?: number, currencyCode?: string | null) {
  if (amount === undefined) {
    return "Fiyat yok";
  }

  try {
    return new Intl.NumberFormat("tr-TR", {
      currency: currencyCode ?? "TRY",
      maximumFractionDigits: 0,
      style: "currency"
    }).format(amount);
  } catch {
    return `${amount} ${currencyCode ?? "TRY"}`;
  }
}

function shortGuid(value?: string | null) {
  if (!value) {
    return "Yok";
  }

  return `${value.slice(0, 8)}...`;
}
