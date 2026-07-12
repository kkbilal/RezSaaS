"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import {
  ChevronDown,
  MoreHorizontal,
  Pencil,
  Plus,
  Scissors,
  Trash2,
  TriangleAlert
} from "lucide-react";
import { useRouter } from "next/navigation";
import { Fragment, useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";
import {
  archiveService,
  createService,
  createVariant,
  deleteVariant,
  updateService,
  updateVariant,
  type CatalogResult
} from "@/features/business/api/business-service-client";
import {
  canArchiveService,
  CATEGORY_MAX,
  CATEGORY_MIN,
  describeServiceStatus,
  formatDuration,
  formatPrice,
  formatPriceRange,
  NAME_MAX,
  NAME_MIN,
  type CatalogResourceType,
  type CatalogVariant,
  type ServiceFormValues,
  type ServiceWithVariants,
  type VariantFormValues
} from "@/features/business/lib/service-catalog";
import { ServiceVariantForm } from "@/features/business/components/service-variant-form";
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
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle
} from "@/components/ui/sheet";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow
} from "@/components/ui/table";
import { useIsMobile } from "@/shared/hooks/use-mobile";
import { cn } from "@/shared/lib/cn";

/**
 * HIZMETLER VE FIYATLAR -- IKI SEVIYELI KATALOG.
 *
 * NEDEN ACILIR SATIR (ACCORDION), AYRI DETAY SAYFASI DEGIL:
 *
 * 1. Veri ZATEN elimizde. Ust listede "kac secenek" ve "fiyat araligi" sutunlarini
 *    cizebilmek icin her hizmetin varyantlarini sunucuda cekmek ZORUNDAYIZ (fiyat/sure
 *    Service'te degil ServiceVariant'ta; toplu donen bir uc yok). Dolayisiyla bir hizmeti
 *    ACMAK SIFIR EK ISTEK. Ayri detay sayfasi bu veriyi cope atip yeniden cekerdi.
 * 2. Isin kendisi KARSILASTIRMALI. "Fiyatlari yonet" demek "sac kesimi 400 ama boya 800,
 *    kesimi 450 yapayim" demektir. Fiyat araligi listesi ekranda kalmadan bu yapilamaz;
 *    detay sayfasi tam da o listeyi gizler.
 * 3. Kucuk yuzey. Ayri rota => yeni page.tsx + nav-manifest'te hidden kayit + izin
 *    tablosunda yeni satir + URL ile girilebilen yeni bir yikici yuzey. Bir salonun
 *    hizmet basina 2-4 secenegi icin bu bedel bosuna.
 * 4. Duzgun kuculur. <768px'te hizmet satiri KARTA, acilan bolum kart ici listeye duser.
 *    Master-detay (sol liste / sag panel) tablette ikiye bolunur, telefonda cokerdi.
 *
 * ARAYUZ DILI: kodda "variant", ekranda "seçenek". Salon sahibi "varyant" demez.
 */

/* ---------------------------------------------------------------------------
   HIZMET FORMU (ad + kategori)
   --------------------------------------------------------------------------- */

type ServiceFormShape = {
  categoryKey: string;
  name: string;
};

const serviceSchema = z.object({
  categoryKey: z
    .string()
    .trim()
    .min(CATEGORY_MIN, `Kategori en az ${CATEGORY_MIN} karakter olmalı.`)
    .max(CATEGORY_MAX, `Kategori en fazla ${CATEGORY_MAX} karakter olabilir.`),
  name: z
    .string()
    .trim()
    .min(NAME_MIN, `Hizmet adı en az ${NAME_MIN} karakter olmalı.`)
    .max(NAME_MAX, `Hizmet adı en fazla ${NAME_MAX} karakter olabilir.`)
});

type ServiceFormProps = {
  service?: ServiceWithVariants | null;
  submitting: boolean;
  onCancel: () => void;
  onSubmit: (values: ServiceFormValues) => void;
};

