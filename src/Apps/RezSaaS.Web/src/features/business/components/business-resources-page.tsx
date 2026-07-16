"use client";

import {
  MoreHorizontal,
  Pencil,
  PowerOff,
  RotateCcw,
  Sofa,
  Tags,
  Trash2,
  TriangleAlert
} from "lucide-react";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useState, type FormEvent } from "react";
import { toast } from "sonner";
import {
  createResource,
  createResourceType,
  deleteResourceType,
  describeResourceStatus,
  listResources,
  listResourceTypes,
  markResourceOutOfService,
  renameResource,
  restoreResource,
  type BusinessResource,
  type BusinessResourceType,
  type ResourceResult
} from "@/features/business/api/business-resource-client";
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
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { useIsMobile } from "@/shared/hooks/use-mobile";

/**
 * KAYNAKLAR + EKIPMAN TURLERI (Serit E) -- TEK sayfa, IKI sekme (docs/29).
 * /panel/kaynak-turleri buraya redirect eder.
 *
 * IKI SEVIYE:
 *  - Ekipman turleri (tenant): "koltuk", "oda", "cihaz" gibi TURLER.
 *  - Kaynaklar (sube altinda): o turlerden somut orneklar ("1 numaralı koltuk").
 *
 * Bir kaynak eklemek icin ONCE en az bir ekipman turu gerekir (resourceTypeId zorunlu).
 * Bu yuzden turler ust seviyede yuklenir ve iki sekme de paylasir.
 */

const NAME_MIN = 2;
const NAME_MAX = 160;
const KEY_MIN = 2;
const KEY_MAX = 80;

type LoadStatus = "idle" | "loading" | "ready" | "error";

export type BusinessResourcesPageProps = {
  tenantId: string;
  branches: BusinessBranch[];
};

export function BusinessResourcesPage({
  branches,
  tenantId
}: BusinessResourcesPageProps) {
  const router = useRouter();

  // Ekipman turleri TENANT seviyesinde -- iki sekme de paylasir.
  const [resourceTypes, setResourceTypes] = useState<BusinessResourceType[]>([]);
  const [typesStatus, setTypesStatus] = useState<LoadStatus>("loading");

  const loadTypes = useCallback(async () => {
    setTypesStatus("loading");
    const result = await listResourceTypes(tenantId);

    if (result.kind !== "success") {
      setTypesStatus("error");
      toast.error(result.message);
      return;
    }

    setResourceTypes(result.data ?? []);
    setTypesStatus("ready");
  }, [tenantId]);

  useEffect(() => {
    void loadTypes();
  }, [loadTypes]);

  return (
    <div className="space-y-6">
      <header className="flex flex-col gap-2">
        <h1 className="text-3xl font-semibold tracking-tight">Koltuklar ve ekipman</h1>
        <p className="max-w-2xl text-sm text-muted-foreground">
          Salonundaki koltuk, oda ve cihazları yönet. Önce bir <strong>ekipman türü</strong>{" "}
          tanımlarsın (ör. &quot;Koltuk&quot;, &quot;Oda&quot;), sonra her şubede o türden
          somut kaynaklar (ör. &quot;1 numaralı koltuk&quot;) eklersin.
        </p>
      </header>

      <Tabs defaultValue="kaynaklar">
        <TabsList>
          <TabsTrigger value="kaynaklar">
            <Sofa aria-hidden className="size-4" />
            Kaynaklar
          </TabsTrigger>
          <TabsTrigger value="turler">
            <Tags aria-hidden className="size-4" />
            Ekipman türleri
          </TabsTrigger>
        </TabsList>

        <TabsContent className="pt-6" value="kaynaklar">
          <ResourcesTab
            branches={branches}
            resourceTypes={resourceTypes}
            router={router}
            tenantId={tenantId}
            typesStatus={typesStatus}
          />
        </TabsContent>

        <TabsContent className="pt-6" value="turler">
          <ResourceTypesTab
            onCreated={(type) => setResourceTypes((current) => [...current, type])}
            onDeleted={(id) =>
              setResourceTypes((current) => current.filter((entry) => entry.id !== id))
            }
            resourceTypes={resourceTypes}
            router={router}
            status={typesStatus}
            tenantId={tenantId}
          />
        </TabsContent>
      </Tabs>
    </div>
  );
}

