"use client";

import {
  CalendarOff,
  ChevronDown,
  MoreHorizontal,
  Pencil,
  Plus,
  Trash2,
  TriangleAlert,
  Users
} from "lucide-react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import {
  Fragment,
  useCallback,
  useEffect,
  useState,
  type FormEvent
} from "react";
import { toast } from "sonner";
import {
  archiveStaff,
  createStaff,
  createUnavailable,
  deleteUnavailable,
  describeStaffStatus,
  listStaff,
  listUnavailable,
  renameStaff,
  type BusinessStaff,
  type StaffResult,
  type StaffUnavailable
} from "@/features/business/api/business-staff-client";
import type { BusinessBranch } from "@/features/business/api/get-business-branches-server";
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
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow
} from "@/components/ui/table";
import { routes, withTenant } from "@/shared/config/routes";
import { useIsMobile } from "@/shared/hooks/use-mobile";
import { cn } from "@/shared/lib/cn";
import {
  formatBranchDateTime,
  parseBranchDateTimeLocalValue
} from "@/shared/lib/date-time";

/**
 * EKIP YONETIMI (Serit D) -- personel = TAKVIM KAYNAGI, login'i olan kullanici DEGIL.
 *
 * NEDEN ACILIR SATIR (ACCORDION), AYRI DETAY SAYFASI DEGIL -- kardes ekran
 * (hizmetler, Serit C) ile ayni gerekce:
 *
 * 1. Ayri detay sayfasi personeli TEK BASINA cekemez. Personel ucları SUBE ALTINDA
 *    nested (GET .../branches/{branchId}/staff/{staffId}); branchId olmadan yuklenemez.
 *    Ayri rota ya query-param ile branchId tasirdi (kirilgan) ya da her subeyi tarardi.
 *    Liste ZATEN elimizde (branchId dahil) -> bir satiri acmak SIFIR ek istek gerektirir.
 * 2. Kucuk yuzey. Ayri rota => yeni page.tsx + nav-manifest'te hidden kayit + izin
 *    tablosunda yeni satir + URL ile girilebilen yeni bir yikici yuzey. Bir salonun
 *    birkac elemani icin bu bedel bosuna.
 * 3. Duzgun kuculur: <768px'te personel satiri KARTA, acilan bolum kart ici panele duser.
 *
 * KASITLI OLARAK YOK:
 *  - "userAccountId" alani: personel bir takvim kaydidir (Tuzak 2). Sadece ad girilir.
 *  - "Yetkinlik" sekmesi: atama WRITE-ONLY, okuyan GET yok -> cizilemez (Tuzak 4).
 *  - "Calisma saatleri" sekmesi: personel bazli mesai yok, sadece sube saati var (Tuzak 5).
 *    Onun yerine OKUNABILIR olan "Izin / Musait degil" var.
 */

const NAME_MIN = 2;
const NAME_MAX = 200;
const REASON_MAX = 200;

/* ---------------------------------------------------------------------------
   ANA EKRAN
   --------------------------------------------------------------------------- */

type StaffDialogState =
  | { mode: "create" }
  | { mode: "edit"; staff: BusinessStaff }
  | null;

export type BusinessStaffPageProps = {
  tenantId: string;
  branches: BusinessBranch[];
};

