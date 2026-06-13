"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import type { PublicBusinessProfile } from "@/features/public-discovery/api/public-businesses";
import { apiClient } from "@/shared/api/client";
import type { ApiSchema } from "@/shared/api/types";
import { routes, withReturnTo } from "@/shared/config/routes";
import { createWebIdempotencyKey } from "@/shared/lib/idempotency";
import { Button } from "@/shared/ui/button";
import { Card, CardDescription, CardHeader, CardTitle } from "@/shared/ui/card";

type PublicSlotSearchResponse = ApiSchema<"PublicSlotSearchResponse">;
type PublicSlot = ApiSchema<"PublicSlotResponse">;
type Branch = NonNullable<PublicBusinessProfile["branches"]>[number];
type Service = NonNullable<PublicBusinessProfile["services"]>[number];
type ServiceVariant = NonNullable<Service["variants"]>[number];

type PublicBookingPanelProps = {
  profile: PublicBusinessProfile;
};

type BookingDraft = {
  branchSlug: string;
  businessSlug: string;
  date: string;
  expiresAt: number;
  idempotencyKey: string;
  serviceVariantIds: string[];
  staffMemberId?: string;
  startUtc: string;
  version: 1;
};

type SubmitState =
  | {
      kind: "idle";
    }
  | {
      kind: "success";
      message: string;
    }
  | {
      kind: "error";
      message: string;
    };

const bookingDraftStorageKey = "rezsaas.bookingDraft.v1";
const bookingDraftTtlMs = 30 * 60 * 1000;