type AppRouter = ReturnType<typeof useRouter>;

/**
 * Mutasyon sonucunu yorumlar: "rejected" (sunucu reddetti) -> refresh; "failed" (ag) -> refresh ETME.
 */
function handleFailure(result: ResourceResult<unknown>, router: AppRouter) {
  if (result.kind === "rejected") {
    toast.error(result.message);
    router.refresh();
    return;
  }
  if (result.kind === "failed") {
    toast.error(result.message);
  }
}

/* ===========================================================================
   SEKME 1: KAYNAKLAR (sube altinda)
   =========================================================================== */

type ResourceDialogState =
  | { mode: "create" }
  | { mode: "rename"; resource: BusinessResource }
  | null;

function ResourcesTab({
  branches,
  resourceTypes,
  router,
  tenantId,
  typesStatus
}: {
  branches: BusinessBranch[];
  resourceTypes: BusinessResourceType[];
  router: AppRouter;
  tenantId: string;
  typesStatus: LoadStatus;
}) {
  const isMobile = useIsMobile();

  const [selectedBranchId, setSelectedBranchId] = useState<string>(
    branches.length === 1 ? (branches[0]?.id ?? "") : ""
  );
  const [resources, setResources] = useState<BusinessResource[]>([]);
  const [status, setStatus] = useState<LoadStatus>("idle");
  const [dialog, setDialog] = useState<ResourceDialogState>(null);
  const [blockTarget, setBlockTarget] = useState<BusinessResource | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const selectedBranch = branches.find((branch) => branch.id === selectedBranchId);

  const load = useCallback(
    async (branchId: string) => {
      setStatus("loading");
      const result = await listResources(tenantId, branchId);

      if (result.kind !== "success") {
        setStatus("error");
        toast.error(result.message);
        return;
      }

      setResources(result.data ?? []);
      setStatus("ready");
    },
    [tenantId]
  );

  useEffect(() => {
    if (!selectedBranchId) {
      setStatus("idle");
      setResources([]);
      return;
    }
    void load(selectedBranchId);
  }, [selectedBranchId, load]);

  function typeName(resourceTypeId: string | null | undefined): string {
    const found = resourceTypes.find((entry) => entry.id === resourceTypeId);
    return found?.displayName ?? "Bilinmeyen tür";
  }

  async function submitResource(input: { resourceTypeId: string; displayName: string }) {
    if (!dialog || !selectedBranchId) {
      return;
    }

    setSubmitting(true);
    const result =
      dialog.mode === "rename"
        ? await renameResource(
            tenantId,
            selectedBranchId,
            dialog.resource.id ?? "",
            input.displayName
          )
        : await createResource(tenantId, selectedBranchId, input);
    setSubmitting(false);

    if (result.kind !== "success") {
      handleFailure(result, router);
      return;
    }

    const saved = result.data;
    if (saved !== null) {
      if (dialog.mode === "rename") {
        setResources((current) =>
          current.map((entry) =>
            entry.id === saved.id ? { ...entry, ...saved } : entry
          )
        );
      } else {
        setResources((current) => [...current, saved]);
      }
    } else if (dialog.mode === "create") {
      // Govde eksik -> yerel yamayi atla, sunucudan tazele.
      void load(selectedBranchId);
    }

    toast.success(dialog.mode === "rename" ? "Kaynak adı güncellendi." : "Kaynak eklendi.");
    setDialog(null);
    router.refresh();
  }

  async function confirmBlock() {
    if (!blockTarget || !selectedBranchId) {
      return;
    }

    const view = describeResourceStatus(blockTarget.status);
    setSubmitting(true);
    const result = view.isOutOfService
      ? await restoreResource(tenantId, selectedBranchId, blockTarget.id ?? "")
      : await markResourceOutOfService(tenantId, selectedBranchId, blockTarget.id ?? "");
    setSubmitting(false);

    if (result.kind !== "success") {
      handleFailure(result, router);
      setBlockTarget(null);
      return;
    }

    const saved = result.data;
    setResources((current) =>
      current.map((entry) =>
        entry.id === blockTarget.id
          ? saved ?? {
              ...entry,
              status: view.isOutOfService ? "Active" : "OutOfService"
            }
          : entry
      )
    );
    toast.success(view.isOutOfService ? "Kaynak yeniden hizmete alındı." : "Kaynak hizmet dışı bırakıldı.");
    setBlockTarget(null);
    router.refresh();
  }

  const hasTypes = resourceTypes.length > 0;

  const formNode = dialog ? (
    <ResourceForm
      key={dialog.mode === "rename" ? dialog.resource.id : "yeni"}
      initialName={dialog.mode === "rename" ? (dialog.resource.displayName ?? "") : ""}
      initialTypeId={dialog.mode === "rename" ? (dialog.resource.resourceTypeId ?? "") : ""}
      mode={dialog.mode}
      onCancel={() => setDialog(null)}
      onSubmit={submitResource}
      resourceTypes={resourceTypes}
      submitting={submitting}
    />
  ) : null;

  const dialogTitle = dialog?.mode === "rename" ? "Kaynak adını düzenle" : "Kaynak ekle";
  const dialogDescription =
    dialog?.mode === "rename"
      ? "Bu koltuğun/odanın/cihazın görünen adını değiştir."
      : "Bir ekipman türü seç ve bu kaynağa bir ad ver (ör. “1 numaralı koltuk”).";

  if (branches.length === 0) {
    return <NoBranch />;
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        {/* SUBE SECICI -- birden fazla sube varsa. Tek subede otomatik secili. */}
        {branches.length > 1 ? (
          <div className="flex flex-1 flex-col gap-2 sm:max-w-sm">
            <Label htmlFor="kaynak-sube-secici">Şube</Label>
            <Select onValueChange={setSelectedBranchId} value={selectedBranchId}>
              <SelectTrigger className="min-h-11" id="kaynak-sube-secici">
                <SelectValue placeholder="Kaynaklarını görmek için bir şube seç" />
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
            Şube:{" "}
            <span className="font-medium text-foreground">
              {selectedBranch.displayName}
            </span>
          </p>
        ) : (
          <span />
        )}

        <Button
          className="min-h-11 shrink-0"
          disabled={!selectedBranchId || !hasTypes}
          onClick={() => setDialog({ mode: "create" })}
        >
          <Sofa aria-hidden className="size-4" />
          Kaynak ekle
        </Button>
      </div>

      {/* Tur yoksa kaynak eklenemez -> GORUNUR yonlendirme (Tuzak: resourceTypeId zorunlu). */}
      {typesStatus === "ready" && !hasTypes ? (
        <Card>
          <CardContent className="flex flex-col items-center gap-3 py-10 text-center">
            <Tags aria-hidden className="size-6 text-muted-foreground" />
            <p className="max-w-md text-sm text-muted-foreground">
              Henüz ekipman türü yok. Kaynak eklemeden önce &quot;Ekipman türleri&quot;
              sekmesinden en az bir tür (ör. &quot;Koltuk&quot;) tanımla.
            </p>
          </CardContent>
        </Card>
      ) : null}

      <ResourceList
        onBlock={setBlockTarget}
        onRename={(resource) => setDialog({ mode: "rename", resource })}
        resources={resources}
        selectedBranchId={selectedBranchId}
        status={status}
        typeName={typeName}
      />

      {/* KAYNAK FORMU -- masaustunde Dialog, mobilde Sheet */}
      {isMobile ? (
        <Sheet onOpenChange={(open) => !open && setDialog(null)} open={dialog !== null}>
          <SheetContent className="overflow-y-auto" side="bottom">
            <SheetHeader>
              <SheetTitle>{dialogTitle}</SheetTitle>
              <SheetDescription>{dialogDescription}</SheetDescription>
            </SheetHeader>
            <div className="px-4 pb-4">{formNode}</div>
          </SheetContent>
        </Sheet>
      ) : (
        <Dialog onOpenChange={(open) => !open && setDialog(null)} open={dialog !== null}>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>{dialogTitle}</DialogTitle>
              <DialogDescription>{dialogDescription}</DialogDescription>
            </DialogHeader>
            {formNode}
          </DialogContent>
        </Dialog>
      )}

      <BlockResourceDialog
        onCancel={() => setBlockTarget(null)}
        onConfirm={confirmBlock}
        resource={blockTarget}
        submitting={submitting}
      />
    </div>
  );
}