export function BusinessStaffPage({ branches, tenantId }: BusinessStaffPageProps) {
  const router = useRouter();
  const isMobile = useIsMobile();

  // Tek sube varsa OTOMATIK sec (Tuzak 1). Birden fazlaysa kullanici secer.
  const [selectedBranchId, setSelectedBranchId] = useState<string>(
    branches.length === 1 ? (branches[0]?.id ?? "") : ""
  );

  const [staff, setStaff] = useState<BusinessStaff[]>([]);
  const [status, setStatus] = useState<"idle" | "loading" | "ready" | "error">(
    "idle"
  );
  const [expanded, setExpanded] = useState<ReadonlySet<string>>(new Set());
  const [submitting, setSubmitting] = useState(false);

  const [staffDialog, setStaffDialog] = useState<StaffDialogState>(null);
  const [archiveTarget, setArchiveTarget] = useState<BusinessStaff | null>(null);

  const selectedBranch = branches.find((branch) => branch.id === selectedBranchId);
  const branchTimeZoneId = selectedBranch?.timeZoneId ?? "Europe/Istanbul";

  /**
   * Mutasyon sonucunu TEK yerde yorumlar (hizmetler ekranindaki ayni ayrim):
   * "rejected" (sunucu reddetti) -> yerel liste bayat, refresh; "failed" (ag) -> refresh ETME.
   */
  const handleFailure = useCallback(
    (result: StaffResult<unknown>) => {
      if (result.kind === "rejected") {
        toast.error(result.message);
        router.refresh();
        return;
      }

      if (result.kind === "failed") {
        toast.error(result.message);
      }
    },
    [router]
  );

  const loadStaff = useCallback(
    async (branchId: string) => {
      setStatus("loading");
      const result = await listStaff(tenantId, branchId);

      if (result.kind !== "success") {
        setStatus("error");
        toast.error(result.message);
        return;
      }

      setStaff(result.data ?? []);
      setExpanded(new Set());
      setStatus("ready");
    },
    [tenantId]
  );

  useEffect(() => {
    if (!selectedBranchId) {
      setStatus("idle");
      setStaff([]);
      return;
    }

    void loadStaff(selectedBranchId);
  }, [selectedBranchId, loadStaff]);

  function toggle(staffId: string) {
    setExpanded((current) => {
      const next = new Set(current);
      if (next.has(staffId)) {
        next.delete(staffId);
      } else {
        next.add(staffId);
      }
      return next;
    });
  }

  async function submitStaff(displayName: string) {
    if (!staffDialog || !selectedBranchId) {
      return;
    }

    setSubmitting(true);

    const result =
      staffDialog.mode === "edit"
        ? await renameStaff(
            tenantId,
            selectedBranchId,
            staffDialog.staff.id ?? "",
            displayName
          )
        : await createStaff(tenantId, selectedBranchId, displayName);

    setSubmitting(false);

    if (result.kind !== "success") {
      handleFailure(result);
      return;
    }

    const saved = result.data;

    // saved === null: yazma basarili ama govde eksik -> yerel yamayi atla, refresh getirsin.
    if (saved !== null) {
      if (staffDialog.mode === "edit") {
        setStaff((current) =>
          current.map((entry) => (entry.id === saved.id ? { ...entry, ...saved } : entry))
        );
      } else {
        setStaff((current) => [...current, saved]);
      }
    }

    toast.success(
      staffDialog.mode === "edit" ? "Personel adı güncellendi." : "Personel eklendi."
    );
    setStaffDialog(null);
    router.refresh();
  }

  async function confirmArchive() {
    if (!archiveTarget || !selectedBranchId) {
      return;
    }

    setSubmitting(true);
    const result = await archiveStaff(
      tenantId,
      selectedBranchId,
      archiveTarget.id ?? ""
    );
    setSubmitting(false);

    if (result.kind !== "success") {
      handleFailure(result);
      setArchiveTarget(null);
      return;
    }

    const saved = result.data;

    // Personel LISTEDEN DUSMEZ; arsivli statuyle kalir (geri alma ucu yok, gorunur olsun).
    setStaff((current) =>
      current.map((entry) =>
        entry.id === archiveTarget.id
          ? saved ?? { ...entry, status: "Archived" }
          : entry
      )
    );
    toast.success("Personel arşivlendi.");
    setArchiveTarget(null);
    router.refresh();
  }

  const staffFormNode = staffDialog ? (
    <StaffNameForm
      key={staffDialog.mode === "edit" ? staffDialog.staff.id : "yeni"}
      initialName={staffDialog.mode === "edit" ? staffDialog.staff.displayName ?? "" : ""}
      mode={staffDialog.mode}
      onCancel={() => setStaffDialog(null)}
      onSubmit={submitStaff}
      submitting={submitting}
    />
  ) : null;

  const staffDialogTitle =
    staffDialog?.mode === "edit" ? "Personel adını düzenle" : "Personel ekle";
  const staffDialogDescription =
    "Personel, takviminde randevu alınan bir kişidir (ör. “Ayşe Usta”). Sadece görünen adı gir.";

  // Sube secilmemis ve hic sube yoksa: once sube ekle.
  if (branches.length === 0) {
    return (
      <div className="space-y-6">
        <StaffHeader onAdd={() => undefined} addDisabled />
        <Card>
          <CardContent className="flex flex-col items-center gap-4 py-14 text-center">
            <div className="flex size-14 items-center justify-center rounded-full bg-muted">
              <Users aria-hidden className="size-6 text-muted-foreground" />
            </div>
            <div className="space-y-1">
              <h2 className="text-lg font-semibold">Önce bir şube ekle</h2>
              <p className="mx-auto max-w-sm text-sm text-muted-foreground">
                Ekip her zaman bir şubeye bağlıdır. Personel eklemeden önce en az bir
                şube tanımlaman gerekiyor.
              </p>
            </div>
            <Button asChild className="min-h-11">
              <Link href={withTenant(routes.business.branches, tenantId)}>
                Şubelere git
              </Link>
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <StaffHeader
        addDisabled={!selectedBranchId}
        onAdd={() => setStaffDialog({ mode: "create" })}
      />

      {/* SUBE SECICI -- birden fazla sube varsa. Tek subede otomatik secili. */}
      {branches.length > 1 ? (
        <div className="flex flex-col gap-2 sm:max-w-sm">
          <Label htmlFor="sube-secici">Şube</Label>
          <Select onValueChange={setSelectedBranchId} value={selectedBranchId}>
            <SelectTrigger className="min-h-11" id="sube-secici">
              <SelectValue placeholder="Ekibini görmek için bir şube seç" />
            </SelectTrigger>
            <SelectContent>
              {branches.map((branch) => (
                <SelectItem key={branch.id} value={branch.id ?? ""}>
                  {branch.displayName}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      ) : selectedBranch ? (
        <p className="text-sm text-muted-foreground">
          Şube: <span className="font-medium text-foreground">{selectedBranch.displayName}</span>
        </p>
      ) : null}

      <StaffList
        branchTimeZoneId={branchTimeZoneId}
        expanded={expanded}
        onAdd={() => setStaffDialog({ mode: "create" })}
        onArchive={(entry) => setArchiveTarget(entry)}
        onEdit={(entry) => setStaffDialog({ mode: "edit", staff: entry })}
        onToggle={toggle}
        selectedBranchId={selectedBranchId}
        staff={staff}
        status={status}
        tenantId={tenantId}
      />

      {/* PERSONEL FORMU -- masaustunde Dialog, mobilde Sheet */}
      {isMobile ? (
        <Sheet
          onOpenChange={(open) => !open && setStaffDialog(null)}
          open={staffDialog !== null}
        >
          <SheetContent className="overflow-y-auto" side="bottom">
            <SheetHeader>
              <SheetTitle>{staffDialogTitle}</SheetTitle>
              <SheetDescription>{staffDialogDescription}</SheetDescription>
            </SheetHeader>
            <div className="px-4 pb-4">{staffFormNode}</div>
          </SheetContent>
        </Sheet>
      ) : (
        <Dialog
          onOpenChange={(open) => !open && setStaffDialog(null)}
          open={staffDialog !== null}
        >
          <DialogContent>
            <DialogHeader>
              <DialogTitle>{staffDialogTitle}</DialogTitle>
              <DialogDescription>{staffDialogDescription}</DialogDescription>
            </DialogHeader>
            {staffFormNode}
          </DialogContent>
        </Dialog>
      )}

      <ArchiveStaffDialog
        onCancel={() => setArchiveTarget(null)}
        onConfirm={confirmArchive}
        staff={archiveTarget}
        submitting={submitting}
      />
    </div>
  );
}

/* ---------------------------------------------------------------------------
   BASLIK
   --------------------------------------------------------------------------- */

function StaffHeader({
  addDisabled,
  onAdd
}: {
  addDisabled: boolean;
  onAdd: () => void;
}) {
  return (
    <header className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
      <div>
        <h1 className="text-3xl font-semibold tracking-tight">Ekip</h1>
        <p className="mt-2 max-w-2xl text-sm text-muted-foreground">
          Salonundaki personeli yönet. Her personel bir takvim kaynağıdır — randevular
          onların adına alınır. İzin ve müsait olmadığı zamanları buradan girebilirsin.
        </p>
      </div>
      <Button className="min-h-11 shrink-0" disabled={addDisabled} onClick={onAdd}>
        <Plus aria-hidden className="size-4" />
        Personel ekle
      </Button>
    </header>
  );
}

/* ---------------------------------------------------------------------------
   PERSONEL ADI FORMU (ekle + duzenle)
   --------------------------------------------------------------------------- */

function StaffNameForm({
  initialName,
  mode,
  onCancel,
  onSubmit,
  submitting
}: {
  initialName: string;
  mode: "create" | "edit";
  onCancel: () => void;
  onSubmit: (displayName: string) => void;
  submitting: boolean;
}) {
  const [name, setName] = useState(initialName);
  const trimmed = name.trim();
  const tooShort = trimmed.length < NAME_MIN;
  const tooLong = trimmed.length > NAME_MAX;
  const invalid = tooShort || tooLong;

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (invalid || submitting) {
      return;
    }
    onSubmit(trimmed);
  }

  return (
    <form className="space-y-5" onSubmit={handleSubmit}>
      <div className="space-y-2">
        <Label htmlFor="personel-adi">Görünen ad</Label>
        <Input
          autoComplete="off"
          autoFocus
          className="min-h-11"
          id="personel-adi"
          maxLength={NAME_MAX}
          onChange={(event) => setName(event.target.value)}
          placeholder="Örn. Ayşe Usta"
          value={name}
        />
        {/* Bilgi GORUNUR etikette; tooltip degil. */}
        <p
          className={cn(
            "text-sm",
            tooLong ? "text-destructive" : "text-muted-foreground"
          )}
        >
          {tooLong
            ? `En fazla ${NAME_MAX} karakter.`
            : `Müşterilerin ve takvimin göreceği ad. En az ${NAME_MIN} karakter.`}
        </p>
      </div>

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
        <Button className="min-h-11" disabled={invalid || submitting} type="submit">
          {submitting
            ? "Kaydediliyor…"
            : mode === "edit"
              ? "Kaydet"
              : "Personeli ekle"}
        </Button>
      </div>
    </form>
  );
}

/* ---------------------------------------------------------------------------
   STATU ROZETI -- renk TEK sinyal degil, METIN de tasir
   --------------------------------------------------------------------------- */

function StaffStatusBadge({ status }: { status: string | null | undefined }) {
  const view = describeStaffStatus(status);
  return (
    <Badge variant={view.tone === "active" ? "secondary" : "outline"}>
      {view.label}
    </Badge>
  );
}

/* ---------------------------------------------------------------------------
   PERSONEL LISTESI -- tablo (md+) / kart (<768px)
   --------------------------------------------------------------------------- */

function StaffList({
  branchTimeZoneId,
  expanded,
  onAdd,
  onArchive,
  onEdit,
  onToggle,
  selectedBranchId,
  staff,
  status,
  tenantId
}: {
  branchTimeZoneId: string;
  expanded: ReadonlySet<string>;
  selectedBranchId: string;
  staff: BusinessStaff[];
  status: "idle" | "loading" | "ready" | "error";
  tenantId: string;
  onAdd: () => void;
  onArchive: (staff: BusinessStaff) => void;
  onEdit: (staff: BusinessStaff) => void;
  onToggle: (staffId: string) => void;
}) {
  if (!selectedBranchId) {
    return (
      <Card>
        <CardContent className="py-14 text-center text-sm text-muted-foreground">
          Ekibini görmek için yukarıdan bir şube seç.
        </CardContent>
      </Card>
    );
  }

  if (status === "loading" || status === "idle") {
    return (
      <div className="space-y-3">
        {[0, 1, 2].map((row) => (
          <Skeleton className="h-16 w-full rounded-xl" key={row} />
        ))}
      </div>
    );
  }

  if (status === "error") {
    return (
      <Card>
        <CardContent className="flex flex-col items-center gap-2 py-14 text-center">
          <TriangleAlert aria-hidden className="size-6 text-muted-foreground" />
          <p className="text-sm text-muted-foreground">
            Ekip listesi yüklenemedi. Sayfayı yenileyip tekrar dene.
          </p>
        </CardContent>
      </Card>
    );
  }

  if (staff.length === 0) {
    return (
      <Card>
        <CardContent className="flex flex-col items-center gap-4 py-14 text-center">
          <div className="flex size-14 items-center justify-center rounded-full bg-muted">
            <Users aria-hidden className="size-6 text-muted-foreground" />
          </div>
          <div className="space-y-1">
            <h2 className="text-lg font-semibold">Bu şubede henüz personel yok</h2>
            <p className="text-sm text-muted-foreground">
              İlk personelini ekle; randevular onun takviminde görünsün.
            </p>
          </div>
          <Button className="min-h-11" onClick={onAdd}>
            <Plus aria-hidden className="size-4" />
            Personel ekle
          </Button>
        </CardContent>
      </Card>
    );
  }

  return (
    <>
      {/* MASAUSTU / TABLET: tablo */}
      <Card className="hidden overflow-hidden py-0 md:block">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-12" />
              <TableHead>Personel</TableHead>
              <TableHead>Durum</TableHead>
              <TableHead className="w-12 text-right">
                <span className="sr-only">İşlemler</span>
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {staff.map((entry) => {
              const isOpen = expanded.has(entry.id ?? "");
              const view = describeStaffStatus(entry.status);

              return (
                <Fragment key={entry.id}>
                  <TableRow>
                    <TableCell>
                      <Button
                        aria-controls={`izin-${entry.id}`}
                        aria-expanded={isOpen}
                        className="size-11"
                        onClick={() => onToggle(entry.id ?? "")}
                        size="icon"
                        variant="ghost"
                      >
                        <ChevronDown
                          aria-hidden
                          className={cn(
                            "size-4 transition-transform",
                            isOpen && "rotate-180"
                          )}
                        />
                        <span className="sr-only">
                          {isOpen
                            ? `${entry.displayName} izinlerini kapat`
                            : `${entry.displayName} izinlerini aç`}
                        </span>
                      </Button>
                    </TableCell>
                    <TableCell className="font-medium">{entry.displayName}</TableCell>
                    <TableCell>
                      <StaffStatusBadge status={entry.status} />
                    </TableCell>
                    <TableCell className="text-right">
                      <StaffActions
                        isArchived={view.isArchived}
                        onArchive={() => onArchive(entry)}
                        onEdit={() => onEdit(entry)}
                      />
                    </TableCell>
                  </TableRow>

                  {isOpen ? (
                    <TableRow className="hover:bg-transparent">
                      <TableCell className="bg-muted/40 p-0" colSpan={4}>
                        <div className="p-4" id={`izin-${entry.id}`}>
                          <StaffLeavePanel
                            branchTimeZoneId={branchTimeZoneId}
                            staffMemberId={entry.id ?? ""}
                            tenantId={tenantId}
                          />
                        </div>
                      </TableCell>
                    </TableRow>
                  ) : null}
                </Fragment>
              );
            })}
          </TableBody>
        </Table>
      </Card>

      {/* <768px: tablo yerine KART listesi */}
      <div className="space-y-3 md:hidden">
        {staff.map((entry) => {
          const isOpen = expanded.has(entry.id ?? "");
          const view = describeStaffStatus(entry.status);

          return (
            <Card key={entry.id}>
              <CardContent className="space-y-3 p-4">
                <div className="flex items-start justify-between gap-2">
                  <button
                    aria-controls={`izin-mobil-${entry.id}`}
                    aria-expanded={isOpen}
                    className="flex min-h-11 flex-1 items-start gap-2 text-left"
                    onClick={() => onToggle(entry.id ?? "")}
                    type="button"
                  >
                    <ChevronDown
                      aria-hidden
                      className={cn(
                        "mt-0.5 size-4 shrink-0 transition-transform",
                        isOpen && "rotate-180"
                      )}
                    />
                    <span className="min-w-0">
                      <span className="block font-medium">{entry.displayName}</span>
                      <span className="mt-1 block">
                        <StaffStatusBadge status={entry.status} />
                      </span>
                    </span>
                  </button>
                  <StaffActions
                    isArchived={view.isArchived}
                    onArchive={() => onArchive(entry)}
                    onEdit={() => onEdit(entry)}
                  />
                </div>

                {isOpen ? (
                  <div className="border-t pt-3" id={`izin-mobil-${entry.id}`}>
                    <StaffLeavePanel
                      branchTimeZoneId={branchTimeZoneId}
                      staffMemberId={entry.id ?? ""}
                      tenantId={tenantId}
                    />
                  </div>
                ) : null}
              </CardContent>
            </Card>
          );
        })}
      </div>
    </>
  );
}

/* ---------------------------------------------------------------------------
   PERSONEL AKSIYONLARI
   --------------------------------------------------------------------------- */

function StaffActions({
  isArchived,
  onArchive,
  onEdit
}: {
  isArchived: boolean;
  onArchive: () => void;
  onEdit: () => void;
}) {
  // Arsivli personelde yeniden ad/arsiv aksiyonu YOK: geri alma ucu olmadigi icin
  // arsivi tekrar sunmak yaniltir. Satir yine acilip izinleri gorunebilir.
  if (isArchived) {
    return null;
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button className="size-11" size="icon" variant="ghost">
          <MoreHorizontal aria-hidden className="size-4" />
          <span className="sr-only">Personel işlemleri</span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem className="min-h-11" onSelect={onEdit}>
          <Pencil aria-hidden className="size-4" />
          Adı düzenle
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
  );
}

/**
 * ARSIVLEME ONAYI.
 *
 * URUN RISKI (Tuzak 6): backend ArchiveAsync gelecekteki randevu kontrolu YAPMIYOR.
 * Gelecek randevusu olan personel sorgusuz arsivlenir, o randevular sahipsiz kalabilir.
 * Bu yuzden onay metninde bu GORUNUR sekilde yazili -- kullanici bilerek onaylasin.
 */
function ArchiveStaffDialog({
  onCancel,
  onConfirm,
  staff,
  submitting
}: {
  staff: BusinessStaff | null;
  submitting: boolean;
  onCancel: () => void;
  onConfirm: () => void;
}) {
  return (
    <AlertDialog onOpenChange={(open) => !open && onCancel()} open={staff !== null}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>
            &quot;{staff?.displayName}&quot; arşivlensin mi?
          </AlertDialogTitle>
          <AlertDialogDescription>
            Arşivlenen personel ekip listesinde &quot;Arşivli&quot; olarak kalır ve
            geri alınamaz. <strong>Bu personelin gelecekteki randevuları varsa
            arşivleme onları etkileyebilir</strong> — önce takvimini kontrol et.
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
              // AlertDialogAction varsayilan kapanir; istek bitene kadar acik kalsin.
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
   IZIN / MUSAIT DEGIL PANELI -- OKUNABILIR uc (Tuzak 5)
   --------------------------------------------------------------------------- */

function StaffLeavePanel({
  branchTimeZoneId,
  staffMemberId,
  tenantId
}: {
  branchTimeZoneId: string;
  staffMemberId: string;
  tenantId: string;
}) {
  const router = useRouter();
  const [items, setItems] = useState<StaffUnavailable[]>([]);
  const [status, setStatus] = useState<"loading" | "ready" | "error">("loading");
  const [showForm, setShowForm] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<StaffUnavailable | null>(null);

  // Form alanlari: datetime-local degerleri SUBE SAAT DILIMINDE yorumlanir.
  const [startLocal, setStartLocal] = useState("");
  const [endLocal, setEndLocal] = useState("");
  const [reason, setReason] = useState("");

  const load = useCallback(async () => {
    setStatus("loading");
    const result = await listUnavailable(tenantId, staffMemberId);

    if (result.kind !== "success") {
      setStatus("error");
      return;
    }

    setItems(result.data ?? []);
    setStatus("ready");
  }, [tenantId, staffMemberId]);

  // Panel yalnizca satir ACILINCA mount olur -> burada tembel yukleme dogru yer.
  useEffect(() => {
    void load();
  }, [load]);

  function resetForm() {
    setStartLocal("");
    setEndLocal("");
    setReason("");
  }

  async function submit(event: FormEvent) {
    event.preventDefault();

    const startUtc = parseBranchDateTimeLocalValue(startLocal, branchTimeZoneId);
    const endUtc = parseBranchDateTimeLocalValue(endLocal, branchTimeZoneId);

    if (!startUtc || !endUtc) {
      toast.error("Başlangıç ve bitiş için geçerli bir tarih-saat gir.");
      return;
    }

    // Client tarafi da kontrol eder ki gereksiz istek atmayalim (backend de reddeder).
    if (new Date(endUtc).getTime() <= new Date(startUtc).getTime()) {
      toast.error("Bitiş zamanı başlangıçtan sonra olmalı.");
      return;
    }

    setSubmitting(true);
    const result = await createUnavailable(tenantId, staffMemberId, {
      startUtc,
      endUtc,
      reason: reason.trim()
    });
    setSubmitting(false);

    if (result.kind !== "success") {
      if (result.kind === "rejected") {
        toast.error(result.message);
        router.refresh();
      } else {
        toast.error(result.message);
      }
      return;
    }

    const saved = result.data;
    if (saved !== null) {
      setItems((current) =>
        [...current, saved].sort(
          (a, b) =>
            new Date(a.startUtc ?? "").getTime() - new Date(b.startUtc ?? "").getTime()
        )
      );
    } else {
      void load();
    }

    toast.success("İzin eklendi.");
    resetForm();
    setShowForm(false);
    router.refresh();
  }

  async function confirmDelete() {
    if (!deleteTarget) {
      return;
    }

    setSubmitting(true);
    const result = await deleteUnavailable(
      tenantId,
      staffMemberId,
      deleteTarget.id ?? ""
    );
    setSubmitting(false);

    if (result.kind !== "success") {
      if (result.kind === "rejected") {
        toast.error(result.message);
        router.refresh();
      } else {
        toast.error(result.message);
      }
      setDeleteTarget(null);
      return;
    }

    setItems((current) => current.filter((item) => item.id !== deleteTarget.id));
    toast.success("İzin silindi.");
    setDeleteTarget(null);
    router.refresh();
  }

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between gap-2">
        <div>
          <h3 className="flex items-center gap-2 text-sm font-medium">
            <CalendarOff aria-hidden className="size-4" />
            İzin / müsait değil
          </h3>
          <p className="mt-0.5 text-xs text-muted-foreground">
            Saatler şube saat dilimine göre ({branchTimeZoneId}).
          </p>
        </div>
        {!showForm ? (
          <Button
            className="min-h-11"
            onClick={() => setShowForm(true)}
            size="sm"
            variant="outline"
          >
            <Plus aria-hidden className="size-4" />
            İzin ekle
          </Button>
        ) : null}
      </div>

      {showForm ? (
        <form
          className="space-y-4 rounded-md border bg-background p-4"
          onSubmit={submit}
        >
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor={`baslangic-${staffMemberId}`}>Başlangıç</Label>
              <Input
                className="min-h-11"
                id={`baslangic-${staffMemberId}`}
                onChange={(event) => setStartLocal(event.target.value)}
                required
                type="datetime-local"
                value={startLocal}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor={`bitis-${staffMemberId}`}>Bitiş</Label>
              <Input
                className="min-h-11"
                id={`bitis-${staffMemberId}`}
                onChange={(event) => setEndLocal(event.target.value)}
                required
                type="datetime-local"
                value={endLocal}
              />
            </div>
          </div>
          <div className="space-y-2">
            <Label htmlFor={`sebep-${staffMemberId}`}>Sebep (isteğe bağlı)</Label>
            <Input
              className="min-h-11"
              id={`sebep-${staffMemberId}`}
              maxLength={REASON_MAX}
              onChange={(event) => setReason(event.target.value)}
              placeholder="Örn. Yıllık izin"
              type="text"
              value={reason}
            />
          </div>
          <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
            <Button
              className="min-h-11"
              disabled={submitting}
              onClick={() => {
                resetForm();
                setShowForm(false);
              }}
              type="button"
              variant="outline"
            >
              Vazgeç
            </Button>
            <Button className="min-h-11" disabled={submitting} type="submit">
              {submitting ? "Ekleniyor…" : "İzni ekle"}
            </Button>
          </div>
        </form>
      ) : null}

      {status === "loading" ? (
        <div className="space-y-2">
          <Skeleton className="h-12 w-full rounded-md" />
          <Skeleton className="h-12 w-full rounded-md" />
        </div>
      ) : status === "error" ? (
        <p className="flex items-center gap-2 py-2 text-sm text-muted-foreground">
          <TriangleAlert aria-hidden className="size-4" />
          İzinler yüklenemedi. Sayfayı yenileyip tekrar dene.
        </p>
      ) : items.length === 0 ? (
        <p className="rounded-md border border-dashed px-3 py-6 text-center text-sm text-muted-foreground">
          Bu personelin girilmiş bir izni yok.
        </p>
      ) : (
        <ul className="space-y-2">
          {items.map((item) => (
            <li
              className="flex flex-col gap-3 rounded-md border bg-background p-3 sm:flex-row sm:items-center sm:justify-between"
              key={item.id}
            >
              <div className="min-w-0">
                <p className="text-sm font-medium">
                  {formatBranchDateTime(item.startUtc ?? "", branchTimeZoneId)}
                  {" – "}
                  {formatBranchDateTime(item.endUtc ?? "", branchTimeZoneId)}
                </p>
                <p className="text-sm text-muted-foreground">
                  {item.reason?.trim() ? item.reason : "Sebep belirtilmemiş"}
                </p>
              </div>
              <Button
                className="min-h-11 shrink-0 text-destructive hover:text-destructive"
                onClick={() => setDeleteTarget(item)}
                size="sm"
                variant="outline"
              >
                <Trash2 aria-hidden className="size-4" />
                Sil
              </Button>
            </li>
          ))}
        </ul>
      )}

      <AlertDialog
        onOpenChange={(open) => !open && setDeleteTarget(null)}
        open={deleteTarget !== null}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>İzin silinsin mi?</AlertDialogTitle>
            <AlertDialogDescription>
              {deleteTarget
                ? `${formatBranchDateTime(
                    deleteTarget.startUtc ?? "",
                    branchTimeZoneId
                  )} – ${formatBranchDateTime(
                    deleteTarget.endUtc ?? "",
                    branchTimeZoneId
                  )} aralığındaki izin kaydı silinecek. Bu personel o aralıkta yeniden randevuya açılır.`
                : ""}
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
                event.preventDefault();
                void confirmDelete();
              }}
            >
              {submitting ? "Siliniyor…" : "İzni sil"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
