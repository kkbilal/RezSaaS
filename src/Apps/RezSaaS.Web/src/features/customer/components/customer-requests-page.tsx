"use client";

import Link from "next/link";
import { useMemo, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import type { CustomerAppointmentHistoryItem } from "@/features/customer/api/get-appointment-history";
import { apiClient } from "@/shared/api/client";
import { routes } from "@/shared/config/routes";
import { formatBranchDateTime } from "@/shared/lib/date-time";
import {
  clearIntentIdempotencyKey,
  getOrCreateIntentIdempotencyKey,
  type IdempotencyKeyCache
} from "@/shared/lib/idempotency";
import { Button } from "@/shared/ui/button";
import { Card, CardDescription, CardTitle } from "@/shared/ui/card";
import { StatusBadge } from "@/shared/ui/status-badge";

type CustomerRequestsPageProps = {
  items: CustomerAppointmentHistoryItem[];
};

type FilterValue = "all" | "PendingApproval" | "Confirmed" | "closed";

export function CustomerRequestsPage({
  items
}: CustomerRequestsPageProps) {
  const router = useRouter();
  const [filter, setFilter] = useState<FilterValue>("all");
  const [actingId, setActingId] = useState<string | null>(null);
  const [toast, setToast] = useState<string | null>(null);
  const [statusOverrides, setStatusOverrides] = useState<Record<string, string>>(
    {}
  );
  const cancelIdempotencyKeys = useRef<IdempotencyKeyCache>({});

  const visibleItems = useMemo(() => {
    return items
      .map((item) => {
        const key = getItemKey(item);
        const override = key ? statusOverrides[key] : undefined;

        return override
          ? {
              ...item,
              status: override
            }
          : item;
      })
      .filter((item) => {
        const status = item.status ?? "";

        if (filter === "all") {
          return true;
        }

        if (filter === "closed") {
          return !["PendingApproval", "Confirmed", "Approved"].includes(status);
        }

        return status === filter;
      });
  }, [filter, items, statusOverrides]);

  async function cancelRequest(item: CustomerAppointmentHistoryItem) {
    const appointmentRequestId = item.appointmentRequestId;
    const businessSlug = item.businessSlug;

    if (!appointmentRequestId || !businessSlug) {
      showToast("Bu talep iptal edilemiyor.");
      return;
    }

    setActingId(appointmentRequestId);
    const idempotencyKey = getOrCreateIntentIdempotencyKey(
      cancelIdempotencyKeys.current,
      appointmentRequestId,
      "customer-cancel"
    );

    try {
      const result = await apiClient.POST(
        "/api/public/businesses/{slug}/appointment-requests/{appointmentRequestId}/cancel",
        {
          params: {
            header: {
              "Idempotency-Key": idempotencyKey
            },
            path: {
              appointmentRequestId,
              slug: businessSlug
            }
          }
        }
      );

      if (!result.response.ok) {
        showToast(getCancelErrorCopy(result.response.status));
        return;
      }

      setStatusOverrides((current) => ({
        ...current,
        [appointmentRequestId]: result.data?.status ?? "CancelledByCustomer"
      }));
      clearIntentIdempotencyKey(
        cancelIdempotencyKeys.current,
        appointmentRequestId
      );
      showToast("Onay bekleyen talep iptal edildi.");
      router.refresh();
    } catch {
      showToast("Talep şu anda iptal edilemedi. Lütfen tekrar dene.");
    } finally {
      setActingId(null);
    }
  }

  function showToast(message: string) {
    setToast(message);
    window.setTimeout(() => setToast(null), 3200);
  }

  return (
    <div className="space-y-6">
      <div className="mx-auto max-w-7xl space-y-8">
        <section className="fade-up rounded-[2.5rem] border border-[var(--rs-border)] bg-[var(--rs-glass)] p-6 shadow-[var(--rs-shadow-card)] backdrop-blur-xl sm:p-8">
          <div className="flex flex-col gap-8 lg:flex-row lg:items-end lg:justify-between">
            <div className="max-w-4xl space-y-5">
              <h1 className="text-5xl font-semibold tracking-[-0.07em] text-[var(--rs-ink)] sm:text-7xl">
                Taleplerim ve randevularım.
              </h1>
              <p className="max-w-2xl text-lg leading-8 text-[var(--rs-muted-strong)]">
                Onay bekleyen talepler kesin randevu değildir. İşletme onayıyla
                confirmed randevuya dönüşür; yalnızca onay bekleyen talepler buradan
                iptal edilebilir.
              </p>
            </div>

            <div className="rounded-[2rem] bg-[var(--rs-accent)] p-6 text-white shadow-[var(--rs-shadow-card)]">
              <p className="text-xs uppercase tracking-[0.22em] text-white/50">
                Toplam kayıt
              </p>
              <p className="mt-6 text-5xl font-semibold tracking-[-0.07em]">
                {items.length}
              </p>
            </div>
          </div>
        </section>

        <Card className="p-4">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
            <div>
              <CardTitle>Rezervasyon geçmişi</CardTitle>
              <CardDescription>
                Saatler her kaydın şube zaman dilimine göre gösterilir.
              </CardDescription>
            </div>
            <div className="flex flex-wrap gap-2">
              {[
                ["all", "Hepsi"],
                ["PendingApproval", "Onay bekleyen"],
                ["Confirmed", "Kesinleşen"],
                ["closed", "Kapanan"]
              ].map(([value, label]) => (
                <button
                  className={
                    filter === value
                      ? "rounded-full bg-[var(--rs-accent)] px-4 py-2 text-xs font-medium text-white"
                      : "rounded-full border border-[var(--rs-border)] bg-[var(--rs-surface)] px-4 py-2 text-xs font-medium text-[var(--rs-muted)] transition hover:text-[var(--rs-ink)]"
                  }
                  key={value}
                  onClick={() => setFilter(value as FilterValue)}
                  type="button"
                >
                  {label}
                </button>
              ))}
            </div>
          </div>
        </Card>

        {visibleItems.length === 0 ? (
          <Card className="border-dashed bg-[var(--rs-glass)] p-10 text-center shadow-none">
            <CardTitle>Bu filtrede kayıt yok</CardTitle>
            <CardDescription className="mx-auto mt-2 max-w-lg">
              Yeni bir işletme keşfedip onay bekleyen rezervasyon talebi
              oluşturabilirsin.
            </CardDescription>
            <Button asChild className="mt-6">
              <Link href={routes.public.discover}>İşletme keşfet</Link>
            </Button>
          </Card>
        ) : (
          <section className="grid gap-4">
            {visibleItems.map((item, index) => (
              <CustomerHistoryCard
                index={index}
                isSubmitting={actingId === item.appointmentRequestId}
                item={item}
                key={getItemKey(item) ?? index}
                onCancel={() => void cancelRequest(item)}
              />
            ))}
          </section>
        )}
      </div>

      {toast ? (
        <div className="fixed bottom-5 left-1/2 z-50 w-[calc(100%-2rem)] max-w-xl -translate-x-1/2 rounded-full border border-[var(--rs-border)] bg-[var(--rs-surface)] px-5 py-3 text-sm text-[var(--rs-ink)] shadow-[var(--rs-shadow-card)]">
          {toast}
        </div>
      ) : null}
    </div>
  );
}

function CustomerHistoryCard({
  index,
  isSubmitting,
  item,
  onCancel
}: {
  index: number;
  isSubmitting: boolean;
  item: CustomerAppointmentHistoryItem;
  onCancel: () => void;
}) {
  const status = item.status ?? "Unknown";
  const isPending = status === "PendingApproval";

  return (
    <article
      className="fade-up rounded-[2rem] border border-[var(--rs-border)] bg-[var(--rs-glass)] p-5 shadow-[var(--rs-shadow-soft)] backdrop-blur-xl"
      style={{ animationDelay: `${index * 45}ms` }}
    >
      <div className="grid gap-5 lg:grid-cols-[1fr_auto] lg:items-start">
        <div className="space-y-5">
          <div className="flex flex-wrap items-center gap-3">
            <StatusBadge status={status} />
            <span className="rounded-full border border-[var(--rs-border)] bg-[var(--rs-surface)] px-3 py-1 text-xs text-[var(--rs-muted)]">
              {getItemTypeCopy(item.itemType)}
            </span>
            <span className="text-xs text-[var(--rs-muted)]">
              {item.businessDisplayName ?? item.businessSlug}
            </span>
          </div>

          <div>
            <h2 className="text-2xl font-semibold tracking-[-0.05em] text-[var(--rs-ink)]">
              {getLineSummary(item)}
            </h2>
            <p className="mt-1 text-sm text-[var(--rs-muted)]">
              {item.branchDisplayName ?? "Şube"} ·{" "}
              {formatItemDateTime(item.startUtc, item.branchTimeZoneId)}
            </p>
          </div>

          <div className="grid gap-3 md:grid-cols-3">
            <InfoBlock label="Personel" value={item.staffMemberDisplayName ?? "Atanacak"} />
            <InfoBlock
              label="Süre"
              value={`${getTotalDurationMinutes(item)} dk`}
            />
            <InfoBlock label="Toplam" value={formatTotalPrice(item)} />
          </div>

          {isPending ? (
            <p className="rounded-2xl border border-[var(--rs-warning-border)] bg-[var(--rs-warning-soft)] px-4 py-3 text-sm leading-6 text-[var(--rs-warning)]">
              Bu talep işletme onayı bekliyor ve kesin randevu değildir.
            </p>
          ) : null}
        </div>

        <div className="flex gap-3 lg:min-w-48 lg:flex-col">
          {item.businessSlug ? (
            <Button asChild className="flex-1 lg:w-full" variant="secondary">
              <Link href={routes.public.businessProfile(item.businessSlug)}>
                İşletme profili
              </Link>
            </Button>
          ) : null}
          {isPending ? (
            <Button
              className="flex-1 lg:w-full"
              disabled={isSubmitting}
              onClick={onCancel}
              type="button"
              variant="danger"
            >
              {isSubmitting ? "İptal ediliyor" : "Talebi iptal et"}
            </Button>
          ) : null}
        </div>
      </div>
    </article>
  );
}

function InfoBlock({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface)] p-4">
      <p className="text-xs text-[var(--rs-muted)]">{label}</p>
      <p className="mt-2 font-medium text-[var(--rs-ink)]">{value}</p>
    </div>
  );
}