function ServiceForm({ onCancel, onSubmit, service, submitting }: ServiceFormProps) {
  const isEdit = Boolean(service);

  const form = useForm<ServiceFormShape>({
    defaultValues: {
      categoryKey: service?.categoryKey ?? "",
      name: service?.name ?? ""
    },
    resolver: zodResolver(serviceSchema)
  });

  return (
    <Form {...form}>
      <form
        className="space-y-5"
        onSubmit={form.handleSubmit((shape) =>
          // Ad ve kategori BIRLIKTE gider: servis PATCH'i de ikisini kosulsuz uygular.
          onSubmit({ categoryKey: shape.categoryKey, name: shape.name })
        )}
      >
        <FormField
          control={form.control}
          name="name"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Hizmet adı</FormLabel>
              <FormControl>
                <Input
                  autoComplete="off"
                  className="min-h-11"
                  placeholder="Örn. Saç kesimi"
                  {...field}
                />
              </FormControl>
              <FormDescription>
                Fiyat ve süre burada değil, hizmetin seçeneklerinde tanımlanır.
              </FormDescription>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="categoryKey"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Kategori</FormLabel>
              <FormControl>
                <Input
                  autoComplete="off"
                  className="min-h-11"
                  placeholder="Örn. Saç"
                  {...field}
                />
              </FormControl>
              <FormDescription>
                Benzer hizmetleri gruplamak için. Örn. &quot;Saç&quot;, &quot;Cilt&quot;.
              </FormDescription>
              <FormMessage />
            </FormItem>
          )}
        />

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
          <Button className="min-h-11" disabled={submitting} type="submit">
            {submitting ? "Kaydediliyor…" : isEdit ? "Kaydet" : "Hizmeti ekle"}
          </Button>
        </div>
      </form>
    </Form>
  );
}

/* ---------------------------------------------------------------------------
   YARDIMCI GORUNUMLER
   --------------------------------------------------------------------------- */

function StatusBadge({ status }: { status: string | null | undefined }) {
  const view = describeServiceStatus(status);

  // Rozet METIN tasir; renk TEK sinyal degil.
  return (
    <Badge variant={view.tone === "active" ? "secondary" : "outline"}>
      {view.label}
    </Badge>
  );
}

/** Bir hizmetin fiyat ozeti. Cekilemeyen varyant listesi "fiyat yok" gibi gosterilmez. */
function PriceSummary({ service }: { service: ServiceWithVariants }) {
  if (service.variantsUnavailable) {
    return (
      <span className="inline-flex items-center gap-1.5 text-sm text-muted-foreground">
        <TriangleAlert aria-hidden className="size-4" />
        Fiyatlar yüklenemedi
      </span>
    );
  }

  if (service.variants.length === 0) {
    return <span className="text-sm text-muted-foreground">Fiyat yok</span>;
  }

  return (
    <span className="text-sm font-medium tabular-nums">
      {formatPriceRange(service.variants)}
    </span>
  );
}

function VariantCountLabel({ service }: { service: ServiceWithVariants }) {
  if (service.variantsUnavailable) {
    return <span className="text-sm text-muted-foreground">—</span>;
  }

  return (
    <span className="text-sm text-muted-foreground">
      {service.variants.length} seçenek
    </span>
  );
}

/* ---------------------------------------------------------------------------
   ANA EKRAN
   --------------------------------------------------------------------------- */

type ServiceDialogState =
  | { mode: "create" }
  | { mode: "edit"; service: ServiceWithVariants }
  | null;

type VariantDialogState = {
  service: ServiceWithVariants;
  variant: CatalogVariant | null;
} | null;

type VariantDeleteState = {
  service: ServiceWithVariants;
  variant: CatalogVariant;
} | null;

export type BusinessServicesPageProps = {
  initialServices: ServiceWithVariants[];
  resourceTypes: CatalogResourceType[];
  tenantId: string;
};

