"use client";

import {
  Building2,
  Clock,
  MapPin,
  MoreHorizontal,
  Pencil,
  Plus,
  SlidersHorizontal,
  Trash2
} from "lucide-react";
import { useRouter } from "next/navigation";
import { useState, type FormEvent, type ReactNode } from "react";
import { toast } from "sonner";
import {
  archiveBranch,
  BRANCH_TIME_ZONE_OPTIONS,
  createBranch,
  DEFAULT_BRANCH_TIME_ZONE,
  updateBranch,
  updateBranchSlotSettings,
  type BranchResult,
  type BusinessBranchResponse
} from "@/features/business/api/business-branch-client";
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
import { Card, CardContent } from "@/components/ui/card";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle
} from "@/components/ui/dialog";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger
} from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue
} from "@/components/ui/select";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle
} from "@/components/ui/sheet";
import { useIsMobile } from "@/shared/hooks/use-mobile";

/**
 * SUBE YONETIMI (Serit E) -- her sey subenin altinda (Tuzak 1). Sube; zaman dilimi,
 * calisma saati, personel ve kaynaklarin kapsayicisidir.
 *
 * TUZAK 3 (TIMEZONE): olusturma formunda zaman dilimi SERBEST METIN DEGIL, kuratorlu bir
 * IANA Select. Backend gecersizi 400 ile reddeder ama en dogrusu hic yanlis girme sansi
 * vermemek. Zaman dilimi olusturduktan SONRA degistirilemez (update request'te alan yok),
 * bu yuzden duzenleme formunda gorunmez.
 */

const SLUG_MIN = 2;
const SLUG_MAX = 64;
const NAME_MIN = 2;
const NAME_MAX = 200;
const CITY_MAX = 120;
const ADDRESS_MAX = 300;

type BranchDialogState =
  | { mode: "create" }
  | { mode: "edit"; branch: BusinessBranchResponse }
  | { mode: "slot"; branch: BusinessBranchResponse }
  | null;

export type BusinessBranchManagementPageProps = {
  tenantId: string;
  initialBranches: BusinessBranchResponse[];
};