function getItemKey(item: CustomerAppointmentHistoryItem) {
  return item.appointmentRequestId ?? item.appointmentId ?? null;
}

function getItemTypeCopy(itemType?: string | null) {
  if (itemType === "AppointmentRequest") {
    return "Talep";
  }

  if (itemType === "Appointment") {
    return "Randevu";
  }

  return itemType ?? "Kayıt";
}

function getLineSummary(item: CustomerAppointmentHistoryItem) {
  const lines = item.lines ?? [];
  const firstLine = lines.at(0);
  const firstService = firstLine?.serviceNameSnapshot ?? "Hizmet detayı yok";

  if (lines.length <= 1) {
    return firstService;
  }

  return `${firstService} + ${lines.length - 1} hizmet`;
}

function getTotalDurationMinutes(item: CustomerAppointmentHistoryItem) {
  return (item.lines ?? []).reduce(
    (total, line) => total + (line.durationMinutes ?? 0),
    0
  );
}

function formatTotalPrice(item: CustomerAppointmentHistoryItem) {
  const lines = item.lines ?? [];
  const amount = lines.reduce((total, line) => total + (line.priceAmount ?? 0), 0);
  const currencyCode =
    lines.find((line) => line.currencyCode)?.currencyCode ?? "TRY";

  try {
    return new Intl.NumberFormat("tr-TR", {
      currency: currencyCode,
      maximumFractionDigits: 0,
      style: "currency"
    }).format(amount);
  } catch {
    return `${amount} ${currencyCode}`;
  }
}

function formatItemDateTime(value?: string, branchTimeZoneId?: string | null) {
  if (!value || !branchTimeZoneId) {
    return "Zaman bilgisi yok";
  }

  return formatBranchDateTime(value, branchTimeZoneId);
}

function getCancelErrorCopy(status: number) {
  if (status === 401) {
    return "Oturum doğrulanamadı. Lütfen yeniden giriş yap.";
  }

  if (status === 404) {
    return "Talep bulunamadı veya bu hesapla görüntülenemiyor.";
  }

  if (status === 409) {
    return "Bu talep artık iptal edilebilir durumda değil.";
  }

  return "Talep iptal edilemedi. Lütfen tekrar dene.";
}
