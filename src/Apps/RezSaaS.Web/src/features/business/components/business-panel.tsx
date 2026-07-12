import Link from "next/link";
import type {
  BusinessAppointment,
  BusinessAppointmentScheduleState
} from "@/features/business/api/get-business-appointments";
import type { BusinessAppointmentInboxState } from "@/features/business/api/get-appointment-inbox";
import type {
  BusinessContextState,
  BusinessTenantContext
} from "@/features/business/api/get-business-context";
import { isRequestUrgent } from "@/features/business/lib/request-ttl";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle
} from "@/components/ui/card";
import { routes, withTenant } from "@/shared/config/routes";
import {
  formatBranchDateTime,
  getBranchTimeParts
} from "@/shared/lib/date-time";

/*
 * /panel = "Bugun" OZETI.
 *
 * Talep kutusu buradan SOKULDU; tek evi /panel/talepler (business-request-inbox-page.tsx).
 * Ayni veriyi iki yerde gostermek, iki yerde karar aldirir: isletme /panel'de onaylar,
 * /panel/talepler'de bayat liste gorur. Ozet sayar ve YONLENDIRIR, karar aldirmaz.
 */

const cancelledAppointmentStatuses = new Set([
  "Cancelled",
  "CancelledByAppeal",
  "CancelledByBusiness",
  "CancelledByCustomer",
  "Rebooked"
]);

const upcomingAppointmentLimit = 5;

type BusinessPanelProps = {
  appointmentSchedule: BusinessAppointmentScheduleState;
  context: BusinessContextState;
  inbox: BusinessAppointmentInboxState;
};

export function BusinessPanel({
  appointmentSchedule,
  context,
  inbox
}: BusinessPanelProps) {
  const tenant = getPanelTenant(context, inbox);
  const tenantId = tenant?.tenantId ?? null;
  const nowUtc = new Date().toISOString();

  const requests = inbox.kind === "ready" ? inbox.requests : [];
  const pendingRequests = requests.filter(
    (request) => (request.status ?? "Unknown") === "PendingApproval"
  );
  const urgentRequests = pendingRequests.filter((request) =>
    isRequestUrgent(request.expiresAtUtc, nowUtc)
  );

  const appointments = appointmentSchedule.appointments.filter(
    (appointment) =>
      !cancelledAppointmentStatuses.has(appointment.status ?? "Unknown")
  );
  const todayAppointments = appointments.filter((appointment) =>
    isSameBranchDay(appointment, nowUtc)
  );
  const upcomingAppointments = getUpcomingAppointments(appointments, nowUtc);

  const requestsHref = withTenant(routes.business.requests, tenantId);
  const calendarHref = withTenant(routes.business.calendar, tenantId);

  return (
    <div className="space-y-6">
      <header className="space-y-2">
        <h1 className="text-3xl font-semibold tracking-tight text-foreground sm:text-4xl">
          Bugün
        </h1>
        <p className="text-sm text-muted-foreground">
          {tenant?.tenantDisplayName ??
            tenant?.tenantSlug ??
            "İşletme bilgisi bekleniyor"}
        </p>
      </header>

      <div className="grid gap-4 sm:grid-cols-3">
        <SummaryTile
          description="İptal edilmeyen randevular, şube saatine göre."
          label="Bugünün randevusu"
          value={todayAppointments.length}
        />
        <SummaryTile
          description="Senin kararını bekliyor."
          href={requestsHref}
          label="Bekleyen talep"
          linkLabel="Talep kutusunu aç"
          tone={pendingRequests.length > 0 ? "attention" : "default"}
          value={pendingRequests.length}
        />
        <SummaryTile
          description="15 dakikadan az süresi kalan talepler; süre dolunca otomatik düşer."
          href={urgentRequests.length > 0 ? requestsHref : undefined}
          label="Süresi bitmek üzere"
          linkLabel="Hemen karar ver"
          tone={urgentRequests.length > 0 ? "critical" : "default"}
          value={urgentRequests.length}
        />
      </div>

      {inbox.kind === "unavailable" ? (
        <Card className="border-amber-200 bg-amber-50 dark:border-amber-900 dark:bg-amber-950">
          <CardHeader>
            <CardTitle className="text-base text-amber-900 dark:text-amber-200">
              Talep sayısı okunamadı
            </CardTitle>
            <CardDescription className="text-amber-800 dark:text-amber-300">
              {inbox.reason} Talep kutusu yine de açılabilir.
            </CardDescription>
          </CardHeader>
        </Card>
      ) : null}

      <div className="grid gap-6 xl:grid-cols-[1fr_22rem]">
        <Card>
          <CardHeader>
            <CardTitle>Yaklaşan randevular</CardTitle>
            <CardDescription>
              Kesinleşmiş randevular. Saatler şubenin kendi saat dilimiyle
              gösterilir.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            {appointmentSchedule.kind === "unavailable" ? (
              <p className="rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-sm leading-6 text-amber-900 dark:border-amber-900 dark:bg-amber-950 dark:text-amber-200">
                {appointmentSchedule.reason}
              </p>
            ) : upcomingAppointments.length === 0 ? (
              <p className="rounded-md border border-dashed border-border px-3 py-6 text-center text-sm text-muted-foreground">
                Yaklaşan randevu yok.
              </p>
            ) : (
              upcomingAppointments.map((appointment) => (
                <UpcomingAppointmentRow
                  appointment={appointment}
                  key={
                    appointment.appointmentId ??
                    `${appointment.branchId}-${appointment.startUtc}`
                  }
                />
              ))
            )}

            <Button
              asChild
              className="min-h-11 w-full sm:w-auto"
              variant="outline"
            >
              <Link href={calendarHref}>Takvimi aç</Link>
            </Button>
          </CardContent>
        </Card>

        <TenantContextCard context={context} tenant={tenant} />
      </div>
    </div>
  );
}