export function PublicBookingPanel({ profile }: PublicBookingPanelProps) {
  const businessSlug = profile.slug ?? "";
  const branches = useMemo(() => profile.branches ?? [], [profile.branches]);
  const services = useMemo(() => profile.services ?? [], [profile.services]);
  const variantOptions = useMemo(() => createVariantOptions(services), [services]);
  const [selectedBranchSlug, setSelectedBranchSlug] = useState(
    branches[0]?.slug ?? ""
  );
  const selectedBranch = useMemo(
    () => branches.find((branch) => branch.slug === selectedBranchSlug) ?? branches[0],
    [branches, selectedBranchSlug]
  );
  const [date, setDate] = useState(() =>
    getTodayInBranchTimeZone(branches[0]?.timeZoneId)
  );
  const [selectedVariantIds, setSelectedVariantIds] = useState<string[]>([]);
  const [selectedStaffId, setSelectedStaffId] = useState("");
  const [slotState, setSlotState] = useState<{
    error?: string;
    isLoading: boolean;
    result?: PublicSlotSearchResponse;
  }>({
    isLoading: false
  });
  const [selectedSlotStartUtc, setSelectedSlotStartUtc] = useState("");
  const [idempotencyKey, setIdempotencyKey] = useState("");
  const [submitState, setSubmitState] = useState<SubmitState>({ kind: "idle" });

  const selectedVariants = useMemo(
    () =>
      variantOptions.filter((option) => selectedVariantIds.includes(option.id)),
    [selectedVariantIds, variantOptions]
  );
  const selectedSlot = useMemo(
    () =>
      (slotState.result?.slots ?? []).find(
        (slot) => slot.startUtc === selectedSlotStartUtc
      ) ?? null,
    [selectedSlotStartUtc, slotState.result?.slots]
  );

  useEffect(() => {
    const draft = readBookingDraft(businessSlug);

    if (!draft) {
      return;
    }

    const restoreDraft = window.setTimeout(() => {
      setSelectedBranchSlug(draft.branchSlug);
      setDate(draft.date);
      setSelectedVariantIds(draft.serviceVariantIds);
      setSelectedStaffId(draft.staffMemberId ?? "");
      setSelectedSlotStartUtc(draft.startUtc);
      setIdempotencyKey(draft.idempotencyKey);
      setSubmitState({
        kind: "idle"
      });
    }, 0);

    return () => window.clearTimeout(restoreDraft);
  }, [businessSlug]);

  function toggleVariant(variantId: string) {
    setSelectedVariantIds((current) =>
      current.includes(variantId)
        ? current.filter((item) => item !== variantId)
        : [...current, variantId]
    );
    setSelectedSlotStartUtc("");
    setSubmitState({ kind: "idle" });
  }

  async function searchSlots() {
    if (!businessSlug || !selectedBranch?.slug || selectedVariantIds.length === 0) {
      setSlotState({
        error: "Slot aramak için şube ve en az bir hizmet seçmelisin.",
        isLoading: false
      });
      return;
    }

    setSlotState({ isLoading: true });
    setSelectedSlotStartUtc("");
    setSubmitState({ kind: "idle" });

    try {
      const { data, response } = await apiClient.GET(
        "/api/public/businesses/{slug}/slots",
        {
          params: {
            path: {
              slug: businessSlug
            },
            query: {
              branchSlug: selectedBranch.slug,
              date,
              serviceVariantIds: selectedVariantIds.join(","),
              staffMemberId: selectedStaffId || undefined
            }
          }
        }
      );

      if (!response.ok || !data) {
        setSlotState({
          error: getSlotErrorCopy(response.status),
          isLoading: false
        });
        return;
      }

      setSlotState({
        isLoading: false,
        result: data
      });
    } catch {
      setSlotState({
        error: "Uygun saatler şu anda alınamadı. Lütfen tekrar dene.",
        isLoading: false
      });
    }
  }

  async function submitAppointmentRequest() {
    if (!selectedBranch?.slug || selectedVariantIds.length === 0 || !selectedSlotStartUtc) {
      setSubmitState({
        kind: "error",
        message: "Talep göndermek için hizmet, şube ve saat seçmelisin."
      });
      return;
    }

    const requestIdempotencyKey = idempotencyKey || createIdempotencyKey();
    setIdempotencyKey(requestIdempotencyKey);
    persistBookingDraft({
      branchSlug: selectedBranch.slug,
      businessSlug,
      date,
      expiresAt: Date.now() + bookingDraftTtlMs,
      idempotencyKey: requestIdempotencyKey,
      serviceVariantIds: selectedVariantIds,
      staffMemberId: selectedStaffId || undefined,
      startUtc: selectedSlotStartUtc,
      version: 1
    });

    try {
      const { data, response } = await apiClient.POST(
        "/api/public/businesses/{slug}/appointment-requests",
        {
          body: {
            branchSlug: selectedBranch.slug,
            serviceVariantIds: selectedVariantIds,
            staffMemberId: selectedStaffId || null,
            startUtc: selectedSlotStartUtc
          },
          params: {
            header: {
              "Idempotency-Key": requestIdempotencyKey
            },
            path: {
              slug: businessSlug
            }
          }
        }
      );

      if (response.status === 401) {
        window.location.href = withReturnTo(
          routes.auth.login,
          `${routes.public.businessProfile(businessSlug)}#rezervasyon`
        );
        return;
      }

      if (!response.ok || !data) {
        setSubmitState({
          kind: "error",
          message: getCreateErrorCopy(response.status)
        });
        return;
      }

      clearBookingDraft();
      setSubmitState({
        kind: "success",
        message: `Talep işletme onayına gönderildi. Durum: ${getStatusCopy(data.status)}.`
      });
    } catch {
      setSubmitState({
        kind: "error",
        message: "Talep şu anda gönderilemedi. Bağlantını kontrol edip tekrar dene."
      });
    }
  }

  return (
    <Card className="p-6" id="rezervasyon">
      <CardHeader>
        <p className="w-fit rounded-full bg-[var(--rs-accent-soft)] px-4 py-2 text-sm font-medium text-[var(--rs-accent-strong)]">
          Rezervasyon başlangıcı
        </p>
        <CardTitle className="mt-4 text-4xl sm:text-5xl">
          Hizmeti seç, uygun saatleri şube saatine göre gör.
        </CardTitle>
        <CardDescription className="max-w-2xl">
          Talep gönderildiğinde randevu kesinleşmez; işletme onayı bekleyen bir
          istek oluşur.
        </CardDescription>
      </CardHeader>

      <div className="mt-7 grid gap-6 xl:grid-cols-[1fr_22rem]">
        <div className="space-y-6">
          <div className="grid gap-4 md:grid-cols-3">
            <SelectField
              label="Şube"
              onChange={(value) => {
                const branch = branches.find((item) => item.slug === value);
                setSelectedBranchSlug(value);
                setDate(getTodayInBranchTimeZone(branch?.timeZoneId));
                setSelectedSlotStartUtc("");
              }}
              value={selectedBranchSlug}
            >
              {branches.map((branch) => (
                <option key={branch.slug} value={branch.slug ?? ""}>
                  {branch.displayName ?? branch.slug}
                </option>
              ))}
            </SelectField>

            <label className="block space-y-2">
              <span className="text-sm font-medium text-[var(--rs-ink)]">Tarih</span>
              <input
                className="min-h-12 w-full rounded-2xl border border-[var(--rs-border)] bg-white px-4 text-sm text-[var(--rs-ink)] shadow-[var(--rs-shadow-soft)] outline-none transition focus:border-[var(--rs-border-strong)] focus:ring-4 focus:ring-[rgb(5_26_36_/_0.08)]"
                onChange={(event) => {
                  setDate(event.target.value);
                  setSelectedSlotStartUtc("");
                }}
                type="date"
                value={date}
              />
            </label>

            <SelectField
              label="Personel tercihi"
              onChange={(value) => {
                setSelectedStaffId(value);
                setSelectedSlotStartUtc("");
              }}
              value={selectedStaffId}
            >
              <option value="">Fark etmez</option>
              {(selectedBranch?.staffMembers ?? []).map((staff) => (
                <option key={staff.id} value={staff.id ?? ""}>
                  {staff.displayName ?? "Personel"}
                </option>
              ))}
            </SelectField>
          </div>

          <section className="space-y-3">
            <h3 className="text-xl font-semibold tracking-[-0.04em] text-[var(--rs-ink)]">
              Hizmetler
            </h3>
            {variantOptions.length === 0 ? (
              <p className="rounded-2xl border border-dashed border-[var(--rs-border)] p-4 text-sm text-[var(--rs-muted)]">
                Bu işletmede slot aranacak hizmet varyantı henüz yok.
              </p>
            ) : (
              <div className="grid gap-3 md:grid-cols-2">
                {variantOptions.map((option) => (
                  <label
                    className={
                      selectedVariantIds.includes(option.id)
                        ? "rounded-2xl border border-[var(--rs-border-strong)] bg-white p-4 shadow-[var(--rs-shadow-soft)]"
                        : "rounded-2xl border border-[var(--rs-border)] bg-white/60 p-4 transition hover:border-[var(--rs-border-strong)]"
                    }
                    key={option.id}
                  >
                    <div className="flex items-start gap-3">
                      <input
                        checked={selectedVariantIds.includes(option.id)}
                        className="mt-1 h-4 w-4 accent-[var(--rs-ink)]"
                        onChange={() => toggleVariant(option.id)}
                        type="checkbox"
                      />
                      <span>
                        <span className="block font-medium text-[var(--rs-ink)]">
                          {option.serviceName} · {option.name}
                        </span>
                        <span className="mt-1 block text-sm text-[var(--rs-muted)]">
                          {option.durationMinutes} dk ·{" "}
                          {formatMoney(option.priceAmount, option.currencyCode)}
                        </span>
                      </span>
                    </div>
                  </label>
                ))}
              </div>
            )}
          </section>

          <Button
            disabled={slotState.isLoading || selectedVariantIds.length === 0}
            onClick={() => void searchSlots()}
            type="button"
          >
            {slotState.isLoading ? "Saatler aranıyor..." : "Uygun saatleri ara"}
          </Button>

          {slotState.error ? (
            <p className="rounded-2xl border border-[var(--rs-warning-border)] bg-[var(--rs-warning-soft)] p-4 text-sm text-[var(--rs-warning)]">
              {slotState.error}
            </p>
          ) : null}

          <SlotResults
            onSelect={setSelectedSlotStartUtc}
            selectedSlotStartUtc={selectedSlotStartUtc}
            slots={slotState.result?.slots ?? []}
            timeZoneId={slotState.result?.branchTimeZoneId ?? selectedBranch?.timeZoneId}
          />
        </div>

        <aside className="space-y-4">
          <SummaryCard
            branch={selectedBranch}
            date={date}
            selectedSlot={selectedSlot}
            selectedSlotStartUtc={selectedSlotStartUtc}
            selectedVariants={selectedVariants}
          />

          {submitState.kind !== "idle" ? (
            <div
              className={
                submitState.kind === "success"
                  ? "rounded-2xl border border-[rgb(47_122_78_/_0.22)] bg-[var(--rs-success-soft)] p-4 text-sm leading-6 text-[var(--rs-success)]"
                  : "rounded-2xl border border-[var(--rs-warning-border)] bg-[var(--rs-warning-soft)] p-4 text-sm leading-6 text-[var(--rs-warning)]"
              }
            >
              <p>{submitState.message}</p>
              {submitState.kind === "success" ? (
                <Button asChild className="mt-4" variant="secondary">
                  <Link href={routes.customer.requests}>Taleplerime git</Link>
                </Button>
              ) : null}
            </div>
          ) : null}

          <Button
            className="w-full"
            disabled={!selectedSlotStartUtc || submitState.kind === "success"}
            onClick={() => void submitAppointmentRequest()}
            type="button"
          >
            Talep gönder
          </Button>
          <p className="text-xs leading-5 text-[var(--rs-muted)]">
            Giriş yapmadıysan seçimlerin kısa süreli saklanır ve tek giriş ekranından
            sonra bu profile dönebilirsin.
          </p>
        </aside>
      </div>
    </Card>
  );
}

