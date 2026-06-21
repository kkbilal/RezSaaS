"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import {
  listResourcesByBranch,
  createResource,
  renameResource,
  markResourceOutOfService,
  restoreResource,
  type ResourceResponse,
  type CreateResourceRequest
} from "@/features/business/api/business-resource-client";
import { listResourceTypes, type ResourceTypeResponse } from "@/features/business/api/business-resource-type-client";
import { listBranches, type BranchResponse } from "@/features/business/api/business-branch-client";
import { Button } from "@/shared/ui/button";
import { EmptyState } from "@/shared/ui/empty-state";
import { DialogFormPanel, DialogOverlay } from "@/shared/ui/dialog";

type BusinessResourceManagementPageProps = {
  initialBranches: BranchResponse[];
  initialResourceTypes: ResourceTypeResponse[];
};

export function BusinessResourceManagementPage({
  initialBranches,
  initialResourceTypes
}: BusinessResourceManagementPageProps) {
  const router = useRouter();
  const [branches] = useState<BranchResponse[]>(initialBranches);
  const [resourceTypes] = useState<ResourceTypeResponse[]>(initialResourceTypes);
  const [selectedBranchId, setSelectedBranchId] = useState<string | null>(null);
  const [resources, setResources] = useState<ResourceResponse[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showCreate, setShowCreate] = useState(false);
  const [showRename, setShowRename] = useState<string | null>(null);
  const [draft, setDraft] = useState<CreateResourceRequest>({
    resourceTypeId: "",
    displayName: ""
  });
  const [renameDraft, setRenameDraft] = useState("");

  function resetDraft() {
    setDraft({ resourceTypeId: "", displayName: "" });
  }

  async function loadResources(branchId: string) {
    setLoading(true);
    setError(null);

    try {
      const result = await listResourcesByBranch(branchId);
      setResources(result);
    } catch {
      setError("Kaynaklar yüklenemedi.");
    } finally {
      setLoading(false);
    }
  }

  function handleBranchSelect(branchId: string) {
    setSelectedBranchId(branchId);
    loadResources(branchId);
  }

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    if (!selectedBranchId) return;
    setLoading(true);
    setError(null);

    try {
      const result = await createResource(selectedBranchId, {
        resourceTypeId: draft.resourceTypeId,
        displayName: draft.displayName
      });

      if (result) {
        setResources((prev) => [...prev, result]);
        setShowCreate(false);
        resetDraft();
      }
    } catch {
      setError("Kaynak oluşturulamadı.");
    } finally {
      setLoading(false);
    }
  }

  async function handleRename(resourceId: string) {
    if (!selectedBranchId) return;
    setLoading(true);
    setError(null);

    try {
      const result = await renameResource(selectedBranchId, resourceId, { displayName: renameDraft });

      if (result) {
        setResources((prev) => prev.map((r) => (r.id === resourceId ? result : r)));
        setShowRename(null);
        setRenameDraft("");
      }
    } catch {
      setError("Kaynak adı güncellenemedi.");
    } finally {
      setLoading(false);
    }
  }

  async function handleOutOfService(resourceId: string) {
    if (!selectedBranchId) return;
    setLoading(true);

    try {
      const result = await markResourceOutOfService(selectedBranchId, resourceId);
      if (result) {
        setResources((prev) => prev.map((r) => (r.id === resourceId ? result : r)));
      }
    } catch {
      setError("Hizmet dışı durumu güncellenemedi.");
    } finally {
      setLoading(false);
    }
  }

  async function handleRestore(resourceId: string) {
    if (!selectedBranchId) return;
    setLoading(true);

    try {
      const result = await restoreResource(selectedBranchId, resourceId);
      if (result) {
        setResources((prev) => prev.map((r) => (r.id === resourceId ? result : r)));
      }
    } catch {
      setError("Kaynak geri yüklenemedi.");
    } finally {
      setLoading(false);
    }
  }

  function getTypeLabel(typeId: string): string {
    return resourceTypes.find((t) => t.id === typeId)?.displayName ?? typeId.slice(0, 8);
  }

  return (
    <main className="mx-auto max-w-4xl px-4 py-10">
      <div className="mb-8">
        <h1 className="text-3xl font-semibold tracking-[-0.04em] text-[var(--rs-ink)]">
          Kaynak Yönetimi
        </h1>
        <p className="mt-2 text-sm text-[var(--rs-muted)]">
          Şubelere bağlı fiziksel kaynakları (koltuk, oda, yatak, istasyon vb.) yönetin.
        </p>
      </div>

      <div className="mb-8">
        <label className="block text-sm font-medium text-[var(--rs-ink)]">Şube seçin</label>
        <select
          className="mt-1 w-full rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2 text-sm text-[var(--rs-ink)] outline-none focus:border-[var(--rs-accent-strong)]"
          onChange={(e) => handleBranchSelect(e.target.value)}
          value={selectedBranchId ?? ""}
        >
          <option value="">-- Şube seçin --</option>
          {branches.map((b) => (
            <option key={b.id} value={b.id}>
              {b.displayName}
            </option>
          ))}
        </select>
      </div>

      {error && (
        <p className="mb-6 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{error}</p>
      )}

      {selectedBranchId && (
        <>
          <div className="mb-6 flex justify-end">
            <Button onClick={() => { resetDraft(); setShowCreate(true); }}>
              Yeni Kaynak
            </Button>
          </div>

          <div className="space-y-3">
            {loading && resources.length === 0 ? (
              <p className="text-sm text-[var(--rs-muted)]">Yükleniyor...</p>
            ) : resources.length === 0 ? (
              <EmptyState text="Bu şubede kaynak bulunmuyor." />
            ) : (
              resources.map((resource) => (
                <div
                  className="flex items-center justify-between rounded-2xl border border-[var(--rs-border)] bg-white p-4"
                  key={resource.id}
                >
                  <div>
                    <p className="font-medium text-[var(--rs-ink)]">{resource.displayName}</p>
                    <p className="mt-1 text-sm text-[var(--rs-muted)]">
                      {getTypeLabel(resource.resourceTypeId)} ·{" "}
                      {resource.status === "Active"
                        ? "Aktif"
                        : resource.status === "OutOfService"
                          ? "Hizmet Dışı"
                          : resource.status}
                    </p>
                  </div>
                  <div className="flex gap-2">
                    <Button
                      variant="secondary"
                      onClick={() => { setRenameDraft(resource.displayName); setShowRename(resource.id); }}
                    >
                      Adlandır
                    </Button>
                    {resource.status === "OutOfService" ? (
                      <Button onClick={() => handleRestore(resource.id)}>
                        Geri Yükle
                      </Button>
                    ) : (
                      <Button onClick={() => handleOutOfService(resource.id)}>
                        Hizmet Dışı
                      </Button>
                    )}
                  </div>
                </div>
              ))
            )}
          </div>
        </>
      )}

      {showCreate && selectedBranchId && (
        <DialogOverlay onClose={() => { setShowCreate(false); resetDraft(); }}>
          <DialogFormPanel
            label="Yeni kaynak"
            onClose={() => { setShowCreate(false); resetDraft(); }}
          >
            <form className="space-y-4" onSubmit={handleCreate}>
              <div>
                <label className="block text-sm font-medium text-[var(--rs-ink)]">Kaynak türü</label>
                <select
                  className="mt-1 w-full rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2 text-sm text-[var(--rs-ink)] outline-none focus:border-[var(--rs-accent-strong)]"
                  onChange={(e) => setDraft((prev) => ({ ...prev, resourceTypeId: e.target.value }))}
                  required
                  value={draft.resourceTypeId}
                >
                  <option value="">-- Tür seçin --</option>
                  {resourceTypes.map((t) => (
                    <option key={t.id} value={t.id}>
                      {t.displayName} ({t.key})
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-[var(--rs-ink)]">Kaynak adı</label>
                <input
                  className="mt-1 w-full rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2 text-sm text-[var(--rs-ink)] outline-none focus:border-[var(--rs-accent-strong)]"
                  maxLength={160}
                  onChange={(e) => setDraft((prev) => ({ ...prev, displayName: e.target.value }))}
                  placeholder="Koltuk 1, Oda A..."
                  required
                  value={draft.displayName}
                />
              </div>
              {error && <p className="text-sm text-red-600">{error}</p>}
              <div className="flex justify-end gap-3">
                <Button
                  type="button"
                  variant="secondary"
                  onClick={() => { setShowCreate(false); resetDraft(); }}
                >
                  İptal
                </Button>
                <Button disabled={loading} type="submit">
                  {loading ? "Oluşturuluyor..." : "Oluştur"}
                </Button>
              </div>
            </form>
          </DialogFormPanel>
        </DialogOverlay>
      )}

      {showRename && (
        <DialogOverlay onClose={() => { setShowRename(null); setRenameDraft(""); }}>
          <DialogFormPanel
            label="Kaynağı adlandır"
            onClose={() => { setShowRename(null); setRenameDraft(""); }}
          >
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-[var(--rs-ink)]">Yeni ad</label>
                <input
                  className="mt-1 w-full rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2 text-sm text-[var(--rs-ink)] outline-none focus:border-[var(--rs-accent-strong)]"
                  maxLength={160}
                  onChange={(e) => setRenameDraft(e.target.value)}
                  value={renameDraft}
                />
              </div>
              <div className="flex justify-end gap-3">
                <Button variant="secondary" onClick={() => { setShowRename(null); setRenameDraft(""); }}>
                  İptal
                </Button>
                <Button
                  disabled={loading || !renameDraft.trim()}
                  onClick={() => showRename && handleRename(showRename)}
                >
                  {loading ? "Kaydediliyor..." : "Kaydet"}
                </Button>
              </div>
            </div>
          </DialogFormPanel>
        </DialogOverlay>
      )}
    </main>
  );
}