export function BusinessBranchManagementPage({
  initialBranches,
  tenantId
}: BusinessBranchManagementPageProps) {
  const router = useRouter();
  const isMobile = useIsMobile();

  const [branches, setBranches] = useState<BusinessBranchResponse[]>(initialBranches);
  const [dialog, setDialog] = useState<BranchDialogState>(null);
  const [archiveTarget, setArchiveTarget] = useState<BusinessBranchResponse | null>(null);
  const [submitting, setSubmitting] = useState(false);

  /**
   * Mutasyon sonucunu TEK yerde yorumlar (personel ekraniyla ayni ayrim):
   * "rejected" (sunucu reddetti) -> yerel liste bayat, refresh; "failed" (ag) -> refresh ETME.
   */
  function handleFailure(result: BranchResult<unknown>) {
    if (result.kind === "rejected") {
      toast.error(result.message);
      router.refresh();
      return;
    }
    if (result.kind === "failed") {
      toast.error(result.message);
    }
  }

  async function submitCreate(draft: CreateDraft) {
    setSubmitting(true);
    const result = await createBranch(tenantId, {
      slug: draft.slug.trim(),
      displayName: draft.displayName.trim(),
      timeZoneId: draft.timeZoneId,
      city: draft.city.trim() || undefined,
      district: draft.district.trim() || undefined,
      addressLine: draft.addressLine.trim() || undefined
    });
    setSubmitting(false);

    if (result.kind !== "success") {
      handleFailure(result);
      return;
    }

    if (result.data) {
      setBranches((current) => [...current, result.data as BusinessBranchResponse]);
    }
    toast.success("Şube eklendi.");
    setDialog(null);
    router.refresh();
  }

  async function submitEdit(branchId: string, draft: EditDraft) {
    setSubmitting(true);
    const result = await updateBranch(tenantId, branchId, {
      displayName: draft.displayName.trim(),
      city: draft.city.trim() || undefined,
      district: draft.district.trim() || undefined,
      addressLine: draft.addressLine.trim() || undefined
    });
    setSubmitting(false);

    if (result.kind !== "success") {
      handleFailure(result);
      return;
    }

    if (result.data !== null) {
      const saved = result.data;
      setBranches((current) =>
        current.map((entry) => (entry.id === branchId ? { ...entry, ...saved } : entry))
      );
    }
    toast.success("Şube güncellendi.");
    setDialog(null);
    router.refresh();
  }

  async function submitSlot(branchId: string, draft: SlotDraft) {
    setSubmitting(true);
    const result = await updateBranchSlotSettings(tenantId, branchId, {
      slotIntervalMinutes: draft.slotIntervalMinutes,
      maxPublicSlots: draft.maxPublicSlots
    });
    setSubmitting(false);

    if (result.kind !== "success") {
      handleFailure(result);
      return;
    }

    if (result.data !== null) {
      const saved = result.data;
      setBranches((current) =>
        current.map((entry) => (entry.id === branchId ? { ...entry, ...saved } : entry))
      );
    }
    toast.success("Slot ayarları kaydedildi.");
    setDialog(null);
    router.refresh();
  }

  async function confirmArchive() {
    if (!archiveTarget?.id) {
      return;
    }
    setSubmitting(true);
    const result = await archiveBranch(tenantId, archiveTarget.id);
    setSubmitting(false);

    if (result.kind !== "success") {
      handleFailure(result);
      setArchiveTarget(null);
      return;
    }

    setBranches((current) => current.filter((entry) => entry.id !== archiveTarget.id));
    toast.success("Şube arşivlendi.");
    setArchiveTarget(null);
    router.refresh();
  }

  // Dialog/Sheet ICERIGI -- moda gore degisir.
  const dialogTitle =
    dialog?.mode === "edit"
      ? "Şubeyi düzenle"
      : dialog?.mode === "slot"
        ? "Slot ayarları"
        : "Yeni şube";

  const dialogDescription =
    dialog?.mode === "edit"
      ? "Şube adı ve konum bilgilerini güncelle. Zaman dilimi oluşturulduktan sonra değiştirilemez."
      : dialog?.mode === "slot"
        ? "Herkese açık randevu ekranında bu şube için kaç slot ve hangi aralıkla gösterileceğini belirle."
        : "Şube; zaman dilimi, çalışma saatleri, personel ve kaynakların kapsayıcısıdır.";

  const dialogBody = dialog ? (
    dialog.mode === "create" ? (
      <BranchCreateForm
        onCancel={() => setDialog(null)}
        onSubmit={submitCreate}
        submitting={submitting}
      />
    ) : dialog.mode === "edit" ? (
      <BranchEditForm
        branch={dialog.branch}
        key={dialog.branch.id}
        onCancel={() => setDialog(null)}
        onSubmit={(draft) => submitEdit(dialog.branch.id ?? "", draft)}
        submitting={submitting}
      />
    ) : (
      <BranchSlotForm
        branch={dialog.branch}
        key={dialog.branch.id}
        onCancel={() => setDialog(null)}
        onSubmit={(draft) => submitSlot(dialog.branch.id ?? "", draft)}
        submitting={submitting}
      />
    )
  ) : null;

  return (
    <div className="space-y-6">
      <header className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-3xl font-semibold tracking-tight">Şubeler</h1>
          <p className="mt-2 max-w-2xl text-sm text-muted-foreground">
            İşletmenin şubelerini yönet. Her şubenin kendi zaman dilimi, çalışma saatleri
            ve personeli olur; randevu saatleri şubenin zaman dilimine göre hesaplanır.
          </p>
        </div>
        <Button
          className="min-h-11 shrink-0"
          onClick={() => setDialog({ mode: "create" })}
        >
          <Plus aria-hidden className="size-4" />
          Şube ekle
        </Button>
      </header>

      {branches.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center gap-4 py-14 text-center">
            <div className="flex size-14 items-center justify-center rounded-full bg-muted">
              <Building2 aria-hidden className="size-6 text-muted-foreground" />
            </div>
            <div className="space-y-1">
              <h2 className="text-lg font-semibold">Henüz şube yok</h2>
              <p className="mx-auto max-w-sm text-sm text-muted-foreground">
                İlk şubeni ekle; personel, çalışma saatleri ve randevular bu şubenin
                altında yönetilir.
              </p>
            </div>
            <Button className="min-h-11" onClick={() => setDialog({ mode: "create" })}>
              <Plus aria-hidden className="size-4" />
              Şube ekle
            </Button>
          </CardContent>
        </Card>
      ) : (
        <div className="grid gap-4 lg:grid-cols-2">
          {branches.map((branch) => (
            <BranchCard
              branch={branch}
              key={branch.id}
              onArchive={() => setArchiveTarget(branch)}
              onEdit={() => setDialog({ mode: "edit", branch })}
              onSlotSettings={() => setDialog({ mode: "slot", branch })}
            />
          ))}
        </div>
      )}

      {/* FORM -- masaustunde Dialog, mobilde alttan Sheet (resepsiyon tableti birincil cihaz) */}
      {isMobile ? (
        <Sheet
          onOpenChange={(open) => !open && setDialog(null)}
          open={dialog !== null}
        >
          <SheetContent className="overflow-y-auto" side="bottom">
            <SheetHeader>
              <SheetTitle>{dialogTitle}</SheetTitle>
              <SheetDescription>{dialogDescription}</SheetDescription>
            </SheetHeader>
            <div className="px-4 pb-6">{dialogBody}</div>
          </SheetContent>
        </Sheet>
      ) : (
        <Dialog onOpenChange={(open) => !open && setDialog(null)} open={dialog !== null}>
          <DialogContent className="max-h-[90vh] overflow-y-auto">
            <DialogHeader>
              <DialogTitle>{dialogTitle}</DialogTitle>
              <DialogDescription>{dialogDescription}</DialogDescription>
            </DialogHeader>
            {dialogBody}
          </DialogContent>
        </Dialog>
      )}

      <ArchiveBranchDialog
        branch={archiveTarget}
        onCancel={() => setArchiveTarget(null)}
        onConfirm={confirmArchive}
        submitting={submitting}
      />
    </div>
  );
}