function SelectField({
  children,
  label,
  onChange,
  value
}: {
  children: React.ReactNode;
  label: string;
  onChange: (value: string) => void;
  value: string;
}) {
  return (
    <label className="block space-y-2">
      <span className="text-sm font-medium text-[var(--rs-ink)]">{label}</span>
      <select
        className="min-h-12 w-full rounded-2xl border border-[var(--rs-border)] bg-white px-4 text-sm text-[var(--rs-ink)] shadow-[var(--rs-shadow-soft)] outline-none transition focus:border-[var(--rs-border-strong)] focus:ring-4 focus:ring-[rgb(5_26_36_/_0.08)]"
        onChange={(event) => onChange(event.target.value)}
        value={value}
      >
        {children}
      </select>
    </label>
  );
}

function SlotResults({
  onSelect,
  selectedSlotStartUtc,
  slots,
  timeZoneId
}: {
  onSelect: (startUtc: string) => void;
  selectedSlotStartUtc: string;
  slots: PublicSlot[];
  timeZoneId?: string | null;
}) {
  if (slots.length === 0) {
    return (
      <p className="rounded-2xl border border-dashed border-[var(--rs-border)] p-4 text-sm text-[var(--rs-muted)]">
        Saatleri görmek için seçimlerini tamamlayıp arama yap.
      </p>
    );
  }

  return (
    <section className="space-y-3">
      <div>
        <h3 className="text-xl font-semibold tracking-[-0.04em] text-[var(--rs-ink)]">
          Uygun saatler
        </h3>
        <p className="mt-1 text-sm text-[var(--rs-muted)]">
          Saatler şube zamanına göre gösterilir: {timeZoneId ?? "zaman dilimi yok"}.
        </p>
      </div>
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {slots.map((slot) => (
          <button
            className={
              selectedSlotStartUtc === slot.startUtc
                ? "rounded-2xl border border-[var(--rs-border-strong)] bg-[var(--rs-ink)] p-4 text-left text-white shadow-[var(--rs-shadow-card)]"
                : "rounded-2xl border border-[var(--rs-border)] bg-white/72 p-4 text-left text-[var(--rs-ink)] shadow-[var(--rs-shadow-soft)] transition hover:-translate-y-0.5 hover:shadow-[var(--rs-shadow-card)]"
            }
            key={slot.startUtc}
            onClick={() => slot.startUtc && onSelect(slot.startUtc)}
            type="button"
          >
            <span className="block text-lg font-semibold tracking-[-0.04em]">
              {formatLocalSlot(slot.localStart)} - {formatLocalSlot(slot.localEnd)}
            </span>
            <span
              className={
                selectedSlotStartUtc === slot.startUtc
                  ? "mt-2 block text-xs text-white/58"
                  : "mt-2 block text-xs text-[var(--rs-muted)]"
              }
            >
              İşletme onayı bekleyen talep oluşur.
            </span>
          </button>
        ))}
      </div>
    </section>
  );
}

