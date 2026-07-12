"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useMemo, useRef, useState } from "react";
import { CalendarDays, Clock, MapPin, User, Wallet } from "lucide-react";

import {
  cancelAppointment,
  cancelAppointmentRequest
} from "@/features/customer/api/cancel-booking";
import type { CustomerAppointmentHistoryItem } from "@/features/customer/api/get-appointment-history";
import {
  formatTotalPrice,
  getCancelKind,
  getItemKey,
  getServiceSummary,
  getStatusPresentation,
  getTotalDurationMinutes,
  partitionAppointments,
  type AppointmentTab,
  type StatusTone
} from "@/features/customer/lib/appointment-view";
// TTL geri sayimi zaten cozulmus bir problem -- kopyalamiyoruz, dogrudan kullaniyoruz.
import { getRequestTtlStatus } from "@/features/business/lib/request-ttl";
import { Alert, AlertDescription } from "@/components/ui/alert";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle
} from "@/components/ui/alert-dialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle
} from "@/components/ui/dialog";
import { Separator } from "@/components/ui/separator";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle
} from "@/components/ui/sheet";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { routes } from "@/shared/config/routes";
import { useIsMobile } from "@/shared/hooks/use-mobile";
import { formatBranchDateTime } from "@/shared/lib/date-time";
import {
  clearIntentIdempotencyKey,
  getOrCreateIntentIdempotencyKey,
  type IdempotencyKeyCache
} from "@/shared/lib/idempotency";
import { cn } from "@/shared/lib/cn";

type CustomerAppointmentsPageProps = {
  items: CustomerAppointmentHistoryItem[];
  /** Sunucudan gelir. SSR ile ilk client render'inin AYNI olmasi icin (hydration). */
  nowUtc: string;
};