export function BusinessServicesPage({
  initialServices,
  resourceTypes,
  tenantId
}: BusinessServicesPageProps) {
  const router = useRouter();
  const isMobile = useIsMobile();

  const [services, setServices] = useState(initialServices);
  const [expanded, setExpanded] = useState<ReadonlySet<string>>(new Set());
  const [submitting, setSubmitting] = useState(false);

  const [serviceDialog, setServiceDialog] = useState<ServiceDialogState>(null);
  const [variantDialog, setVariantDialog] = useState<VariantDialogState>(null);
  const [archiveTarget, setArchiveTarget] = useState<ServiceWithVariants | null>(null);
  const [variantDeleteTarget, setVariantDeleteTarget] = useState<VariantDeleteState>(null);

  function toggle(serviceId: string) {
    setExpanded((current) => {
      const next = new Set(current);

      if (next.has(serviceId)) {
        next.delete(serviceId);
      } else {
        next.add(serviceId);
      }

      return next;
    });
  }

  /**
   * Mutasyon sonucunu TEK yerde yorumlar.
   *
   * "rejected" (sunucu reddetti) ile "failed" (ag hatasi) ayrimi onemli: sunucu reddettiyse
   * yerel liste bayat olabilir -> router.refresh(). Ag hatasinda istek hic gitmemis olabilir,
   * refresh etmek kullanicinin yazdiklarini bosuna sifirlar.
   */
  function handleFailure(result: CatalogResult<unknown>): void {
    if (result.kind === "rejected") {
      toast.error(result.message);
      router.refresh();
      return;
    }

    if (result.kind === "failed") {
      toast.error(result.message);
    }
  }

  async function submitService(values: ServiceFormValues) {
    if (!serviceDialog) {
      return;
    }

    setSubmitting(true);

    const result =
      serviceDialog.mode === "edit"
        ? await updateService(tenantId, serviceDialog.service.id, values)
        : await createService(tenantId, values);

    setSubmitting(false);

    if (result.kind !== "success") {
      handleFailure(result);
      return;
    }

    const saved = result.data;

    // saved === null: yazma BASARILI ama govde beklenen alanlari tasimiyor. Yerel listeyi
    // tahminle yamamayiz; asagidaki router.refresh() gercegi sunucudan getirir.
    if (saved !== null) {
      if (serviceDialog.mode === "edit") {
        setServices((current) =>
          current.map((service) =>
            service.id === saved.id ? { ...service, ...saved } : service
          )
        );
      } else {
        setServices((current) => [
          ...current,
          { ...saved, variants: [], variantsUnavailable: false }
        ]);
        // Yeni hizmetin FIYATI YOK. Kullaniciyi bir sonraki adima itmek icin aciyoruz.
        setExpanded((current) => new Set(current).add(saved.id));
      }
    }

    toast.success(
      serviceDialog.mode === "edit"
        ? "Hizmet güncellendi."
        : "Hizmet eklendi. Şimdi bir seçenek ekleyip fiyat tanımla."
    );

    setServiceDialog(null);
    router.refresh();
  }

  async function submitVariant(values: VariantFormValues) {
    if (!variantDialog) {
      return;
    }

    const { service, variant } = variantDialog;

    setSubmitting(true);

    // Guncellemede de olusturmada da govdeyi business-service-client kurar:
    // bes alanin besi birden gider (uc "PATCH" adini tasir ama PUT gibi davranir).
    const result = variant
      ? await updateVariant(tenantId, service.id, variant.id, values)
      : await createVariant(tenantId, service.id, values);

    setSubmitting(false);

    if (result.kind !== "success") {
      handleFailure(result);
      return;
    }

    const saved = result.data;

    // saved === null: yazma basarili ama govde eksik -> yerel yamayi atla, refresh getirsin.
    if (saved !== null) {
      setServices((current) =>
        current.map((entry) => {
          if (entry.id !== service.id) {
            return entry;
          }

          const variants = variant
            ? entry.variants.map((item) => (item.id === saved.id ? saved : item))
            : [...entry.variants, saved];

          return { ...entry, variants };
        })
      );
    }

    toast.success(variant ? "Seçenek güncellendi." : "Seçenek eklendi.");
    setVariantDialog(null);
    router.refresh();
  }

  async function confirmVariantDelete() {
    if (!variantDeleteTarget) {
      return;
    }

    const { service, variant } = variantDeleteTarget;

    setSubmitting(true);
    const result = await deleteVariant(tenantId, service.id, variant.id);
    setSubmitting(false);

    if (result.kind !== "success") {
      handleFailure(result);
      setVariantDeleteTarget(null);
      return;
    }

    setServices((current) =>
      current.map((entry) =>
        entry.id === service.id
          ? {
              ...entry,
              variants: entry.variants.filter((item) => item.id !== variant.id)
            }
          : entry
      )
    );

    toast.success("Seçenek silindi.");
    setVariantDeleteTarget(null);
    router.refresh();
  }

  async function confirmArchive() {
    if (!archiveTarget) {
      return;
    }

    setSubmitting(true);
    const result = await archiveService(tenantId, archiveTarget.id);
    setSubmitting(false);

    if (result.kind !== "success") {
      handleFailure(result);
      setArchiveTarget(null);
      return;
    }

    // Uc "archive" deniyor ama backend satiri KALDIRIYOR -> listeden de dusuyoruz.
    setServices((current) =>
      current.filter((service) => service.id !== archiveTarget.id)
    );
    toast.success("Hizmet arşivlendi.");
    setArchiveTarget(null);
    router.refresh();
  }

  // key: react-hook-form defaultValues'i YALNIZCA mount'ta okur. Form ornegi iki farkli
  // hedef arasinda yeniden kullanilirsa (A'yi duzenle -> kapat -> B'yi duzenle) alanlar
  // A'nin degerlerinde takili kalir ve kullanici B'yi A'nin fiyatiyla kaydeder.
  // Hedef degisince key degisir -> taze mount -> taze defaultValues.
  const variantFormNode = variantDialog ? (
    <ServiceVariantForm
      key={variantDialog.variant?.id ?? `yeni-${variantDialog.service.id}`}
      onCancel={() => setVariantDialog(null)}
      onSubmit={submitVariant}
      resourceTypes={resourceTypes}
      submitting={submitting}
      variant={variantDialog.variant}
    />
  ) : null;

  const serviceFormNode = serviceDialog ? (
    <ServiceForm
      key={serviceDialog.mode === "edit" ? serviceDialog.service.id : "yeni"}
      onCancel={() => setServiceDialog(null)}
      onSubmit={submitService}
      service={serviceDialog.mode === "edit" ? serviceDialog.service : null}
      submitting={submitting}
    />
  ) : null;

  const variantDialogTitle = variantDialog?.variant
    ? "Seçeneği düzenle"
    : "Seçenek ekle";
  const variantDialogDescription = variantDialog
    ? `${variantDialog.service.name} · süre ve fiyat bu seçenekte tanımlanır.`
    : "";

  const serviceDialogTitle =
    serviceDialog?.mode === "edit" ? "Hizmeti düzenle" : "Hizmet ekle";

  return (
    <div className="space-y-6">
      <header className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-3xl font-semibold tracking-tight">
            Hizmetler ve fiyatlar
          </h1>
          <p className="mt-2 max-w-2xl text-sm text-muted-foreground">
            Her hizmetin bir veya daha fazla seçeneği olur. Süre ve fiyat
            seçenekte tanımlanır — örneğin &quot;Saç kesimi&quot; hizmetinin
            &quot;Kısa saç&quot; ve &quot;Uzun saç&quot; seçenekleri.
          </p>
        </div>
        <Button
          className="min-h-11 shrink-0"
          onClick={() => setServiceDialog({ mode: "create" })}
        >
          <Plus aria-hidden className="size-4" />
          Hizmet ekle
        </Button>
      </header>

      {services.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center gap-4 py-14 text-center">
            <div className="flex size-14 items-center justify-center rounded-full bg-muted">
              <Scissors aria-hidden className="size-6 text-muted-foreground" />
            </div>
            <div className="space-y-1">
              <h2 className="text-lg font-semibold">Henüz hizmet yok</h2>
              <p className="text-sm text-muted-foreground">
                İlk hizmetini ekle, sonra süresini ve fiyatını seçenek olarak tanımla.
              </p>
            </div>
            <Button
              className="min-h-11"
              onClick={() => setServiceDialog({ mode: "create" })}
            >
              <Plus aria-hidden className="size-4" />
              Hizmet ekle
            </Button>
          </CardContent>
        </Card>
      ) : (
        <>
          {/* MASAUSTU / TABLET: tablo */}
          <Card className="hidden overflow-hidden py-0 md:block">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-12" />
                  <TableHead>Hizmet</TableHead>
                  <TableHead>Kategori</TableHead>
                  <TableHead>Seçenek</TableHead>
                  <TableHead>Fiyat aralığı</TableHead>
                  <TableHead>Durum</TableHead>
                  <TableHead className="w-12 text-right">
                    <span className="sr-only">İşlemler</span>
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {services.map((service) => {
                  const isOpen = expanded.has(service.id);

                  return (
                    <Fragment key={service.id}>
                      <TableRow>
                        <TableCell>
                          <Button
                            aria-controls={`secenekler-${service.id}`}
                            aria-expanded={isOpen}
                            className="size-11"
                            onClick={() => toggle(service.id)}
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
                                ? `${service.name} seçeneklerini kapat`
                                : `${service.name} seçeneklerini aç`}
                            </span>
                          </Button>
                        </TableCell>
                        <TableCell className="font-medium">{service.name}</TableCell>
                        <TableCell className="text-muted-foreground">
                          {service.categoryKey}
                        </TableCell>
                        <TableCell>
                          <VariantCountLabel service={service} />
                        </TableCell>
                        <TableCell>
                          <PriceSummary service={service} />
                        </TableCell>
                        <TableCell>
                          <StatusBadge status={service.status} />
                        </TableCell>
                        <TableCell className="text-right">
                          <ServiceActions
                            onArchive={() => setArchiveTarget(service)}
                            onEdit={() => setServiceDialog({ mode: "edit", service })}
                          />
                        </TableCell>
                      </TableRow>

                      {isOpen ? (
                        <TableRow className="hover:bg-transparent">
                          <TableCell className="bg-muted/40 p-0" colSpan={7}>
                            <div className="p-4" id={`secenekler-${service.id}`}>
                              <VariantPanel
                                onAdd={() =>
                                  setVariantDialog({ service, variant: null })
                                }
                                onDelete={(variant) =>
                                  setVariantDeleteTarget({ service, variant })
                                }
                                onEdit={(variant) =>
                                  setVariantDialog({ service, variant })
                                }
                                resourceTypes={resourceTypes}
                                service={service}
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
            {services.map((service) => {
              const isOpen = expanded.has(service.id);

              return (
                <Card key={service.id}>
                  <CardContent className="space-y-3 p-4">
                    <div className="flex items-start justify-between gap-2">
                      <button
                        aria-controls={`secenekler-mobil-${service.id}`}
                        aria-expanded={isOpen}
                        className="flex min-h-11 flex-1 items-start gap-2 text-left"
                        onClick={() => toggle(service.id)}
                        type="button"
                      >
                        <ChevronDown
                          aria-hidden
                          className={cn(
                            "mt-0.5 size-4 shrink-0 transition-transform",
                            isOpen && "rotate-180"
                          )}
                        />
                        <span>
                          <span className="block font-medium">{service.name}</span>
                          <span className="block text-sm text-muted-foreground">
                            {service.categoryKey}
                          </span>
                        </span>
                      </button>
                      <ServiceActions
                        onArchive={() => setArchiveTarget(service)}
                        onEdit={() => setServiceDialog({ mode: "edit", service })}
                      />
                    </div>

                    <div className="flex flex-wrap items-center gap-x-3 gap-y-2">
                      <StatusBadge status={service.status} />
                      <VariantCountLabel service={service} />
                      <PriceSummary service={service} />
                    </div>

                    {isOpen ? (
                      <div
                        className="border-t pt-3"
                        id={`secenekler-mobil-${service.id}`}
                      >
                        <VariantPanel
                          onAdd={() => setVariantDialog({ service, variant: null })}
                          onDelete={(variant) =>
                            setVariantDeleteTarget({ service, variant })
                          }
                          onEdit={(variant) => setVariantDialog({ service, variant })}
                          resourceTypes={resourceTypes}
                          service={service}
                        />
                      </div>
                    ) : null}
                  </CardContent>
                </Card>
              );
            })}
          </div>
        </>
      )}

      {/* HIZMET FORMU -- masaustunde Dialog, mobilde Sheet */}
      {isMobile ? (
        <Sheet
          onOpenChange={(open) => !open && setServiceDialog(null)}
          open={serviceDialog !== null}
        >
          <SheetContent className="overflow-y-auto" side="bottom">
            <SheetHeader>
              <SheetTitle>{serviceDialogTitle}</SheetTitle>
              <SheetDescription>
                Fiyat ve süre hizmette değil, seçeneklerde tanımlanır.
              </SheetDescription>
            </SheetHeader>
            <div className="px-4 pb-4">{serviceFormNode}</div>
          </SheetContent>
        </Sheet>
      ) : (
        <Dialog
          onOpenChange={(open) => !open && setServiceDialog(null)}
          open={serviceDialog !== null}
        >
          <DialogContent>
            <DialogHeader>
              <DialogTitle>{serviceDialogTitle}</DialogTitle>
              <DialogDescription>
                Fiyat ve süre hizmette değil, seçeneklerde tanımlanır.
              </DialogDescription>
            </DialogHeader>
            {serviceFormNode}
          </DialogContent>
        </Dialog>
      )}

      {/* SECENEK FORMU -- masaustunde Dialog, mobilde Sheet */}
      {isMobile ? (
        <Sheet
          onOpenChange={(open) => !open && setVariantDialog(null)}
          open={variantDialog !== null}
        >
          <SheetContent className="overflow-y-auto" side="bottom">
            <SheetHeader>
              <SheetTitle>{variantDialogTitle}</SheetTitle>
              <SheetDescription>{variantDialogDescription}</SheetDescription>
            </SheetHeader>
            <div className="px-4 pb-4">{variantFormNode}</div>
          </SheetContent>
        </Sheet>
      ) : (
        <Dialog
          onOpenChange={(open) => !open && setVariantDialog(null)}
          open={variantDialog !== null}
        >
          <DialogContent>
            <DialogHeader>
              <DialogTitle>{variantDialogTitle}</DialogTitle>
              <DialogDescription>{variantDialogDescription}</DialogDescription>
            </DialogHeader>
            {variantFormNode}
          </DialogContent>
        </Dialog>
      )}

      <ArchiveServiceDialog
        onCancel={() => setArchiveTarget(null)}
        onConfirm={confirmArchive}
        service={archiveTarget}
        submitting={submitting}
      />

      <AlertDialog
        onOpenChange={(open) => !open && setVariantDeleteTarget(null)}
        open={variantDeleteTarget !== null}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              &quot;{variantDeleteTarget?.variant.name}&quot; seçeneği silinsin mi?
            </AlertDialogTitle>
            <AlertDialogDescription>
              Bu seçenek kalıcı olarak silinir ve müşteriler artık bunu seçemez.
              Alınmış randevular etkilenmez.
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
                // AlertDialogAction varsayilan olarak kapanir; istek bitene kadar acik kalsin.
                event.preventDefault();
                void confirmVariantDelete();
              }}
            >
              {submitting ? "Siliniyor…" : "Seçeneği sil"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}

/* ---------------------------------------------------------------------------
   HIZMET AKSIYONLARI
   --------------------------------------------------------------------------- */

function ServiceActions({
  onArchive,
  onEdit
}: {
  onArchive: () => void;
  onEdit: () => void;
}) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button className="size-11" size="icon" variant="ghost">
          <MoreHorizontal aria-hidden className="size-4" />
          <span className="sr-only">Hizmet işlemleri</span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem className="min-h-11" onSelect={onEdit}>
          <Pencil aria-hidden className="size-4" />
          Düzenle
        </DropdownMenuItem>
        {/* "Sil" YOK: backend'de servis icin DELETE ucu yok, yalnizca archive var. */}
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
 * Uc "archive" adini tasir ama backend hizmeti KALICI OLARAK SILER ve varyanti olani
 * 409 ile reddeder. Onay metni bu gercegi soyler; on kosul saglanmiyorsa YIKICI BUTON
 * HIC GOSTERILMEZ (pasif buton + tooltip degil -- dokunmatikte tooltip okunmaz,
 * bilgi gorunur metinde durur).
 */
function ArchiveServiceDialog({
  onCancel,
  onConfirm,
  service,
  submitting
}: {
  service: ServiceWithVariants | null;
  submitting: boolean;
  onCancel: () => void;
  onConfirm: () => void;
}) {
  const blockedByUnknownVariants = service?.variantsUnavailable ?? false;
  const variantCount = service?.variants.length ?? 0;
  const allowed =
    service !== null && !blockedByUnknownVariants && canArchiveService(variantCount);

  return (
    <AlertDialog onOpenChange={(open) => !open && onCancel()} open={service !== null}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>
            {allowed
              ? `"${service?.name}" arşivlensin mi?`
              : "Önce seçenekleri sil"}
          </AlertDialogTitle>
          <AlertDialogDescription>
            {blockedByUnknownVariants ? (
              <>
                Bu hizmetin seçenekleri yüklenemediği için arşivleme güvenli değil.
                Sayfayı yenileyip tekrar dene.
              </>
            ) : allowed ? (
              <>
                Bu hizmet kalıcı olarak kaldırılır ve <strong>geri alınamaz</strong>.
                Alınmış randevular etkilenmez.
              </>
            ) : (
              <>
                &quot;{service?.name}&quot; hizmetinin {variantCount} seçeneği var.
                Seçenekleri olan bir hizmet arşivlenemez. Önce tüm seçenekleri sil,
                sonra hizmeti arşivle.
              </>
            )}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel className="min-h-11" disabled={submitting}>
            {allowed ? "Vazgeç" : "Tamam"}
          </AlertDialogCancel>
          {allowed ? (
            <AlertDialogAction
              className="min-h-11 bg-destructive text-white hover:bg-destructive/90"
              disabled={submitting}
              onClick={(event) => {
                event.preventDefault();
                onConfirm();
              }}
            >
              {submitting ? "Arşivleniyor…" : "Arşivle"}
            </AlertDialogAction>
          ) : null}
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}

/* ---------------------------------------------------------------------------
   IKINCI SEVIYE: SECENEKLER (fiyat ve sure burada)
   --------------------------------------------------------------------------- */

function VariantPanel({
  onAdd,
  onDelete,
  onEdit,
  resourceTypes,
  service
}: {
  resourceTypes: readonly CatalogResourceType[];
  service: ServiceWithVariants;
  onAdd: () => void;
  onDelete: (variant: CatalogVariant) => void;
  onEdit: (variant: CatalogVariant) => void;
}) {
  function resourceTypeLabel(variant: CatalogVariant) {
    if (!variant.requiredResourceTypeId) {
      return "Ekipman gerekmiyor";
    }

    const match = resourceTypes.find(
      (type) => type.id === variant.requiredResourceTypeId
    );

    // Tur listede yoksa (silinmis ya da cekilemedi) uydurmayiz: bilinmedigini soyleriz.
    return match?.displayName ?? "Bilinmeyen tür";
  }

  if (service.variantsUnavailable) {
    return (
      <p className="flex items-center gap-2 py-2 text-sm text-muted-foreground">
        <TriangleAlert aria-hidden className="size-4" />
        Bu hizmetin seçenekleri yüklenemedi. Sayfayı yenileyip tekrar dene.
      </p>
    );
  }

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between gap-2">
        <h3 className="text-sm font-medium">Seçenekler ve fiyatlar</h3>
        <Button className="min-h-11" onClick={onAdd} size="sm" variant="outline">
          <Plus aria-hidden className="size-4" />
          Seçenek ekle
        </Button>
      </div>

      {service.variants.length === 0 ? (
        <p className="rounded-md border border-dashed px-3 py-6 text-center text-sm text-muted-foreground">
          Bu hizmetin henüz seçeneği yok, bu yüzden <strong>fiyatı da yok</strong>.
          Süre ve fiyat tanımlamak için bir seçenek ekle.
        </p>
      ) : (
        <ul className="space-y-2">
          {service.variants.map((variant) => (
            <li
              className="flex flex-col gap-3 rounded-md border bg-background p-3 sm:flex-row sm:items-center sm:justify-between"
              key={variant.id}
            >
              <div className="min-w-0">
                <p className="truncate font-medium">{variant.name}</p>
                <p className="text-sm text-muted-foreground">
                  <span className="tabular-nums">
                    {formatDuration(variant.durationMinutes)}
                  </span>
                  {" · "}
                  <span className="tabular-nums font-medium text-foreground">
                    {formatPrice(variant.priceAmount, variant.currencyCode)}
                  </span>
                  {" · "}
                  {resourceTypeLabel(variant)}
                </p>
              </div>

              <div className="flex shrink-0 gap-2">
                <Button
                  className="min-h-11 flex-1 sm:flex-none"
                  onClick={() => onEdit(variant)}
                  size="sm"
                  variant="outline"
                >
                  <Pencil aria-hidden className="size-4" />
                  Düzenle
                </Button>
                <Button
                  className="min-h-11 flex-1 text-destructive hover:text-destructive sm:flex-none"
                  onClick={() => onDelete(variant)}
                  size="sm"
                  variant="outline"
                >
                  <Trash2 aria-hidden className="size-4" />
                  Sil
                </Button>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