/* ---------------------------------------------------------------------------
   SUBE KARTI
   --------------------------------------------------------------------------- */

function BranchCard({
  branch,
  onArchive,
  onEdit,
  onSlotSettings
}: {
  branch: BusinessBranchResponse;
  onArchive: () => void;
  onEdit: () => void;
  onSlotSettings: () => void;
}) {
  const location = [branch.addressLine, branch.district, branch.city]
    .filter(Boolean)
    .join(", ");

  return (
    <Card>
      <CardContent className="space-y-4 p-5">
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0">
            <h2 className="truncate text-lg font-semibold">{branch.displayName}</h2>
            <p className="mt-1 font-mono text-xs text-muted-foreground">{branch.slug}</p>
          </div>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button className="size-11 shrink-0" size="icon" variant="ghost">
                <MoreHorizontal aria-hidden className="size-4" />
                <span className="sr-only">Şube işlemleri</span>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem className="min-h-11" onSelect={onEdit}>
                <Pencil aria-hidden className="size-4" />
                Düzenle
              </DropdownMenuItem>
              <DropdownMenuItem className="min-h-11" onSelect={onSlotSettings}>
                <SlidersHorizontal aria-hidden className="size-4" />
                Slot ayarları
              </DropdownMenuItem>
              <DropdownMenuItem
                className="min-h-11 text-destructive focus:text-destructive"
                onSelect={onArchive}
              >
                <Trash2 aria-hidden className="size-4" />
                Arşivle
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <Badge className="gap-1" variant="secondary">
            <Clock aria-hidden className="size-3" />
            {branch.timeZoneId ?? "Zaman dilimi yok"}
          </Badge>
          {branch.slotIntervalMinutes ? (
            <Badge variant="outline">Aralık: {branch.slotIntervalMinutes} dk</Badge>
          ) : null}
          {branch.maxPublicSlots ? (
            <Badge variant="outline">Maks. slot: {branch.maxPublicSlots}</Badge>
          ) : null}
        </div>

        {location ? (
          <p className="flex items-start gap-2 text-sm text-muted-foreground">
            <MapPin aria-hidden className="mt-0.5 size-4 shrink-0" />
            <span>{location}</span>
          </p>
        ) : (
          <p className="text-sm text-muted-foreground">Konum bilgisi girilmemiş.</p>
        )}
      </CardContent>
    </Card>
  );
}