function ResourceList({
  onBlock,
  onRename,
  resources,
  selectedBranchId,
  status,
  typeName
}: {
  resources: BusinessResource[];
  selectedBranchId: string;
  status: LoadStatus;
  typeName: (resourceTypeId: string | null | undefined) => string;
  onBlock: (resource: BusinessResource) => void;
  onRename: (resource: BusinessResource) => void;
}) {
  if (!selectedBranchId) {
    return (
      <Card>
        <CardContent className="py-14 text-center text-sm text-muted-foreground">
          Kaynaklarını görmek için yukarıdan bir şube seç.
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
            Kaynaklar yüklenemedi. Sayfayı yenileyip tekrar dene.
          </p>
        </CardContent>
      </Card>
    );
  }

  if (resources.length === 0) {
    return (
      <Card>
        <CardContent className="flex flex-col items-center gap-4 py-14 text-center">
          <div className="flex size-14 items-center justify-center rounded-full bg-muted">
            <Sofa aria-hidden className="size-6 text-muted-foreground" />
          </div>
          <div className="space-y-1">
            <h2 className="text-lg font-semibold">Bu şubede henüz kaynak yok</h2>
            <p className="text-sm text-muted-foreground">
              İlk koltuğunu, odanı veya cihazını ekle.
            </p>
          </div>
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
              <TableHead>Kaynak</TableHead>
              <TableHead>Tür</TableHead>
              <TableHead>Durum</TableHead>
              <TableHead className="w-12 text-right">
                <span className="sr-only">İşlemler</span>
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {resources.map((resource) => (
              <TableRow key={resource.id}>
                <TableCell className="font-medium">{resource.displayName}</TableCell>
                <TableCell className="text-muted-foreground">
                  {typeName(resource.resourceTypeId)}
                </TableCell>
                <TableCell>
                  <ResourceStatusBadge status={resource.status} />
                </TableCell>
                <TableCell className="text-right">
                  <ResourceActions
                    onBlock={() => onBlock(resource)}
                    onRename={() => onRename(resource)}
                    status={resource.status}
                  />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Card>

      {/* <768px: tablo yerine KART listesi */}
      <div className="space-y-3 md:hidden">
        {resources.map((resource) => (
          <Card key={resource.id}>
            <CardContent className="flex items-start justify-between gap-2 p-4">
              <div className="min-w-0 space-y-1">
                <p className="font-medium">{resource.displayName}</p>
                <p className="text-sm text-muted-foreground">
                  {typeName(resource.resourceTypeId)}
                </p>
                <ResourceStatusBadge status={resource.status} />
              </div>
              <ResourceActions
                onBlock={() => onBlock(resource)}
                onRename={() => onRename(resource)}
                status={resource.status}
              />
            </CardContent>
          </Card>
        ))}
      </div>
    </>
  );
}

function ResourceStatusBadge({ status }: { status: string | null | undefined }) {
  const view = describeResourceStatus(status);
  return (
    <Badge variant={view.tone === "active" ? "secondary" : "outline"}>
      {view.label}
    </Badge>
  );
}

function ResourceActions({
  onBlock,
  onRename,
  status
}: {
  status: string | null | undefined;
  onBlock: () => void;
  onRename: () => void;
}) {
  const view = describeResourceStatus(status);
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button className="size-11" size="icon" variant="ghost">
          <MoreHorizontal aria-hidden className="size-4" />
          <span className="sr-only">Kaynak işlemleri</span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem className="min-h-11" onSelect={onRename}>
          <Pencil aria-hidden className="size-4" />
          Adı düzenle
        </DropdownMenuItem>
        {view.isOutOfService ? (
          <DropdownMenuItem className="min-h-11" onSelect={onBlock}>
            <RotateCcw aria-hidden className="size-4" />
            Yeniden hizmete al
          </DropdownMenuItem>
        ) : (
          <DropdownMenuItem
            className="min-h-11 text-destructive focus:text-destructive"
            onSelect={onBlock}
          >
            <PowerOff aria-hidden className="size-4" />
            Hizmet dışı bırak
          </DropdownMenuItem>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

function ResourceForm({
  initialName,
  initialTypeId,
  mode,
  onCancel,
  onSubmit,
  resourceTypes,
  submitting
}: {
  initialName: string;
  initialTypeId: string;
  mode: "create" | "rename";
  resourceTypes: BusinessResourceType[];
  submitting: boolean;
  onCancel: () => void;
  onSubmit: (input: { resourceTypeId: string; displayName: string }) => void;
}) {
  const [name, setName] = useState(initialName);
  const [typeId, setTypeId] = useState(initialTypeId);

  const trimmed = name.trim();
  const nameInvalid = trimmed.length < NAME_MIN || trimmed.length > NAME_MAX;
  const typeMissing = mode === "create" && !typeId;
  const invalid = nameInvalid || typeMissing;

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (invalid || submitting) {
      return;
    }
    onSubmit({ resourceTypeId: typeId, displayName: trimmed });
  }

  return (
    <form className="space-y-5" onSubmit={handleSubmit}>
      {mode === "create" ? (
        <div className="space-y-2">
          <Label htmlFor="kaynak-tur">Ekipman türü</Label>
          <Select onValueChange={setTypeId} value={typeId}>
            <SelectTrigger className="min-h-11" id="kaynak-tur">
              <SelectValue placeholder="Bir tür seç" />
            </SelectTrigger>
            <SelectContent>
              {resourceTypes.map((type) => (
                <SelectItem key={type.id} value={type.id ?? ""}>
                  {type.displayName}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      ) : null}

      <div className="space-y-2">
        <Label htmlFor="kaynak-ad">Görünen ad</Label>
        <Input
          autoComplete="off"
          autoFocus
          className="min-h-11"
          id="kaynak-ad"
          maxLength={NAME_MAX}
          onChange={(event) => setName(event.target.value)}
          placeholder="Örn. 1 numaralı koltuk"
          value={name}
        />
        <p className="text-sm text-muted-foreground">
          Kaynağa verilecek ad. En az {NAME_MIN} karakter.
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
          {submitting ? "Kaydediliyor…" : mode === "rename" ? "Kaydet" : "Kaynağı ekle"}
        </Button>
      </div>
    </form>
  );
}

/**
 * HIZMET DISI / HIZMETE ALMA ONAYI (yikici aksiyon -> AlertDialog).
 * Hizmet disi kaynak yeni randevulara acilmaz; geri alinabilir.
 */
function BlockResourceDialog({
  onCancel,
  onConfirm,
  resource,
  submitting
}: {
  resource: BusinessResource | null;
  submitting: boolean;
  onCancel: () => void;
  onConfirm: () => void;
}) {
  const view = describeResourceStatus(resource?.status);
  const restoring = view.isOutOfService;

  return (
    <AlertDialog onOpenChange={(open) => !open && onCancel()} open={resource !== null}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>
            {restoring
              ? `“${resource?.displayName}” yeniden hizmete alınsın mı?`
              : `“${resource?.displayName}” hizmet dışı bırakılsın mı?`}
          </AlertDialogTitle>
          <AlertDialogDescription>
            {restoring
              ? "Kaynak yeniden randevulara açılır."
              : "Hizmet dışı kaynak yeni randevulara açılmaz. İstediğin zaman yeniden hizmete alabilirsin."}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel className="min-h-11" disabled={submitting}>
            Vazgeç
          </AlertDialogCancel>
          <AlertDialogAction
            className={
              restoring
                ? "min-h-11"
                : "min-h-11 bg-destructive text-white hover:bg-destructive/90"
            }
            disabled={submitting}
            onClick={(event) => {
              event.preventDefault();
              onConfirm();
            }}
          >
            {submitting
              ? "İşleniyor…"
              : restoring
                ? "Yeniden hizmete al"
                : "Hizmet dışı bırak"}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}

/* ===========================================================================
   SEKME 2: EKIPMAN TURLERI (tenant seviyesinde)
   =========================================================================== */

function ResourceTypesTab({
  onCreated,
  onDeleted,
  resourceTypes,
  router,
  status,
  tenantId
}: {
  resourceTypes: BusinessResourceType[];
  status: LoadStatus;
  tenantId: string;
  router: AppRouter;
  onCreated: (type: BusinessResourceType) => void;
  onDeleted: (id: string) => void;
}) {
  const isMobile = useIsMobile();
  const [showForm, setShowForm] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<BusinessResourceType | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function submitType(input: { key: string; displayName: string }) {
    setSubmitting(true);
    const result = await createResourceType(tenantId, input);
    setSubmitting(false);

    if (result.kind !== "success") {
      handleFailure(result, router);
      return;
    }

    if (result.data !== null) {
      onCreated(result.data);
    }
    toast.success("Ekipman türü eklendi.");
    setShowForm(false);
    router.refresh();
  }

  async function confirmDelete() {
    if (!deleteTarget) {
      return;
    }

    setSubmitting(true);
    const result = await deleteResourceType(tenantId, deleteTarget.id ?? "");
    setSubmitting(false);

    if (result.kind !== "success") {
      handleFailure(result, router);
      setDeleteTarget(null);
      return;
    }

    onDeleted(deleteTarget.id ?? "");
    toast.success("Ekipman türü silindi.");
    setDeleteTarget(null);
    router.refresh();
  }

  const formNode = (
    <ResourceTypeForm
      onCancel={() => setShowForm(false)}
      onSubmit={submitType}
      submitting={submitting}
    />
  );

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-4">
        <p className="max-w-xl text-sm text-muted-foreground">
          Kaynaklarını sınıflandıran türler. Örnek: &quot;Koltuk&quot;, &quot;Oda&quot;,
          &quot;Cihaz&quot;.
        </p>
        <Button
          className="min-h-11 shrink-0"
          onClick={() => setShowForm(true)}
          variant="outline"
        >
          <Tags aria-hidden className="size-4" />
          Tür ekle
        </Button>
      </div>

      {status === "loading" ? (
        <div className="space-y-3">
          {[0, 1].map((row) => (
            <Skeleton className="h-14 w-full rounded-xl" key={row} />
          ))}
        </div>
      ) : status === "error" ? (
        <Card>
          <CardContent className="flex flex-col items-center gap-2 py-14 text-center">
            <TriangleAlert aria-hidden className="size-6 text-muted-foreground" />
            <p className="text-sm text-muted-foreground">
              Ekipman türleri yüklenemedi. Sayfayı yenileyip tekrar dene.
            </p>
          </CardContent>
        </Card>
      ) : resourceTypes.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center gap-4 py-14 text-center">
            <div className="flex size-14 items-center justify-center rounded-full bg-muted">
              <Tags aria-hidden className="size-6 text-muted-foreground" />
            </div>
            <div className="space-y-1">
              <h2 className="text-lg font-semibold">Henüz ekipman türü yok</h2>
              <p className="text-sm text-muted-foreground">
                İlk türünü ekle (ör. &quot;Koltuk&quot;); sonra kaynak ekleyebilirsin.
              </p>
            </div>
          </CardContent>
        </Card>
      ) : (
        <Card className="overflow-hidden py-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Tür</TableHead>
                <TableHead>Anahtar</TableHead>
                <TableHead className="w-12 text-right">
                  <span className="sr-only">İşlemler</span>
                </TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {resourceTypes.map((type) => (
                <TableRow key={type.id}>
                  <TableCell className="font-medium">{type.displayName}</TableCell>
                  <TableCell className="font-mono text-xs text-muted-foreground">
                    {type.key}
                  </TableCell>
                  <TableCell className="text-right">
                    <Button
                      className="min-h-11 text-destructive hover:text-destructive"
                      onClick={() => setDeleteTarget(type)}
                      size="sm"
                      variant="ghost"
                    >
                      <Trash2 aria-hidden className="size-4" />
                      Sil
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Card>
      )}

      {/* TUR FORMU -- masaustunde Dialog, mobilde Sheet */}
      {isMobile ? (
        <Sheet onOpenChange={(open) => !open && setShowForm(false)} open={showForm}>
          <SheetContent className="overflow-y-auto" side="bottom">
            <SheetHeader>
              <SheetTitle>Ekipman türü ekle</SheetTitle>
              <SheetDescription>
                Bir görünen ad ve kısa bir anahtar gir (ör. &quot;Koltuk&quot; / &quot;koltuk&quot;).
              </SheetDescription>
            </SheetHeader>
            <div className="px-4 pb-4">{formNode}</div>
          </SheetContent>
        </Sheet>
      ) : (
        <Dialog onOpenChange={(open) => !open && setShowForm(false)} open={showForm}>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Ekipman türü ekle</DialogTitle>
              <DialogDescription>
                Bir görünen ad ve kısa bir anahtar gir (ör. &quot;Koltuk&quot; / &quot;koltuk&quot;).
              </DialogDescription>
            </DialogHeader>
            {formNode}
          </DialogContent>
        </Dialog>
      )}

      <AlertDialog
        onOpenChange={(open) => !open && setDeleteTarget(null)}
        open={deleteTarget !== null}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              “{deleteTarget?.displayName}” türü silinsin mi?
            </AlertDialogTitle>
            <AlertDialogDescription>
              Bu türe bağlı kaynaklar varsa silme başarısız olur. Önce o kaynakları
              sil, sonra türü kaldır.
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
              {submitting ? "Siliniyor…" : "Türü sil"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}

function ResourceTypeForm({
  onCancel,
  onSubmit,
  submitting
}: {
  submitting: boolean;
  onCancel: () => void;
  onSubmit: (input: { key: string; displayName: string }) => void;
}) {
  const [displayName, setDisplayName] = useState("");
  const [key, setKey] = useState("");

  const trimmedName = displayName.trim();
  const trimmedKey = key.trim();
  const nameInvalid = trimmedName.length < NAME_MIN || trimmedName.length > NAME_MAX;
  const keyInvalid = trimmedKey.length < KEY_MIN || trimmedKey.length > KEY_MAX;
  const invalid = nameInvalid || keyInvalid;

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (invalid || submitting) {
      return;
    }
    onSubmit({ key: trimmedKey, displayName: trimmedName });
  }

  return (
    <form className="space-y-5" onSubmit={handleSubmit}>
      <div className="space-y-2">
        <Label htmlFor="tur-ad">Görünen ad</Label>
        <Input
          autoComplete="off"
          autoFocus
          className="min-h-11"
          id="tur-ad"
          maxLength={NAME_MAX}
          onChange={(event) => setDisplayName(event.target.value)}
          placeholder="Örn. Koltuk"
          value={displayName}
        />
        <p className="text-sm text-muted-foreground">
          Salonunda göreceğin ad. En az {NAME_MIN} karakter.
        </p>
      </div>

      <div className="space-y-2">
        <Label htmlFor="tur-anahtar">Anahtar</Label>
        <Input
          autoComplete="off"
          className="min-h-11"
          id="tur-anahtar"
          maxLength={KEY_MAX}
          onChange={(event) => setKey(event.target.value)}
          placeholder="Örn. koltuk"
          value={key}
        />
        <p className="text-sm text-muted-foreground">
          Kısa, benzersiz bir kod. En az {KEY_MIN} karakter. Sonradan değiştirilemez.
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
          {submitting ? "Ekleniyor…" : "Türü ekle"}
        </Button>
      </div>
    </form>
  );
}

/* ---------------------------------------------------------------------------
   SUBE YOK DURUMU
   --------------------------------------------------------------------------- */

function NoBranch() {
  return (
    <Card>
      <CardContent className="flex flex-col items-center gap-4 py-14 text-center">
        <div className="flex size-14 items-center justify-center rounded-full bg-muted">
          <Sofa aria-hidden className="size-6 text-muted-foreground" />
        </div>
        <div className="space-y-1">
          <h2 className="text-lg font-semibold">Önce bir şube ekle</h2>
          <p className="mx-auto max-w-sm text-sm text-muted-foreground">
            Kaynaklar her zaman bir şubeye bağlıdır. Kaynak eklemeden önce en az bir
            şube tanımlaman gerekiyor.
          </p>
        </div>
      </CardContent>
    </Card>
  );
}
