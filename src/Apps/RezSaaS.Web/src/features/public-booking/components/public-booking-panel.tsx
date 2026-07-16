"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { Check, Clock, Info } from "lucide-react";
import {
  bookingDraftStorageKey,
  createBookingDraft,
  parseBookingDraft,
  shouldRecoverSlotSelection,
  type BookingDraft
} from "@/features/public-booking/lib/booking-draft";
import type { PublicBusinessProfile } from "@/features/public-discovery/api/public-businesses";
import { showStaffNames } from "@/features/public-discovery/lib/staff-display";
import { apiClient } from "@/shared/api/client";
import type { ApiSchema } from "@/shared/api/types";
import { routes, withReturnTo } from "@/shared/config/routes";
import { createWebIdempotencyKey } from "@/shared/lib/idempotency";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle
} from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue
} from "@/components/ui/select";
import { Separator } from "@/components/ui/separator";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/shared/lib/cn";

type PublicSlotSearchResponse = ApiSchema<"PublicSlotSearchResponse">;
type PublicSlot = ApiSchema<"PublicSlotResponse">;
type Branch = NonNullable<PublicBusinessProfile["branches"]>[number];
type Service = NonNullable<PublicBusinessProfile["services"]>[number];
type ServiceVariant = NonNullable<Service["variants"]>[number];

type PublicBookingPanelProps = {
  profile: PublicBusinessProfile;
};

type SubmitState =
  | {
      kind: "idle";
    }
  | {
      kind: "success";
      expiresAtUtc?: string | null;
      status?: string | null;
    }
  | {
      kind: "error";
      message: string;
    };

// Personel secimi "Fark etmez" = bos string. Radix Select bos string'i deger olarak KABUL
// ETMEZ (bos degeri placeholder temizligi icin ayirir), bu yuzden sentinel bir deger gerekiyor.
const anyStaffValue = "any";