function SummaryCard({
  branch,
  date,
  selectedSlot,
  selectedSlotStartUtc,
  selectedVariants
}: {
  branch?: Branch;
  date: string;
  selectedSlot: PublicSlot | null;
  selectedSlotStartUtc: string;
  selectedVariants: Array<ServiceVariant & { serviceName: string }>;
}) {
  const totalMinutes = selectedVariants.reduce(
    (total, variant) => total + (variant.durationMinutes ?? 0),
    0
  );
  const totalPrice = selectedVariants.reduce(
    (total, variant) => total + (variant.priceAmount ?? 0),
    0
  );
  const currencyCode =
    selectedVariants.find((variant) => variant.currencyCode)?.currencyCode ?? "TRY";

  return (
    <div className="rounded-[2rem] border border-[var(--rs-border)] bg-white/72 p-5 shadow-[var(--rs-shadow-soft)]">
      <p className="text-xs uppercase tracking-[0.22em] text-[var(--rs-muted)]">
        Seçim özeti
      </p>
      <div className="mt-5 space-y-4 text-sm leading-6">
        <SummaryLine label="Şube" value={branch?.displayName ?? "Şube seç"} />
        <SummaryLine label="Tarih" value={date || "Tarih seç"} />
        <SummaryLine
          label="Hizmet"
          value={
            selectedVariants.length > 0
              ? `${selectedVariants.length} hizmet · ${totalMinutes} dk`
              : "Hizmet seç"
          }
        />
        <SummaryLine
          label="Tahmini toplam"
          value={
            selectedVariants.length > 0
              ? formatMoney(totalPrice, currencyCode)
              : "Hizmet seç"
          }
        />
        <SummaryLine
          label="Saat"
          value={
            selectedSlot
              ? `${formatLocalSlot(selectedSlot.localStart)} - ${formatLocalSlot(selectedSlot.localEnd)}`
              : selectedSlotStartUtc
                ? "Kaydedilmiş seçim"
                : "Saat seç"
          }
        />
      </div>
    </div>
  );
}