function SummaryTile({
  description,
  href,
  label,
  linkLabel,
  tone = "default",
  value
}: {
  description: string;
  href?: string;
  label: string;
  linkLabel?: string;
  tone?: "attention" | "critical" | "default";
  value: number;
}) {
  return (
    <Card>
      <CardHeader className="gap-2">
        <div className="flex items-center justify-between gap-2">
          <CardDescription>{label}</CardDescription>
          {/* Renk TEK sinyal degil: rozette sayinin yaninda durum metni de yazar. */}
          {tone === "critical" && value > 0 ? (
            <Badge className="border border-rose-300 bg-rose-50 text-rose-800 dark:border-rose-800 dark:bg-rose-950 dark:text-rose-200" variant="outline">
              Acil
            </Badge>
          ) : null}
          {tone === "attention" && value > 0 ? (
            <Badge className="border border-amber-300 bg-amber-50 text-amber-900 dark:border-amber-800 dark:bg-amber-950 dark:text-amber-200" variant="outline">
              Karar bekliyor
            </Badge>
          ) : null}
        </div>
        <CardTitle className="text-4xl tabular-nums">{value}</CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        <p className="text-sm leading-6 text-muted-foreground">{description}</p>
        {href && linkLabel ? (
          <Button asChild className="min-h-11 w-full" variant="outline">
            <Link href={href}>{linkLabel}</Link>
          </Button>
        ) : null}
      </CardContent>
    </Card>
  );
}

function UpcomingAppointmentRow({
  appointment
}: {
  appointment: BusinessAppointment;
}) {
  return (
    <div className="flex flex-col gap-1 rounded-md border border-border p-3 sm:flex-row sm:items-center sm:justify-between">
      <div className="min-w-0">
        <p className="truncate font-medium text-foreground">
          {getAppointmentServiceSummary(appointment)}
        </p>
        <p className="truncate text-sm text-muted-foreground">
          {appointment.staffMemberDisplayName ?? "Personel belirtilmemiş"} ·{" "}
          {appointment.resourceDisplayName ?? "Koltuk belirtilmemiş"}
        </p>
      </div>
      <p className="shrink-0 text-sm font-medium text-foreground sm:text-right">
        {formatAppointmentStart(appointment)}
      </p>
    </div>
  );
}

