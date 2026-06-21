"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import {
  listResourceTypes,
  createResourceType,
  deleteResourceType,
  type ResourceTypeResponse,
  type CreateResourceTypeRequest
} from "@/features/business/api/business-resource-type-client";
import { Button } from "@/shared/ui/button";
import { EmptyState } from "@/shared/ui/empty-state";
import { DialogFormPanel, DialogOverlay } from "@/shared/ui/dialog";

type BusinessResourceTypeManagementPageProps = {
  initialResourceTypes: ResourceTypeResponse[];
};

export function BusinessResourceTypeManagementPage({
  initialResourceTypes
}: BusinessResourceTypeManagementPageProps) {
  const router = useRouter();
  const [types, setTypes] = useState<ResourceTypeResponse[]>(initialResourceTypes);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showCreate, setShowCreate] = useState(false);
  const [showDelete, setShowDelete] = useState<string | null>(null);
  const [draft, setDraft] = useState<CreateResourceTypeRequest>({
    key: "",
    displayName: ""
  });

  function resetDraft() {
    setDraft({ key: "", displayName: "" });
  }

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      const result = await createResourceType({
        key: draft.key,
        displayName: draft.displayName
      });

      if (result) {
        setTypes((prev) => [...prev, result]);
        setShowCreate(false);
        resetDraft();
      }
    } catch {
      setError("Kaynak türü oluşturulamadı.");
    } finally {
      setLoading(false);
    }
  }

  async function handleDelete(id: string) {
    setLoading(true);
    setError(null);

    try {
      await deleteResourceType(id);
      setTypes((prev) => prev.filter((t) => t.id !== id));
      setShowDelete(null);
    } catch {
      setError("Kaynak türü silinemedi. Kullanımda olabilir.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="mx-auto max-w-4xl px-4 py-10">
      <div className="mb-8 flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-semibold tracking-[-0.04em] text-[var(--rs-ink)]">
            Kaynak Türleri
          </h1>
          <p className="mt-2 text-sm text-[var(--rs-muted)]">
            Koltuk, oda, yatak, istasyon, cihaz gibi fiziksel kapasite türleri.
          </p>
        </div>
        <Button onClick={() => { resetDraft(); setShowCreate(true); }}>
          Yeni Kaynak Türü
        </Button>
      </div>

      {error && (
        <p className="mb-6 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{error}</p>
      )}

      <div className="space-y-3">
        {types.length === 0 ? (
          <EmptyState text="Henüz kaynak türü tanımlanmamış." />
        ) : (
          types.map((rt) => (
            <div
              className="flex items-center justify-between rounded-2xl border border-[var(--rs-border)] bg-white p-4"
              key={rt.id}
            >
              <div>
                <p className="font-medium text-[var(--rs-ink)]">{rt.displayName}</p>
                <p className="mt-1 text-sm text-[var(--rs-muted)]">
                  {rt.key}
                </p>
              </div>
              <Button
                variant="secondary"
                onClick={() => setShowDelete(rt.id)}
              >
                Sil
              </Button>
            </div>
          ))
        )}
      </div>

      {showCreate && (
        <DialogOverlay onClose={() => { setShowCreate(false); resetDraft(); }}>
          <DialogFormPanel
            label="Yeni kaynak türü"
            onClose={() => { setShowCreate(false); resetDraft(); }}
          >
            <form className="space-y-4" onSubmit={handleCreate}>
              <div>
                <label className="block text-sm font-medium text-[var(--rs-ink)]">Anahtar (key)</label>
                <input
                  className="mt-1 w-full rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2 text-sm text-[var(--rs-ink)] outline-none focus:border-[var(--rs-accent-strong)]"
                  maxLength={80}
                  onChange={(e) => setDraft((prev) => ({ ...prev, key: e.target.value }))}
                  placeholder="chair, room, bed, station, device"
                  required
                  value={draft.key}
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-[var(--rs-ink)]">Görünen ad</label>
                <input
                  className="mt-1 w-full rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2 text-sm text-[var(--rs-ink)] outline-none focus:border-[var(--rs-accent-strong)]"
                  maxLength={160}
                  onChange={(e) => setDraft((prev) => ({ ...prev, displayName: e.target.value }))}
                  placeholder="Koltuk, Oda, Yatak..."
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

      {showDelete && (
        <DialogOverlay onClose={() => setShowDelete(null)}>
          <DialogFormPanel
            label="Kaynak türünü sil"
            onClose={() => setShowDelete(null)}
          >
            <p className="text-sm text-[var(--rs-muted)]">
              Bu kaynak türünü silmek istediğinize emin misiniz? Kullanımda olan türler silinemez.
            </p>
            <div className="mt-6 flex justify-end gap-3">
              <Button variant="secondary" onClick={() => setShowDelete(null)}>
                İptal
              </Button>
              <Button
                disabled={loading}
                onClick={() => showDelete && handleDelete(showDelete)}
              >
                {loading ? "Siliniyor..." : "Sil"}
              </Button>
            </div>
          </DialogFormPanel>
        </DialogOverlay>
      )}
    </main>
  );
}