function SummaryLine({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl bg-[var(--rs-surface-muted)] p-3">
      <p className="text-xs text-[var(--rs-muted)]">{label}</p>
      <p className="mt-1 font-medium text-[var(--rs-ink)]">{value}</p>
    </div>
  );
}

function createVariantOptions(services: Service[]) {
  return services.flatMap((service) =>
    (service.variants ?? [])
      .filter((variant): variant is ServiceVariant & { id: string } =>
        Boolean(variant.id)
      )
      .map((variant) => ({
        ...variant,
        serviceName: service.name ?? "Hizmet"
      }))
  );
}

function persistBookingDraft(draft: BookingDraft) {
  window.sessionStorage.setItem(bookingDraftStorageKey, JSON.stringify(draft));
}

function readBookingDraft(businessSlug: string): BookingDraft | null {
  try {
    const rawDraft = window.sessionStorage.getItem(bookingDraftStorageKey);

    if (!rawDraft) {
      return null;
    }

    const draft = JSON.parse(rawDraft) as BookingDraft;

    if (
      draft.version !== 1 ||
      draft.businessSlug !== businessSlug ||
      draft.expiresAt <= Date.now()
    ) {
      clearBookingDraft();
      return null;
    }

    return draft;
  } catch {
    clearBookingDraft();
    return null;
  }
}

function clearBookingDraft() {
  window.sessionStorage.removeItem(bookingDraftStorageKey);
}

function createIdempotencyKey() {
  return createWebIdempotencyKey("public-booking");
}

function getTodayInBranchTimeZone(timeZoneId?: string | null) {
  try {
    const parts = new Intl.DateTimeFormat("en-CA", {
      day: "2-digit",
      month: "2-digit",
      timeZone: timeZoneId ?? "Europe/Istanbul",
      year: "numeric"
    }).formatToParts(new Date());
    const year = parts.find((part) => part.type === "year")?.value;
    const month = parts.find((part) => part.type === "month")?.value;
    const day = parts.find((part) => part.type === "day")?.value;

    if (year && month && day) {
      return `${year}-${month}-${day}`;
    }
  } catch {
    // Fall through to UTC date fallback.
  }

  return new Date().toISOString().slice(0, 10);
}

function formatLocalSlot(value?: string) {
  if (!value) {
    return "--:--";
  }

  return value.slice(11, 16);
}

function formatMoney(amount?: number, currencyCode?: string | null) {
  if (amount === undefined) {
    return "Fiyat bilgisi yok";
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

function getSlotErrorCopy(status: number) {
  if (status === 400) {
    return "Seçimlerden biri eksik veya geçersiz. Hizmet, şube ve tarihi kontrol et.";
  }

  if (status === 404) {
    return "Bu işletme veya şube için uygun saat bulunamadı.";
  }

  if (status === 429) {
    return "Çok hızlı arama yapıldı. Lütfen kısa süre sonra tekrar dene.";
  }

  return "Uygun saatler şu anda alınamadı. Lütfen tekrar dene.";
}

function getCreateErrorCopy(status: number) {
  if (status === 403) {
    return "Bu hesap şu anda yeni rezervasyon talebi gönderemiyor.";
  }

  if (status === 404) {
    return "İşletme, şube veya hizmet bilgisi artık bulunamıyor.";
  }

  if (status === 409) {
    return "Seçilen saat artık aynı şekilde uygun görünmüyor. Saatleri yeniden ara.";
  }

  if (status === 422) {
    return "Bu saate artık talep oluşturulamıyor. Daha ileri bir saat seç.";
  }

  if (status === 429) {
    return "Çok fazla talep denendi. Lütfen kısa süre sonra tekrar dene.";
  }

  return "Talep şu anda gönderilemedi. Lütfen tekrar dene.";
}

function getStatusCopy(status?: string | null) {
  if (status === "PendingApproval") {
    return "işletme onayı bekliyor";
  }

  return status ?? "işletme onayı bekliyor";
}