function TenantContextCard({
  context,
  tenant
}: {
  context: BusinessContextState;
  tenant: BusinessTenantContext | null;
}) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>İşletme bağlamı</CardTitle>
        <CardDescription>
          Panel yalnızca hesabına bağlı işletme ve şube yetkileriyle açılır.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-3">
        <div className="rounded-md border border-border bg-muted/60 p-3">
          <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
            Aktif işletme
          </p>
          <p className="mt-1 font-medium text-foreground">
            {tenant?.tenantDisplayName ??
              tenant?.tenantSlug ??
              "İşletme bilgisi bekleniyor"}
          </p>
        </div>

        <div className="grid grid-cols-2 gap-3">
          <div className="rounded-md border border-border p-3">
            <p className="text-xs text-muted-foreground">Rol</p>
            <p className="mt-1 text-sm font-medium text-foreground">
              {getRoleLabel(tenant?.role)}
            </p>
          </div>
          <div className="rounded-md border border-border p-3">
            <p className="text-xs text-muted-foreground">Kapsam</p>
            <p className="mt-1 text-sm font-medium text-foreground">
              {tenant?.isTenantWide
                ? "Tüm işletme"
                : tenant?.branchId
                  ? "Şube"
                  : "Yok"}
            </p>
          </div>
        </div>

        {context.kind !== "ready" ? (
          <p className="rounded-md border border-amber-200 bg-amber-50 p-3 text-sm leading-6 text-amber-900 dark:border-amber-900 dark:bg-amber-950 dark:text-amber-200">
            {contextStatusCopy(context)}. İşletme bilgisi doğrulanmadan operasyon
            aksiyonu açılmaz.
          </p>
        ) : null}
      </CardContent>
    </Card>
  );
}

function contextStatusCopy(context: BusinessContextState) {
  if (context.kind === "ready" && context.tenants.length > 0) {
    return "Yetkili işletme hesabı";
  }

  if (context.kind === "ready") {
    return "İşletme yetkisi görünmüyor";
  }

  if (context.kind === "unauthenticated") {
    return "Giriş bekleniyor";
  }

  return context.reason;
}

function getPanelTenant(
  context: BusinessContextState,
  inbox: BusinessAppointmentInboxState
): BusinessTenantContext | null {
  if (inbox.kind === "ready") {
    return inbox.tenant;
  }

  return context.kind === "ready" ? context.tenants[0] ?? null : null;
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

/**
 * "Bugun" SUBENIN gununu ifade eder, sunucunun ya da tarayicinin gununu degil.
 * Salon 23:00'te kapanip 09:00'da acilir; gun sinirini yanlis timezone'da cizersek
 * sabahin randevusu "dun"de kalir.
 */
function isSameBranchDay(appointment: BusinessAppointment, nowUtc: string) {
  const timeZoneId = appointment.branchTimeZoneId;

  if (!appointment.startUtc || !timeZoneId) {
    return false;
  }

  const appointmentParts = getBranchTimeParts(appointment.startUtc, timeZoneId);
  const nowParts = getBranchTimeParts(nowUtc, timeZoneId);

  if (!appointmentParts || !nowParts) {
    return false;
  }

  return (
    appointmentParts.year === nowParts.year &&
    appointmentParts.month === nowParts.month &&
    appointmentParts.day === nowParts.day
  );
}

function getUpcomingAppointments(
  appointments: ReadonlyArray<BusinessAppointment>,
  nowUtc: string
) {
  const nowMs = Date.parse(nowUtc);

  return appointments
    .filter((appointment) => {
      if (!appointment.startUtc) {
        return false;
      }

      const startMs = Date.parse(appointment.startUtc);

      return !Number.isNaN(startMs) && startMs >= nowMs;
    })
    .sort(
      (left, right) =>
        Date.parse(left.startUtc ?? "") - Date.parse(right.startUtc ?? "")
    )
    .slice(0, upcomingAppointmentLimit);
}

function getAppointmentServiceSummary(appointment: BusinessAppointment) {
  const lines = appointment.lines ?? [];
  const firstService = lines.at(0)?.serviceNameSnapshot ?? "Hizmet detayı yok";

  if (lines.length <= 1) {
    return firstService;
  }

  return `${firstService} + ${lines.length - 1} hizmet`;
}

function formatAppointmentStart(appointment: BusinessAppointment) {
  if (!appointment.startUtc) {
    return "Zaman bilgisi yok";
  }

  if (!appointment.branchTimeZoneId) {
    return `${appointment.startUtc} UTC`;
  }

  return formatBranchDateTime(
    appointment.startUtc,
    appointment.branchTimeZoneId
  );
}