export function PublicBookingPanel({ profile }: PublicBookingPanelProps) {
  const businessSlug = profile.slug ?? "";
  const branches = useMemo(() => profile.branches ?? [], [profile.branches]);
  const services = useMemo(() => profile.services ?? [], [profile.services]);
  const variantOptions = useMemo(() => createVariantOptions(services), [services]);
  const staffNamesVisible = showStaffNames(profile.metadata?.staffDisplayPolicy);
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
  const hasSearched = slotState.result !== undefined;
  // Personel filtresi sadece isimler GORUNURKEN anlamli. HideNames'te backend staffMembers'i
  // bos dondurur; secim kutusunu tamamen gizliyoruz ki bos bir kontrol kalmasin.
  const staffOptions = staffNamesVisible ? (selectedBranch?.staffMembers ?? []) : [];

  function resetBookingIntent() {
    setIdempotencyKey("");
    clearBookingDraft();
  }

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
    resetBookingIntent();
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
    resetBookingIntent();
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
    // Taslak GONDERMEDEN ONCE yazilir: 401 login'e firlatir, geri donunce secim burada durur.
    persistBookingDraft({
      branchSlug: selectedBranch.slug,
      businessSlug,
      date,
      idempotencyKey: requestIdempotencyKey,
      serviceVariantIds: selectedVariantIds,
      staffMemberId: selectedStaffId || undefined,
      startUtc: selectedSlotStartUtc
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
        if (shouldRecoverSlotSelection(response.status)) {
          setSelectedSlotStartUtc("");
          resetBookingIntent();
        }

        setSubmitState({
          kind: "error",
          message: getCreateErrorCopy(response.status)
        });
        return;
      }

      clearBookingDraft();
      setSubmitState({
        expiresAtUtc: data.expiresAtUtc,
        kind: "success",
        status: data.status
      });
    } catch {
      setSubmitState({
        kind: "error",
        message: "Talep şu anda gönderilemedi. Bağlantını kontrol edip tekrar dene."
      });
    }
  }

  const canSubmit = Boolean(selectedSlotStartUtc) && submitState.kind !== "success";

  // Gonderim sonrasi: secim arayuzu yerini "sonra ne olacak" anlatimina birakir.
  if (submitState.kind === "success") {
    return (
      <Card id="rezervasyon">
        <CardContent>
          <RequestSubmitted
            branch={selectedBranch}
            expiresAtUtc={submitState.expiresAtUtc}
            selectedSlot={selectedSlot}
            status={submitState.status}
          />
        </CardContent>
      </Card>
    );
  }

  return (
    <Card id="rezervasyon">
      <CardHeader>
        <CardTitle className="text-xl">Randevu al</CardTitle>
        <CardDescription>
          Hizmetini seç, uygun saatleri gör. Saatler şubenin kendi saatiyle gösterilir.
        </CardDescription>
      </CardHeader>

      <CardContent className="space-y-6">
        <BookingSteps
          hasSearched={hasSearched}
          hasSlot={Boolean(selectedSlotStartUtc)}
        />

        <Separator />

        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {/* Tek sube varsa secim kutusu bir karar degil, gurultudur. */}
          {branches.length > 1 ? (
            <div className="space-y-2">
              <Label htmlFor="booking-branch">Şube</Label>
              <Select
                onValueChange={(value) => {
                  const branch = branches.find((item) => item.slug === value);
                  setSelectedBranchSlug(value);
                  setDate(getTodayInBranchTimeZone(branch?.timeZoneId));
                  setSelectedSlotStartUtc("");
                  resetBookingIntent();
                  setSubmitState({ kind: "idle" });
                }}
                value={selectedBranchSlug}
              >
                <SelectTrigger className="min-h-11 w-full" id="booking-branch">
                  <SelectValue placeholder="Şube seç" />
                </SelectTrigger>
                <SelectContent>
                  {branches.map((branch) => (
                    <SelectItem key={branch.slug} value={branch.slug ?? ""}>
                      {branch.displayName ?? branch.slug}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          ) : null}

          <div className="space-y-2">
            <Label htmlFor="booking-date">Tarih</Label>
            <input
              className="flex min-h-11 w-full rounded-md border bg-transparent px-3 py-2 text-sm shadow-xs outline-none transition-[color,box-shadow] focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50 dark:bg-input/30"
              id="booking-date"
              onChange={(event) => {
                setDate(event.target.value);
                setSelectedSlotStartUtc("");
                resetBookingIntent();
                setSubmitState({ kind: "idle" });
              }}
              type="date"
              value={date}
            />
          </div>

          {staffOptions.length > 0 ? (
            <div className="space-y-2">
              <Label htmlFor="booking-staff">Personel tercihi</Label>
              <Select
                onValueChange={(value) => {
                  setSelectedStaffId(value === anyStaffValue ? "" : value);
                  setSelectedSlotStartUtc("");
                  resetBookingIntent();
                  setSubmitState({ kind: "idle" });
                }}
                value={selectedStaffId || anyStaffValue}
              >
                <SelectTrigger className="min-h-11 w-full" id="booking-staff">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={anyStaffValue}>Fark etmez</SelectItem>
                  {staffOptions.map((staff) => (
                    <SelectItem key={staff.id} value={staff.id ?? ""}>
                      {staff.displayName ?? "Personel"}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          ) : null}
        </div>

        <ServiceSelection
          onToggle={toggleVariant}
          options={variantOptions}
          selectedVariantIds={selectedVariantIds}
        />

        <Button
          className="min-h-11 w-full sm:w-auto"
          disabled={slotState.isLoading || selectedVariantIds.length === 0}
          onClick={() => void searchSlots()}
          type="button"
        >
          {slotState.isLoading ? "Saatler aranıyor..." : "Uygun saatleri ara"}
        </Button>

        {slotState.error ? (
          <Alert variant="destructive">
            <AlertDescription>{slotState.error}</AlertDescription>
          </Alert>
        ) : null}

        <SlotResults
          hasSearched={hasSearched}
          isLoading={slotState.isLoading}
          onSelect={(startUtc) => {
            setSelectedSlotStartUtc(startUtc);
            resetBookingIntent();
            setSubmitState({ kind: "idle" });
          }}
          selectedSlotStartUtc={selectedSlotStartUtc}
          slots={slotState.result?.slots ?? []}
          timeZoneId={slotState.result?.branchTimeZoneId ?? selectedBranch?.timeZoneId}
        />

        {selectedVariants.length > 0 ? (
          <>
            <Separator />
            <BookingSummary
              branch={selectedBranch}
              date={date}
              selectedSlot={selectedSlot}
              selectedSlotStartUtc={selectedSlotStartUtc}
              selectedVariants={selectedVariants}
              showBranch={branches.length > 1}
            />
          </>
        ) : null}

        {submitState.kind === "error" ? (
          <Alert variant="destructive">
            <AlertDescription>{submitState.message}</AlertDescription>
          </Alert>
        ) : null}

        {/* Masaustu gonderim. Mobilde buton sticky bar'a tasinir (asagida), ama ACIKLAMA
            METNI burada kalir: sticky bar'da tekrarlansa bar ~116px olur ve icerigi orter. */}
        <div className="space-y-2">
          <Button
            className="hidden min-h-11 lg:inline-flex"
            disabled={!canSubmit}
            onClick={() => void submitAppointmentRequest()}
            type="button"
          >
            Onay talebi gönder
          </Button>
          {/* Login duvari SURPRIZ OLMAMALI: anonim kullanici saatleri sonuna kadar gorur,
              duvara sadece gonderirken carpar. Bunu ONCEDEN soyluyoruz. */}
          <p className="text-xs leading-5 text-muted-foreground">
            Talebi göndermek için giriş yapman gerekir. Giriş yapmadıysan
            seçimlerin kısa süre saklanır, girişten sonra kaldığın yerden devam
            edersin.
          </p>
        </div>
      </CardContent>

      {/* Mobil BIRINCIL cihaz: saat seciminden sonra buton her zaman parmak altinda kalmali,
          musteri sayfayi asagi kaydirmak zorunda kalmasin.
          Sadece BUTON -- yuksekligi sabit tutuluyor ki sayfa altindaki pb-24 yetsin. */}
      <div className="fixed inset-x-0 bottom-0 z-40 border-t bg-background/95 p-3 backdrop-blur supports-[backdrop-filter]:bg-background/80 lg:hidden">
        <Button
          className="min-h-11 w-full"
          disabled={!canSubmit}
          onClick={() => void submitAppointmentRequest()}
          type="button"
        >
          Onay talebi gönder
        </Button>
      </div>
    </Card>
  );
}

function BookingSteps({
  hasSearched,
  hasSlot
}: {
  hasSearched: boolean;
  hasSlot: boolean;
}) {
  const steps = [
    { done: hasSearched, label: "Hizmet seç" },
    { done: hasSlot, label: "Saat seç" },
    { done: false, label: "Talep gönder" }
  ];
  const currentIndex = steps.findIndex((step) => !step.done);

  return (
    <ol className="flex flex-wrap items-center gap-x-2 gap-y-2">
      {steps.map((step, index) => {
        const isCurrent = index === currentIndex;

        return (
          <li className="flex items-center gap-2" key={step.label}>
            <span
              aria-current={isCurrent ? "step" : undefined}
              className={cn(
                "flex size-6 shrink-0 items-center justify-center rounded-full text-xs font-semibold",
                step.done && "bg-primary text-primary-foreground",
                isCurrent && "border-2 border-primary text-primary",
                !step.done && !isCurrent && "border text-muted-foreground"
              )}
            >
              {step.done ? <Check aria-hidden="true" className="size-3.5" /> : index + 1}
            </span>
            <span
              className={cn(
                "text-xs font-medium sm:text-sm",
                step.done || isCurrent ? "text-foreground" : "text-muted-foreground"
              )}
            >
              {step.label}
            </span>
            {index < steps.length - 1 ? (
              <span aria-hidden="true" className="ml-1 text-muted-foreground">
                ›
              </span>
            ) : null}
          </li>
        );
      })}
    </ol>
  );
}

function ServiceSelection({
  onToggle,
  options,
  selectedVariantIds
}: {
  onToggle: (variantId: string) => void;
  options: Array<ServiceVariant & { id: string; serviceName: string }>;
  selectedVariantIds: string[];
}) {
  if (options.length === 0) {
    return (
      <p className="rounded-md border border-dashed p-4 text-sm text-muted-foreground">
        Bu işletmede saat aranabilecek hizmet henüz yok.
      </p>
    );
  }

  return (
    <fieldset className="space-y-3">
      <legend className="text-sm font-medium text-foreground">
        Hizmetler{" "}
        <span className="font-normal text-muted-foreground">
          (birden fazla seçebilirsin)
        </span>
      </legend>
      <div className="grid gap-2 sm:grid-cols-2">
        {options.map((option) => {
          const checked = selectedVariantIds.includes(option.id);

          return (
            <Label
              className={cn(
                // min-h-11: dokunma hedefi. Etiketin TAMAMI tiklanabilir.
                "flex min-h-11 cursor-pointer items-start gap-3 rounded-md border p-3 font-normal transition-colors",
                checked ? "border-primary bg-accent" : "hover:bg-accent/50"
              )}
              key={option.id}
            >
              <Checkbox
                checked={checked}
                className="mt-0.5"
                onCheckedChange={() => onToggle(option.id)}
              />
              <span className="space-y-0.5">
                <span className="block text-sm font-medium text-foreground">
                  {option.serviceName} · {option.name}
                </span>
                <span className="block text-sm text-muted-foreground">
                  {option.durationMinutes ?? 0} dk ·{" "}
                  {formatMoney(option.priceAmount, option.currencyCode)}
                </span>
              </span>
            </Label>
          );
        })}
      </div>
    </fieldset>
  );
}

function SlotResults({
  hasSearched,
  isLoading,
  onSelect,
  selectedSlotStartUtc,
  slots,
  timeZoneId
}: {
  hasSearched: boolean;
  isLoading: boolean;
  onSelect: (startUtc: string) => void;
  selectedSlotStartUtc: string;
  slots: PublicSlot[];
  timeZoneId?: string | null;
}) {
  if (isLoading) {
    return (
      <div className="grid gap-2 grid-cols-[repeat(auto-fill,minmax(7rem,1fr))]">
        {[...Array(8)].map((_, index) => (
          <Skeleton className="h-11" key={index} />
        ))}
      </div>
    );
  }

  if (slots.length === 0) {
    return (
      <p className="rounded-md border border-dashed p-4 text-sm text-muted-foreground">
        {hasSearched
          ? "Bu tarihte uygun saat yok. Başka bir tarih ya da hizmet dene."
          : "Hizmet seçip 'Uygun saatleri ara' butonuna bas."}
      </p>
    );
  }

  return (
    <div className="space-y-3">
      <div>
        <h3 className="text-sm font-medium text-foreground">Uygun saatler</h3>
        {/* Saat dilimi GORUNUR yazilir: sube baska sehirde olabilir, musteri hangi saate
            baktigini bilmeli. */}
        <p className="mt-0.5 text-xs text-muted-foreground">
          Saatler şubenin yerel saatiyle gösterilir
          {timeZoneId ? ` (${timeZoneId})` : ""}.
        </p>
      </div>

      <div
        className="grid gap-2 grid-cols-[repeat(auto-fill,minmax(7rem,1fr))]"
        role="group"
      >
        {slots.map((slot) => {
          const selected = selectedSlotStartUtc === slot.startUtc;

          return (
            <button
              aria-pressed={selected}
              className={cn(
                "flex min-h-11 items-center justify-center gap-1.5 rounded-md border px-2 text-sm font-medium transition-colors outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50",
                selected
                  ? "border-primary bg-primary text-primary-foreground"
                  : "hover:bg-accent"
              )}
              key={slot.startUtc}
              onClick={() => slot.startUtc && onSelect(slot.startUtc)}
              type="button"
            >
              {/* Secili saat renkle DEGIL, ikonla da isaretlenir. */}
              {selected ? <Check aria-hidden="true" className="size-3.5" /> : null}
              {formatLocalSlot(slot.localStart)}
            </button>
          );
        })}
      </div>
    </div>
  );
}

function BookingSummary({
  branch,
  date,
  selectedSlot,
  selectedSlotStartUtc,
  selectedVariants,
  showBranch
}: {
  branch?: Branch;
  date: string;
  selectedSlot: PublicSlot | null;
  selectedSlotStartUtc: string;
  selectedVariants: Array<ServiceVariant & { serviceName: string }>;
  showBranch: boolean;
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
    <div className="space-y-3">
      <h3 className="text-sm font-medium text-foreground">Seçimin</h3>

      <ul className="space-y-1">
        {selectedVariants.map((variant) => (
          <li
            className="flex justify-between gap-4 text-sm"
            key={variant.id ?? variant.name}
          >
            <span className="text-muted-foreground">
              {variant.serviceName} · {variant.name}
            </span>
            <span className="text-foreground">
              {formatMoney(variant.priceAmount, variant.currencyCode)}
            </span>
          </li>
        ))}
      </ul>

      <Separator />

      <dl className="space-y-1 text-sm">
        {showBranch ? (
          <SummaryLine label="Şube" value={branch?.displayName ?? "Şube seç"} />
        ) : null}
        <SummaryLine label="Tarih" value={formatDateLabel(date)} />
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
        <SummaryLine label="Toplam süre" value={`${totalMinutes} dk`} />
        <div className="flex justify-between gap-4 pt-1">
          <dt className="font-medium text-foreground">Tahmini toplam</dt>
          <dd className="font-semibold text-foreground">
            {formatMoney(totalPrice, currencyCode)}
          </dd>
        </div>
      </dl>

      <p className="text-xs leading-5 text-muted-foreground">
        Ödeme burada alınmaz; tutar işletmede ödenir.
      </p>
    </div>
  );
}

function SummaryLine({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between gap-4">
      <dt className="text-muted-foreground">{label}</dt>
      <dd className="text-foreground">{value}</dd>
    </div>
  );
}

function RequestSubmitted({
  branch,
  expiresAtUtc,
  selectedSlot,
  status
}: {
  branch?: Branch;
  expiresAtUtc?: string | null;
  selectedSlot: PublicSlot | null;
  status?: string | null;
}) {
  return (
    <div className="space-y-5">
      <div className="flex items-start gap-3">
        <span className="flex size-9 shrink-0 items-center justify-center rounded-full bg-primary text-primary-foreground">
          <Check aria-hidden="true" className="size-5" />
        </span>
        <div className="space-y-1">
          <h2 className="text-lg font-semibold tracking-tight text-foreground">
            Talebin işletmeye iletildi
          </h2>
          <p className="text-sm text-muted-foreground">
            {selectedSlot
              ? `${formatLocalSlot(selectedSlot.localStart)} - ${formatLocalSlot(selectedSlot.localEnd)}`
              : "Seçtiğin saat"}
            {branch?.displayName ? ` · ${branch.displayName}` : ""}
          </p>
          <Badge variant="secondary">{getStatusCopy(status)}</Badge>
        </div>
      </div>

      <Alert>
        <Info aria-hidden="true" />
        <AlertTitle>Randevun henüz kesinleşmedi</AlertTitle>
        <AlertDescription>
          <ul className="list-disc space-y-1 pl-4">
            <li>İşletme talebini onaylarsa randevun kesinleşir.</li>
            <li>
              Aynı saate başka müşteriler de talep göndermiş olabilir; işletme
              birini seçer.
            </li>
            <li>
              Yanıt gelmezse talep kendiliğinden düşer ve randevu oluşmaz.
            </li>
          </ul>
        </AlertDescription>
      </Alert>

      {/* Sure HARDCODE EDILMEZ. Backend: expiry = min(olusturma + 24 saat,
          randevu saati - yanit tamponu). Yani yakin bir saate gonderilen talep 24 saatten
          COK ONCE duser. Yanit MUTLAK tarih olarak geliyorsa onu yaziyoruz. */}
      {expiresAtUtc ? (
        <div className="flex items-start gap-3 rounded-md border p-4">
          <Clock aria-hidden="true" className="mt-0.5 size-4 shrink-0 text-muted-foreground" />
          <div>
            <p className="text-sm font-medium text-foreground">
              Yanıt için son tarih
            </p>
            <p className="text-sm text-muted-foreground">
              {formatDeadline(expiresAtUtc, branch?.timeZoneId)}
            </p>
          </div>
        </div>
      ) : null}

      <Button asChild className="min-h-11 w-full sm:w-auto">
        <Link href={routes.customer.appointments}>Randevularıma git</Link>
      </Button>
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

function persistBookingDraft(draft: Omit<BookingDraft, "expiresAt" | "version">) {
  window.sessionStorage.setItem(
    bookingDraftStorageKey,
    JSON.stringify(createBookingDraft(draft))
  );
}

function readBookingDraft(businessSlug: string): BookingDraft | null {
  const draft = parseBookingDraft(
    window.sessionStorage.getItem(bookingDraftStorageKey),
    businessSlug
  );

  if (!draft) {
    clearBookingDraft();
  }

  return draft;
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

// SAAT DILIMI TUZAGI -- DOKUNMA.
// localStart sunucuda SUBENIN saat dilimine cevrilmis olarak gelir ("2026-06-15T14:30:00").
// Bu string'ten "HH:mm" KESILIR; new Date() ile parse EDILMEZ. Parse edilirse tarayici onu
// ziyaretcinin saat dilimine cevirir ve Izmir'deki musteriye Berlin'deki subenin saati
// YANLIS gosterilir. Donusum YOK, kesme VAR.
function formatLocalSlot(value?: string) {
  if (!value) {
    return "--:--";
  }

  return value.slice(11, 16);
}

// "2026-06-15" -> "15 Haziran 2026 Pazartesi". Girdi zaten sube-yerel bir TARIH (saat yok),
// bu yuzden UTC olarak parse edilip UTC olarak formatlanir: gun kaymaz.
function formatDateLabel(value: string) {
  if (!value) {
    return "Tarih seç";
  }

  const date = new Date(`${value}T00:00:00Z`);

  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat("tr-TR", {
    day: "numeric",
    month: "long",
    timeZone: "UTC",
    weekday: "long",
    year: "numeric"
  }).format(date);
}

// expiresAtUtc GERCEK bir UTC an'idir (slot'un localStart'i gibi onceden cevrilmis DEGIL),
// bu yuzden burada Intl ile subenin saat dilimine cevirmek DOGRU olan.
function formatDeadline(expiresAtUtc: string, branchTimeZoneId?: string | null) {
  const date = new Date(expiresAtUtc);

  if (Number.isNaN(date.getTime())) {
    return "İşletme kısa süre içinde yanıtlar.";
  }

  try {
    return new Intl.DateTimeFormat("tr-TR", {
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
      month: "long",
      timeZone: branchTimeZoneId ?? "Europe/Istanbul"
    }).format(date);
  } catch {
    return "İşletme kısa süre içinde yanıtlar.";
  }
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
    return "İşletme onayı bekliyor";
  }

  return status ?? "İşletme onayı bekliyor";
}