/* ---------------------------------------------------------------------------
   OLUSTURMA FORMU
   --------------------------------------------------------------------------- */

type CreateDraft = {
  slug: string;
  displayName: string;
  timeZoneId: string;
  city: string;
  district: string;
  addressLine: string;
};

function BranchCreateForm({
  onCancel,
  onSubmit,
  submitting
}: {
  onCancel: () => void;
  onSubmit: (draft: CreateDraft) => void;
  submitting: boolean;
}) {
  const [draft, setDraft] = useState<CreateDraft>({
    slug: "",
    displayName: "",
    timeZoneId: DEFAULT_BRANCH_TIME_ZONE,
    city: "",
    district: "",
    addressLine: ""
  });

  const slugTrimmed = draft.slug.trim();
  const nameTrimmed = draft.displayName.trim();
  const slugInvalid = slugTrimmed.length < SLUG_MIN || slugTrimmed.length > SLUG_MAX;
  const nameInvalid = nameTrimmed.length < NAME_MIN || nameTrimmed.length > NAME_MAX;
  const invalid = slugInvalid || nameInvalid || !draft.timeZoneId;

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (invalid || submitting) {
      return;
    }
    onSubmit(draft);
  }

  return (
    <form className="space-y-5" onSubmit={handleSubmit}>
      <Field
        hint={`Değişmez kısa kod (ör. merkez). En az ${SLUG_MIN}, en fazla ${SLUG_MAX} karakter.`}
        htmlFor="sube-slug"
        label="Şube kodu"
      >
        <Input
          autoComplete="off"
          autoFocus
          className="min-h-11"
          id="sube-slug"
          maxLength={SLUG_MAX}
          onChange={(event) => setDraft((p) => ({ ...p, slug: event.target.value }))}
          placeholder="merkez"
          value={draft.slug}
        />
      </Field>

      <Field
        hint="Müşterilerin ve panelin göreceği ad."
        htmlFor="sube-adi"
        label="Şube adı"
      >
        <Input
          autoComplete="off"
          className="min-h-11"
          id="sube-adi"
          maxLength={NAME_MAX}
          onChange={(event) =>
            setDraft((p) => ({ ...p, displayName: event.target.value }))
          }
          placeholder="Örn. Merkez Şube"
          value={draft.displayName}
        />
      </Field>

      <Field
        hint="Randevu saatleri bu zaman dilimine göre hesaplanır. Sonradan değiştirilemez."
        htmlFor="sube-tz"
        label="Zaman dilimi"
      >
        <Select
          onValueChange={(value) => setDraft((p) => ({ ...p, timeZoneId: value }))}
          value={draft.timeZoneId}
        >
          <SelectTrigger className="min-h-11" id="sube-tz">
            <SelectValue placeholder="Zaman dilimi seç" />
          </SelectTrigger>
          <SelectContent>
            {BRANCH_TIME_ZONE_OPTIONS.map((option) => (
              <SelectItem key={option.id} value={option.id}>
                {option.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </Field>

      <LocationFields
        draft={draft}
        onChange={(patch) => setDraft((p) => ({ ...p, ...patch }))}
      />

      <FormActions
        disabled={invalid}
        onCancel={onCancel}
        submitLabel="Şubeyi ekle"
        submitting={submitting}
      />
    </form>
  );
}

/* ---------------------------------------------------------------------------
   DUZENLEME FORMU -- slug ve zaman dilimi YOK (update request'te alan yok)
   --------------------------------------------------------------------------- */

type EditDraft = {
  displayName: string;
  city: string;
  district: string;
  addressLine: string;
};

function BranchEditForm({
  branch,
  onCancel,
  onSubmit,
  submitting
}: {
  branch: BusinessBranchResponse;
  onCancel: () => void;
  onSubmit: (draft: EditDraft) => void;
  submitting: boolean;
}) {
  const [draft, setDraft] = useState<EditDraft>({
    displayName: branch.displayName ?? "",
    city: branch.city ?? "",
    district: branch.district ?? "",
    addressLine: branch.addressLine ?? ""
  });

  const nameTrimmed = draft.displayName.trim();
  const invalid = nameTrimmed.length < NAME_MIN || nameTrimmed.length > NAME_MAX;

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (invalid || submitting) {
      return;
    }
    onSubmit(draft);
  }

  return (
    <form className="space-y-5" onSubmit={handleSubmit}>
      <Field
        hint="Müşterilerin ve panelin göreceği ad."
        htmlFor="sube-adi-edit"
        label="Şube adı"
      >
        <Input
          autoComplete="off"
          autoFocus
          className="min-h-11"
          id="sube-adi-edit"
          maxLength={NAME_MAX}
          onChange={(event) =>
            setDraft((p) => ({ ...p, displayName: event.target.value }))
          }
          value={draft.displayName}
        />
      </Field>

      <LocationFields
        draft={draft}
        onChange={(patch) => setDraft((p) => ({ ...p, ...patch }))}
      />

      <FormActions
        disabled={invalid}
        onCancel={onCancel}
        submitLabel="Kaydet"
        submitting={submitting}
      />
    </form>
  );
}

/* ---------------------------------------------------------------------------
   SLOT AYARLARI FORMU
   --------------------------------------------------------------------------- */

type SlotDraft = {
  slotIntervalMinutes: number | null;
  maxPublicSlots: number | null;
};

function BranchSlotForm({
  branch,
  onCancel,
  onSubmit,
  submitting
}: {
  branch: BusinessBranchResponse;
  onCancel: () => void;
  onSubmit: (draft: SlotDraft) => void;
  submitting: boolean;
}) {
  const [interval, setInterval] = useState<string>(
    branch.slotIntervalMinutes != null ? String(branch.slotIntervalMinutes) : ""
  );
  const [maxSlots, setMaxSlots] = useState<string>(
    branch.maxPublicSlots != null ? String(branch.maxPublicSlots) : ""
  );

  const intervalValue = interval.trim() === "" ? null : Number(interval);
  const maxSlotsValue = maxSlots.trim() === "" ? null : Number(maxSlots);

  // Backend: deger verildiyse > 0 olmali (null serbest). Bos = null = "varsayilana bırak".
  const intervalInvalid =
    intervalValue !== null && (!Number.isInteger(intervalValue) || intervalValue <= 0);
  const maxSlotsInvalid =
    maxSlotsValue !== null && (!Number.isInteger(maxSlotsValue) || maxSlotsValue <= 0);
  const invalid = intervalInvalid || maxSlotsInvalid;

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (invalid || submitting) {
      return;
    }
    onSubmit({ slotIntervalMinutes: intervalValue, maxPublicSlots: maxSlotsValue });
  }

  return (
    <form className="space-y-5" onSubmit={handleSubmit}>
      <Field
        hint="Randevu slotları kaç dakikalık aralıklarla sunulsun (ör. 30). Boş bırakırsan varsayılan kullanılır."
        htmlFor="slot-aralik"
        invalid={intervalInvalid}
        invalidText="Aralık 0'dan büyük bir tam sayı olmalı."
        label="Slot aralığı (dakika)"
      >
        <Input
          autoFocus
          className="min-h-11"
          id="slot-aralik"
          inputMode="numeric"
          min={1}
          onChange={(event) => setInterval(event.target.value)}
          placeholder="30"
          type="number"
          value={interval}
        />
      </Field>

      <Field
        hint="Herkese açık randevu ekranında en fazla kaç slot gösterilsin. Boş bırakırsan varsayılan kullanılır."
        htmlFor="slot-maks"
        invalid={maxSlotsInvalid}
        invalidText="Slot sayısı 0'dan büyük bir tam sayı olmalı."
        label="Maksimum herkese açık slot"
      >
        <Input
          className="min-h-11"
          id="slot-maks"
          inputMode="numeric"
          min={1}
          onChange={(event) => setMaxSlots(event.target.value)}
          placeholder="20"
          type="number"
          value={maxSlots}
        />
      </Field>

      <FormActions
        disabled={invalid}
        onCancel={onCancel}
        submitLabel="Slot ayarlarını kaydet"
        submitting={submitting}
      />
    </form>
  );
}

/* ---------------------------------------------------------------------------
   ARSIVLEME ONAYI (Yikici aksiyon -> AlertDialog)
   --------------------------------------------------------------------------- */

function ArchiveBranchDialog({
  branch,
  onCancel,
  onConfirm,
  submitting
}: {
  branch: BusinessBranchResponse | null;
  onCancel: () => void;
  onConfirm: () => void;
  submitting: boolean;
}) {
  return (
    <AlertDialog onOpenChange={(open) => !open && onCancel()} open={branch !== null}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>
            &quot;{branch?.displayName}&quot; arşivlensin mi?
          </AlertDialogTitle>
          <AlertDialogDescription>
            Arşivlenen şube listeden kalkar ve yeni randevuya kapanır.{" "}
            <strong>Şubeye bağlı personel varsa arşivleme başarısız olur</strong> — önce
            personeli başka bir şubeye taşı veya arşivle.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel className="min-h-11" disabled={submitting}>
            Vazgeç
          </AlertDialogCancel>
          <AlertDialogAction
            className="min-h-11 bg-destructive text-white hover:bg-destructive/90"
            disabled={submitting}
            onClick={(event) => {
              // Istek bitene kadar dialog acik kalsin.
              event.preventDefault();
              onConfirm();
            }}
          >
            {submitting ? "Arşivleniyor…" : "Arşivle"}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}

/* ---------------------------------------------------------------------------
   PAYLASILAN FORM PARCALARI
   --------------------------------------------------------------------------- */

function LocationFields({
  draft,
  onChange
}: {
  draft: { city: string; district: string; addressLine: string };
  onChange: (patch: Partial<{ city: string; district: string; addressLine: string }>) => void;
}) {
  return (
    <>
      <div className="grid gap-5 sm:grid-cols-2">
        <Field htmlFor="sube-il" label="İl">
          <Input
            className="min-h-11"
            id="sube-il"
            maxLength={CITY_MAX}
            onChange={(event) => onChange({ city: event.target.value })}
            placeholder="İstanbul"
            value={draft.city}
          />
        </Field>
        <Field htmlFor="sube-ilce" label="İlçe">
          <Input
            className="min-h-11"
            id="sube-ilce"
            maxLength={CITY_MAX}
            onChange={(event) => onChange({ district: event.target.value })}
            placeholder="Kadıköy"
            value={draft.district}
          />
        </Field>
      </div>
      <Field htmlFor="sube-adres" label="Adres">
        <Input
          className="min-h-11"
          id="sube-adres"
          maxLength={ADDRESS_MAX}
          onChange={(event) => onChange({ addressLine: event.target.value })}
          placeholder="Cadde, sokak, no"
          value={draft.addressLine}
        />
      </Field>
    </>
  );
}

function Field({
  children,
  hint,
  htmlFor,
  invalid,
  invalidText,
  label
}: {
  children: ReactNode;
  hint?: string;
  htmlFor: string;
  invalid?: boolean;
  invalidText?: string;
  label: string;
}) {
  return (
    <div className="space-y-2">
      <Label htmlFor={htmlFor}>{label}</Label>
      {children}
      {/* Bilgi GORUNUR etikette; tooltip degil. Hata varsa hata metni onceliklidir. */}
      {invalid && invalidText ? (
        <p className="text-sm text-destructive">{invalidText}</p>
      ) : hint ? (
        <p className="text-sm text-muted-foreground">{hint}</p>
      ) : null}
    </div>
  );
}

function FormActions({
  disabled,
  onCancel,
  submitLabel,
  submitting
}: {
  disabled: boolean;
  onCancel: () => void;
  submitLabel: string;
  submitting: boolean;
}) {
  return (
    <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
      <Button
        className="min-h-11"
        disabled={submitting}
        onClick={onCancel}
        type="button"
        variant="outline"
      >
        Vazgeç
      </Button>
      <Button className="min-h-11" disabled={disabled || submitting} type="submit">
        {submitting ? "Kaydediliyor…" : submitLabel}
      </Button>
    </div>
  );
}
