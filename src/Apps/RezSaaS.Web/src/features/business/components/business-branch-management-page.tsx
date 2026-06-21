"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import {
  listBranches,
  createBranch,
  updateBranch,
  archiveBranch,
  type BranchResponse,
  type CreateBranchRequest
} from "@/features/business/api/business-branch-client";
import { Button } from "@/shared/ui/button";
import { EmptyState } from "@/shared/ui/empty-state";
import { DialogFormPanel, DialogOverlay } from "@/shared/ui/dialog";

type BusinessBranchManagementPageProps = {
  initialBranches: BranchResponse[];
};

export function BusinessBranchManagementPage({
  initialBranches
}: BusinessBranchManagementPageProps) {
  const router = useRouter();
  const [branches, setBranches] = useState<BranchResponse[]>(initialBranches);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showCreate, setShowCreate] = useState(false);
  const [showEdit, setShowEdit] = useState<string | null>(null);
  const [showArchive, setShowArchive] = useState<string | null>(null);
  const [draft, setDraft] = useState<CreateBranchRequest>({
    slug: "",
    displayName: "",
    timeZoneId: "Europe/Istanbul",
    city: "",
    district: "",
    addressLine: ""
  });

  function resetDraft() {
    setDraft({
      slug: "",
      displayName: "",
      timeZoneId: "Europe/Istanbul",
      city: "",
      district: "",
      addressLine: ""
    });
  }

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      const result = await createBranch({
        slug: draft.slug,
        displayName: draft.displayName,
        timeZoneId: draft.timeZoneId,
        city: draft.city || undefined,
        district: draft.district || undefined,
        addressLine: draft.addressLine || undefined
      });

      if (result) {
        setBranches((prev) => [...prev, result]);
        setShowCreate(false);
        resetDraft();
        router.refresh();
      } else {
        setError("Şube oluşturulamadı.");
      }
    } catch {
      setError("Şube oluşturulurken hata oluştu.");
    } finally {
      setLoading(false);
    }
  }

  async function handleEdit(e: FormEvent) {
    e.preventDefault();
    if (!showEdit) return;

    setLoading(true);
    setError(null);

    try {
      const result = await updateBranch(showEdit, {
        displayName: draft.displayName,
        city: draft.city || undefined,
        district: draft.district || undefined,
        addressLine: draft.addressLine || undefined
      });

      if (result) {
        setBranches((prev) => prev.map((b) => (b.id === showEdit ? result : b)));
        setShowEdit(null);
        resetDraft();
        router.refresh();
      } else {
        setError("Şube güncellenemedi.");
      }
    } catch {
      setError("Şube güncellenirken hata oluştu.");
    } finally {
      setLoading(false);
    }
  }

  async function handleArchive() {
    if (!showArchive) return;

    setLoading(true);
    setError(null);

    try {
      await archiveBranch(showArchive);
      setBranches((prev) => prev.filter((b) => b.id !== showArchive));
      setShowArchive(null);
      router.refresh();
    } catch {
      setError("Şube arşivlenirken hata oluştu.");
    } finally {
      setLoading(false);
    }
  }

  function openEdit(branch: BranchResponse) {
    setDraft({
      slug: branch.slug,
      displayName: branch.displayName,
      timeZoneId: branch.timeZoneId,
      city: branch.city,
      district: branch.district,
      addressLine: branch.addressLine
    });
    setShowEdit(branch.id);
  }

  return (
    <div>
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h2 className="text-xl font-semibold text-[var(--rs-ink)]">Şubeler</h2>
          <p className="mt-1 text-sm text-[var(--rs-muted)]">
            İşletme şubelerini yönetin.
          </p>
        </div>
        <Button onClick={() => { resetDraft(); setShowCreate(true); }}>Şube ekle</Button>
      </div>

      {error ? (
        <p className="mb-4 text-sm text-[var(--rs-danger)]">{error}</p>
      ) : null}

      {branches.length === 0 ? (
        <EmptyState text="Henüz şube tanımlanmamış." />
      ) : (
        <div className="grid gap-4">
          {branches.map((branch) => (
            <article
              className="rounded-2xl border border-[var(--rs-border)] bg-white p-4"
              key={branch.id}
            >
              <div className="flex items-start justify-between">
                <div>
                  <h3 className="font-semibold text-[var(--rs-ink)]">{branch.displayName}</h3>
                  <p className="mt-0.5 font-mono text-xs text-[var(--rs-muted)]">
                    {branch.slug} · {branch.timeZoneId}
                  </p>
                </div>
                <div className="flex gap-2">
                  <Button onClick={() => openEdit(branch)} variant="ghost">
                    Düzenle
                  </Button>
                  <Button onClick={() => setShowArchive(branch.id)} variant="ghost">
                    Arşivle
                  </Button>
                </div>
              </div>
              {branch.city || branch.district || branch.addressLine ? (
                <p className="mt-2 text-sm text-[var(--rs-muted)]">
                  {[branch.addressLine, branch.district, branch.city]
                    .filter(Boolean)
                    .join(", ")}
                </p>
              ) : null}
              {branch.slotIntervalMinutes || branch.maxPublicSlots ? (
                <div className="mt-2 flex gap-4 text-xs text-[var(--rs-muted)]">
                  {branch.slotIntervalMinutes ? (
                    <span>Aralık: {branch.slotIntervalMinutes} dk</span>
                  ) : null}
                  {branch.maxPublicSlots ? (
                    <span>Maks. slot: {branch.maxPublicSlots}</span>
                  ) : null}
                </div>
              ) : null}
            </article>
          ))}
        </div>
      )}

      {showCreate ? (
        <DialogOverlay onClose={() => setShowCreate(false)}>
          <DialogFormPanel
            loading={loading}
            onClose={() => setShowCreate(false)}
            onSubmit={handleCreate}
            title="Yeni şube"
          >
            <div className="grid gap-4">
              <label className="block text-sm font-medium text-[var(--rs-ink)]">
                Şube kodu (slug)
                <input
                  className="mt-1 block w-full rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2.5 text-sm text-[var(--rs-ink)] focus:border-[var(--rs-focus)] focus:outline-none"
                  maxLength={64}
                  onChange={(e) => setDraft((p) => ({ ...p, slug: e.target.value }))}
                  placeholder="ornek-sube"
                  required
                  type="text"
                  value={draft.slug}
                />
              </label>
              <label className="block text-sm font-medium text-[var(--rs-ink)]">
                Şube adı
                <input
                  className="mt-1 block w-full rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2.5 text-sm text-[var(--rs-ink)] focus:border-[var(--rs-focus)] focus:outline-none"
                  maxLength={200}
                  onChange={(e) => setDraft((p) => ({ ...p, displayName: e.target.value }))}
                  placeholder="Örn: Merkez Şube"
                  required
                  type="text"
                  value={draft.displayName}
                />
              </label>
              <label className="block text-sm font-medium text-[var(--rs-ink)]">
                Zaman dilimi
                <input
                  className="mt-1 block w-full rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2.5 text-sm text-[var(--rs-ink)] focus:border-[var(--rs-focus)] focus:outline-none"
                  maxLength={80}
                  onChange={(e) => setDraft((p) => ({ ...p, timeZoneId: e.target.value }))}
                  placeholder="Europe/Istanbul"
                  required
                  type="text"
                  value={draft.timeZoneId}
                />
              </label>
              <div className="grid gap-4 sm:grid-cols-2">
                <label className="block text-sm font-medium text-[var(--rs-ink)]">
                  İl
                  <input
                    className="mt-1 block w-full rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2.5 text-sm text-[var(--rs-ink)] focus:border-[var(--rs-focus)] focus:outline-none"
                    maxLength={120}
                    onChange={(e) => setDraft((p) => ({ ...p, city: e.target.value }))}
                    type="text"
                    value={draft.city}
                  />
                </label>
                <label className="block text-sm font-medium text-[var(--rs-ink)]">
                  İlçe
                  <input
                    className="mt-1 block w-full rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2.5 text-sm text-[var(--rs-ink)] focus:border-[var(--rs-focus)] focus:outline-none"
                    maxLength={120}
                    onChange={(e) => setDraft((p) => ({ ...p, district: e.target.value }))}
                    type="text"
                    value={draft.district}
                  />
                </label>
              </div>
              <label className="block text-sm font-medium text-[var(--rs-ink)]">
                Adres
                <input
                  className="mt-1 block w-full rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2.5 text-sm text-[var(--rs-ink)] focus:border-[var(--rs-focus)] focus:outline-none"
                  maxLength={300}
                  onChange={(e) => setDraft((p) => ({ ...p, addressLine: e.target.value }))}
                  type="text"
                  value={draft.addressLine}
                />
              </label>
            </div>
          </DialogFormPanel>
        </DialogOverlay>
      ) : null}

      {showEdit ? (
        <DialogOverlay onClose={() => setShowEdit(null)}>
          <DialogFormPanel
            loading={loading}
            onClose={() => setShowEdit(null)}
            onSubmit={handleEdit}
            submitLabel="Kaydet"
            title="Şube düzenle"
          >
            <div className="grid gap-4">
              <label className="block text-sm font-medium text-[var(--rs-ink)]">
                Şube adı
                <input
                  className="mt-1 block w-full rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2.5 text-sm text-[var(--rs-ink)] focus:border-[var(--rs-focus)] focus:outline-none"
                  maxLength={200}
                  onChange={(e) => setDraft((p) => ({ ...p, displayName: e.target.value }))}
                  required
                  type="text"
                  value={draft.displayName}
                />
              </label>
              <div className="grid gap-4 sm:grid-cols-2">
                <label className="block text-sm font-medium text-[var(--rs-ink)]">
                  İl
                  <input
                    className="mt-1 block w-full rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2.5 text-sm text-[var(--rs-ink)] focus:border-[var(--rs-focus)] focus:outline-none"
                    maxLength={120}
                    onChange={(e) => setDraft((p) => ({ ...p, city: e.target.value }))}
                    type="text"
                    value={draft.city}
                  />
                </label>
                <label className="block text-sm font-medium text-[var(--rs-ink)]">
                  İlçe
                  <input
                    className="mt-1 block w-full rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2.5 text-sm text-[var(--rs-ink)] focus:border-[var(--rs-focus)] focus:outline-none"
                    maxLength={120}
                    onChange={(e) => setDraft((p) => ({ ...p, district: e.target.value }))}
                    type="text"
                    value={draft.district}
                  />
                </label>
              </div>
              <label className="block text-sm font-medium text-[var(--rs-ink)]">
                Adres
                <input
                  className="mt-1 block w-full rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2.5 text-sm text-[var(--rs-ink)] focus:border-[var(--rs-focus)] focus:outline-none"
                  maxLength={300}
                  onChange={(e) => setDraft((p) => ({ ...p, addressLine: e.target.value }))}
                  type="text"
                  value={draft.addressLine}
                />
              </label>
            </div>
          </DialogFormPanel>
        </DialogOverlay>
      ) : null}

      {showArchive ? (
        <DialogOverlay onClose={() => setShowArchive(null)}>
          <DialogFormPanel
            loading={loading}
            onClose={() => setShowArchive(null)}
            onSubmit={handleArchive}
            submitLabel="Arşivle"
            title="Şube arşivle"
          >
            <p className="text-sm text-[var(--rs-muted)]">
              Bu şubeyi arşivlemek istediğinize emin misiniz? Şubeye bağlı personel
              varsa arşivleme başarısız olur.
            </p>
          </DialogFormPanel>
        </DialogOverlay>
      ) : null}
    </div>
  );
}