export function CustomerAppointmentsPage({
  items,
  nowUtc
}: CustomerAppointmentsPageProps) {
  const router = useRouter();
  const isMobile = useIsMobile();

  const [tab, setTab] = useState<AppointmentTab>("upcoming");
  const [now, setNow] = useState(nowUtc);
  const [detailItem, setDetailItem] =
    useState<CustomerAppointmentHistoryItem | null>(null);
  const [cancelItem, setCancelItem] =
    useState<CustomerAppointmentHistoryItem | null>(null);
  const [cancellingKey, setCancellingKey] = useState<string | null>(null);
  // Iptal hatalari KARTTA kalir. Toast kaybolur; "neden iptal edemedim" sorusu kalmaz.
  const [cancelErrors, setCancelErrors] = useState<Record<string, string>>({});
  const [statusOverrides, setStatusOverrides] = useState<Record<string, string>>(
    {}
  );

  const idempotencyKeys = useRef<IdempotencyKeyCache>({});

  // TTL geri sayimi canli aksin diye saati ilerletiyoruz. Mount'tan SONRA basliyor:
  // ilk client render'i sunucununkiyle birebir ayni kalsin (hydration uyusmazligi yok).
  useEffect(() => {
    const timer = window.setInterval(() => {
      setNow(new Date().toISOString());
    }, 30_000);

    return () => window.clearInterval(timer);
  }, []);

  const resolvedItems = useMemo(() => {
    return items.map((item) => {
      const key = getItemKey(item);
      const override = key ? statusOverrides[key] : undefined;

      return override ? { ...item, status: override } : item;
    });
  }, [items, statusOverrides]);

  const { past, upcoming } = useMemo(
    () => partitionAppointments(resolvedItems, now),
    [now, resolvedItems]
  );

  async function confirmCancel() {
    if (!cancelItem) {
      return;
    }

    const key = getItemKey(cancelItem);
    const kind = getCancelKind(cancelItem);
    const businessSlug = cancelItem.businessSlug;

    if (!key || !kind || !businessSlug) {
      return;
    }

    setCancellingKey(key);
    setCancelErrors((current) => {
      const next = { ...current };
      delete next[key];
      return next;
    });

    // Ayni iptal niyeti = ayni anahtar. Retry'da yeni anahtar uretmeyiz.
    const idempotencyKey = getOrCreateIntentIdempotencyKey(
      idempotencyKeys.current,
      key,
      "customer-cancel"
    );

    const result =
      kind === "request"
        ? await cancelAppointmentRequest(
            businessSlug,
            cancelItem.appointmentRequestId as string,
            idempotencyKey
          )
        : await cancelAppointment(
            businessSlug,
            cancelItem.appointmentId as string,
            idempotencyKey
          );

    setCancellingKey(null);

    if (result.kind === "error") {
      // Onay penceresi ACIK KALIR: hatayi musteri kararini verdigi yerde gorur.
      setCancelErrors((current) => ({ ...current, [key]: result.message }));
      return;
    }

    setStatusOverrides((current) => ({ ...current, [key]: result.status }));
    clearIntentIdempotencyKey(idempotencyKeys.current, key);
    setCancelItem(null);
    setDetailItem(null);
    router.refresh();
  }

  const cancelKey = cancelItem ? getItemKey(cancelItem) : null;
  const cancelError = cancelKey ? cancelErrors[cancelKey] : undefined;
  const isCancelling = !!cancelKey && cancellingKey === cancelKey;
  const cancelKind = cancelItem ? getCancelKind(cancelItem) : null;

  return (
    <div className="space-y-6">
      <header className="space-y-2">
        <h1 className="text-3xl font-semibold tracking-tight sm:text-4xl">
          Randevularım
        </h1>
        <p className="text-sm text-muted-foreground sm:text-base">
          Aldığınız randevular ve onay bekleyen istekleriniz burada. Saatler
          salonun bulunduğu saat dilimine göre gösterilir.
        </p>
      </header>

      <Tabs
        onValueChange={(value) => setTab(value as AppointmentTab)}
        value={tab}
      >
        {/* Telefon birincil cihaz: sekmeler kaydirmada ustte yapisik kalir. */}
        <div className="sticky top-0 z-20 -mx-4 bg-background/95 px-4 py-2 backdrop-blur supports-[backdrop-filter]:bg-background/80 sm:mx-0 sm:px-0">
          <TabsList className="w-full">
            <TabsTrigger className="min-h-11 flex-1" value="upcoming">
              Yaklaşan ({upcoming.length})
            </TabsTrigger>
            <TabsTrigger className="min-h-11 flex-1" value="past">
              Geçmiş ({past.length})
            </TabsTrigger>
          </TabsList>
        </div>

        <TabsContent className="mt-4 space-y-3" value="upcoming">
          {upcoming.length === 0 ? (
            <EmptyTab
              description="Yeni bir randevu almak için salonları keşfedebilirsiniz."
              title="Yaklaşan randevunuz yok"
            />
          ) : (
            upcoming.map((item, index) => (
              <AppointmentCard
                cancelError={getError(cancelErrors, item)}
                item={item}
                key={getItemKey(item) ?? index}
                nowUtc={now}
                onCancel={() => setCancelItem(item)}
                onDetail={() => setDetailItem(item)}
              />
            ))
          )}
        </TabsContent>

        <TabsContent className="mt-4 space-y-3" value="past">
          {past.length === 0 ? (
            <EmptyTab
              description="Tamamlanan veya iptal edilen randevularınız burada listelenir."
              title="Geçmiş kaydınız yok"
            />
          ) : (
            past.map((item, index) => (
              <AppointmentCard
                cancelError={getError(cancelErrors, item)}
                item={item}
                key={getItemKey(item) ?? index}
                nowUtc={now}
                onCancel={() => setCancelItem(item)}
                onDetail={() => setDetailItem(item)}
              />
            ))
          )}
        </TabsContent>
      </Tabs>

      {/* Mobilde Sheet (basparmaga yakin), masaustunde Dialog. */}
      {isMobile ? (
        <Sheet
          onOpenChange={(open) => !open && setDetailItem(null)}
          open={!!detailItem}
        >
          <SheetContent className="max-h-[90vh] overflow-y-auto" side="bottom">
            <SheetHeader>
              <SheetTitle>
                {detailItem ? getServiceSummary(detailItem) : "Randevu"}
              </SheetTitle>
              <SheetDescription>
                {detailItem?.businessDisplayName ?? "Randevu detayı"}
              </SheetDescription>
            </SheetHeader>
            {detailItem ? (
              <DetailBody
                item={detailItem}
                nowUtc={now}
                onCancel={() => setCancelItem(detailItem)}
              />
            ) : null}
          </SheetContent>
        </Sheet>
      ) : (
        <Dialog
          onOpenChange={(open) => !open && setDetailItem(null)}
          open={!!detailItem}
        >
          <DialogContent>
            <DialogHeader>
              <DialogTitle>
                {detailItem ? getServiceSummary(detailItem) : "Randevu"}
              </DialogTitle>
              <DialogDescription>
                {detailItem?.businessDisplayName ?? "Randevu detayı"}
              </DialogDescription>
            </DialogHeader>
            {detailItem ? (
              <DetailBody
                item={detailItem}
                nowUtc={now}
                onCancel={() => setCancelItem(detailItem)}
              />
            ) : null}
          </DialogContent>
        </Dialog>
      )}

      {/* Yikici aksiyon -> her zaman onay. */}
      <AlertDialog
        onOpenChange={(open) => {
          if (!open && !isCancelling) {
            setCancelItem(null);
          }
        }}
        open={!!cancelItem}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {cancelKind === "request"
                ? "Randevu isteğiniz iptal edilsin mi?"
                : "Randevunuz iptal edilsin mi?"}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {cancelItem ? getCancelPrompt(cancelItem) : ""}
            </AlertDialogDescription>
          </AlertDialogHeader>

          {cancelError ? (
            <Alert variant="destructive">
              <AlertDescription>{cancelError}</AlertDescription>
            </Alert>
          ) : null}

          <AlertDialogFooter>
            <AlertDialogCancel className="min-h-11" disabled={isCancelling}>
              Vazgeç
            </AlertDialogCancel>
            <AlertDialogAction
              className="min-h-11 bg-destructive text-white hover:bg-destructive/90"
              disabled={isCancelling}
              onClick={(event) => {
                // Hata durumunda pencere ACIK kalmali; Radix varsayilani kapatmak.
                event.preventDefault();
                void confirmCancel();
              }}
            >
              {isCancelling ? "İptal ediliyor..." : "Evet, iptal et"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}

function getError(
  errors: Record<string, string>,
  item: CustomerAppointmentHistoryItem
) {
  const key = getItemKey(item);
  return key ? errors[key] : undefined;
}

function getCancelPrompt(item: CustomerAppointmentHistoryItem) {
  const when = formatWhen(item);
  const service = getServiceSummary(item);
  const business = item.businessDisplayName ?? "salon";

  return `${business} · ${service} · ${when}. Bu işlem geri alınamaz; yeniden randevu almanız gerekir.`;
}

function formatWhen(item: CustomerAppointmentHistoryItem) {
  if (!item.startUtc || !item.branchTimeZoneId) {
    return "Zaman bilgisi yok";
  }

  // Tarayici saat dilimine SESSIZCE cevirmiyoruz: randevu salonun saatiyle yasar.
  return formatBranchDateTime(item.startUtc, item.branchTimeZoneId);
}

function AppointmentCard({
  cancelError,
  item,
  nowUtc,
  onCancel,
  onDetail
}: {
  cancelError?: string;
  item: CustomerAppointmentHistoryItem;
  nowUtc: string;
  onCancel: () => void;
  onDetail: () => void;
}) {
  const presentation = getStatusPresentation(item.status);
  const cancelKind = getCancelKind(item);
  const ttl = item.status === "PendingApproval"
    ? getRequestTtlStatus(item.expiresAtUtc, nowUtc)
    : null;

  return (
    <Card className="gap-0 p-4">
      <div className="flex flex-wrap items-center gap-2">
        <StatusBadge presentation={presentation} />
        <span className="text-sm font-medium">
          {item.businessDisplayName ?? item.businessSlug ?? "Salon"}
        </span>
      </div>

      <h2 className="mt-3 text-lg font-semibold tracking-tight">
        {getServiceSummary(item)}
      </h2>

      <p className="mt-1 flex items-center gap-1.5 text-sm text-muted-foreground">
        <CalendarDays aria-hidden className="size-4" />
        {formatWhen(item)}
      </p>

      {item.branchDisplayName ? (
        <p className="mt-1 flex items-center gap-1.5 text-sm text-muted-foreground">
          <MapPin aria-hidden className="size-4" />
          {item.branchDisplayName}
        </p>
      ) : null}

      {ttl ? (
        // TTL bir TOOLTIP degil, GORUNUR bir satir: dokunmatikte tooltip yoktur.
        <p
          className={cn(
            "mt-3 rounded-md px-3 py-2 text-sm",
            ttl.level === "expired"
              ? "bg-muted text-muted-foreground"
              : ttl.level === "critical"
                ? "bg-destructive/10 text-destructive"
                : "bg-amber-500/10 text-amber-700 dark:text-amber-400"
          )}
        >
          {ttl.level === "expired"
            ? "Salon yanıt vermedi, isteğin süresi doldu."
            : `Salon yanıt vermezse düşer — onay için ${ttl.label}.`}
        </p>
      ) : null}

      {cancelError ? (
        <Alert className="mt-3" variant="destructive">
          <AlertDescription>{cancelError}</AlertDescription>
        </Alert>
      ) : null}

      <div className="mt-4 flex flex-col gap-2 sm:flex-row">
        <Button
          className="min-h-11 flex-1"
          onClick={onDetail}
          type="button"
          variant="outline"
        >
          Detay
        </Button>
        {cancelKind ? (
          <Button
            className="min-h-11 flex-1"
            onClick={onCancel}
            type="button"
            variant="destructive"
          >
            İptal et
          </Button>
        ) : null}
      </div>
    </Card>
  );
}

function StatusBadge({ presentation }: { presentation: ReturnType<typeof getStatusPresentation> }) {
  // Renk TEK sinyal DEGIL: rozet her zaman Turkce METNI tasir.
  return (
    <Badge className={cn("min-h-6", toneClass(presentation.tone))} variant="outline">
      {presentation.label}
    </Badge>
  );
}

function toneClass(tone: StatusTone) {
  if (tone === "pending") {
    return "border-amber-500/40 bg-amber-500/10 text-amber-700 dark:text-amber-400";
  }

  if (tone === "confirmed") {
    return "border-emerald-500/40 bg-emerald-500/10 text-emerald-700 dark:text-emerald-400";
  }

  if (tone === "negative") {
    return "border-destructive/40 bg-destructive/10 text-destructive";
  }

  return "border-border bg-muted text-muted-foreground";
}

function DetailBody({
  item,
  nowUtc,
  onCancel
}: {
  item: CustomerAppointmentHistoryItem;
  nowUtc: string;
  onCancel: () => void;
}) {
  const presentation = getStatusPresentation(item.status);
  const cancelKind = getCancelKind(item);
  const ttl = item.status === "PendingApproval"
    ? getRequestTtlStatus(item.expiresAtUtc, nowUtc)
    : null;
  const lines = item.lines ?? [];

  return (
    <div className="space-y-4 px-4 pb-4 sm:px-0 sm:pb-0">
      <div className="flex flex-wrap items-center gap-2">
        <StatusBadge presentation={presentation} />
        {ttl && ttl.level !== "expired" ? (
          <span className="text-sm text-muted-foreground">
            Onay için {ttl.label}
          </span>
        ) : null}
      </div>

      <Separator />

      <dl className="space-y-3 text-sm">
        <DetailRow
          icon={<CalendarDays aria-hidden className="size-4" />}
          label="Tarih ve saat"
          value={formatWhen(item)}
        />
        <DetailRow
          icon={<MapPin aria-hidden className="size-4" />}
          label="Şube"
          value={item.branchDisplayName ?? "Belirtilmedi"}
        />
        <DetailRow
          icon={<User aria-hidden className="size-4" />}
          label="Personel"
          value={item.staffMemberDisplayName ?? "Salon atayacak"}
        />
        <DetailRow
          icon={<Clock aria-hidden className="size-4" />}
          label="Süre"
          value={`${getTotalDurationMinutes(item)} dk`}
        />
        <DetailRow
          icon={<Wallet aria-hidden className="size-4" />}
          label="Toplam"
          value={formatTotalPrice(item)}
        />
      </dl>

      {lines.length > 0 ? (
        <>
          <Separator />
          <div>
            <p className="mb-2 text-sm font-medium">Hizmetler</p>
            <ul className="space-y-1.5">
              {lines.map((line, index) => (
                <li
                  className="flex justify-between gap-4 text-sm text-muted-foreground"
                  key={`${line.serviceVariantId ?? index}`}
                >
                  <span>{line.serviceNameSnapshot ?? "Hizmet"}</span>
                  <span>{line.durationMinutes ?? 0} dk</span>
                </li>
              ))}
            </ul>
          </div>
        </>
      ) : null}

      <div className="flex flex-col gap-2 pt-2">
        {item.businessSlug ? (
          <Button asChild className="min-h-11" variant="outline">
            <Link href={routes.public.businessProfile(item.businessSlug)}>
              Salon sayfasına git
            </Link>
          </Button>
        ) : null}
        {cancelKind ? (
          <Button
            className="min-h-11"
            onClick={onCancel}
            type="button"
            variant="destructive"
          >
            İptal et
          </Button>
        ) : null}
      </div>
    </div>
  );
}

function DetailRow({
  icon,
  label,
  value
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
}) {
  return (
    <div className="flex items-start justify-between gap-4">
      <dt className="flex items-center gap-1.5 text-muted-foreground">
        {icon}
        {label}
      </dt>
      <dd className="text-right font-medium">{value}</dd>
    </div>
  );
}

function EmptyTab({
  description,
  title
}: {
  description: string;
  title: string;
}) {
  return (
    <Card className="items-center gap-2 border-dashed p-10 text-center">
      <p className="font-medium">{title}</p>
      <p className="text-sm text-muted-foreground">{description}</p>
      <Button asChild className="mt-4 min-h-11">
        <Link href={routes.public.discover}>Salonları keşfet</Link>
      </Button>
    </Card>
  );
}
