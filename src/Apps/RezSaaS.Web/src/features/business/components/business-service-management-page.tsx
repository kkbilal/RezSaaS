"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import {
  listServices,
  createService,
  updateService,
  archiveService,
  type ServiceResponse,
  type VariantResponse,
  type CreateVariantRequest
} from "@/features/business/api/business-service-client";
import {
  listVariants,
  createVariant,
  updateVariant,
  deleteVariant,
  type VariantResponse as VariantResp
} from "@/features/business/api/business-variant-client";
import { Button } from "@/shared/ui/button";
import { EmptyState } from "@/shared/ui/empty-state";
import { DialogFormPanel, DialogOverlay } from "@/shared/ui/dialog";

type BusinessServiceManagementPageProps = {
  initialServices: ServiceResponse[];
};

export function BusinessServiceManagementPage({
  initialServices
}: BusinessServiceManagementPageProps) {
  const router = useRouter();
  const [services, setServices] = useState<ServiceResponse[]>(initialServices);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showCreate, setShowCreate] = useState(false);
  const [showEdit, setShowEdit] = useState<string | null>(null);
  const [showArchive, setShowArchive] = useState<string | null>(null);
  const [showVariants, setShowVariants] = useState<string | null>(null);
  const [showVariantForm, setShowVariantForm] = useState<{ serviceId: string; variantId?: string } | null>(null);
  const [variants, setVariants] = useState<VariantResp[]>([]);
  const [draft, setDraft] = useState({ name: "", categoryKey: "" });
  const [vDraft, setVDraft] = useState<CreateVariantRequest>({
    name: "", durationMinutes: 30, priceAmount: 0, currencyCode: "TRY", requiredResourceTypeId: null
  });

  function resetDraft() { setDraft({ name: "", categoryKey: "" }); }
  function resetVDraft() {
    setVDraft({ name: "", durationMinutes: 30, priceAmount: 0, currencyCode: "TRY", requiredResourceTypeId: null });
  }

  async function loadVariants(serviceId: string) {
    setLoading(true);
    try {
      const result = await listVariants(serviceId);
      setVariants(result);
      setShowVariants(serviceId);
    } catch { setError("Varyantlar alınamadı."); }
    finally { setLoading(false); }
  }

  async function handleCreateService(e: FormEvent) {
    e.preventDefault(); setLoading(true); setError(null);
    try {
      const result = await createService({ name: draft.name, categoryKey: draft.categoryKey });
      if (result) {
        setServices(prev => [...prev, result]);
        setShowCreate(false); resetDraft(); router.refresh();
      } else setError("Hizmet oluşturulamadı.");
    } catch { setError("Hata oluştu."); }
    finally { setLoading(false); }
  }

  async function handleEditService(e: FormEvent) {
    e.preventDefault(); if (!showEdit) return;
    setLoading(true); setError(null);
    try {
      const result = await updateService(showEdit, { name: draft.name, categoryKey: draft.categoryKey });
      if (result) {
        setServices(prev => prev.map(s => s.id === showEdit ? result : s));
        setShowEdit(null); resetDraft(); router.refresh();
      } else setError("Hizmet güncellenemedi.");
    } catch { setError("Hata oluştu."); }
    finally { setLoading(false); }
  }

  async function handleArchiveService() {
    if (!showArchive) return;
    setLoading(true); setError(null);
    try {
      await archiveService(showArchive);
      setServices(prev => prev.filter(s => s.id !== showArchive));
      setShowArchive(null); router.refresh();
    } catch { setError("Hizmet arşivlenirken hata oluştu."); }
    finally { setLoading(false); }
  }

  async function handleCreateVariant(e: FormEvent) {
    e.preventDefault();
    if (!showVariantForm || showVariantForm.variantId) return;
    setLoading(true); setError(null);
    try {
      const result = await createVariant(showVariantForm.serviceId, vDraft);
      if (result) {
        setVariants(prev => [...prev, result]);
        setShowVariantForm(null); resetVDraft();
        router.refresh();
      } else setError("Varyant oluşturulamadı.");
    } catch { setError("Hata oluştu."); }
    finally { setLoading(false); }
  }

  async function handleDeleteVariant(variantId: string) {
    if (!showVariants) return;
    setLoading(true); setError(null);
    try {
      await deleteVariant(showVariants, variantId);
      setVariants(prev => prev.filter(v => v.id !== variantId));
      router.refresh();
    } catch { setError("Varyant silinirken hata oluştu."); }
    finally { setLoading(false); }
  }

  return (
    <div>
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h2 className="text-xl font-semibold text-[var(--rs-ink)]">Hizmetler</h2>
          <p className="mt-1 text-sm text-[var(--rs-muted)]">
            Hizmetleri ve varyantlarını yönetin.
          </p>
        </div>
        <Button onClick={() => { resetDraft(); setShowCreate(true); }}>Hizmet ekle</Button>
      </div>

      {error ? <p className="mb-4 text-sm text-[var(--rs-danger)]">{error}</p> : null}

      {services.length === 0 ? <EmptyState text="Henüz hizmet tanımlanmamış." /> : (
        <div className="grid gap-4">
          {services.map((service) => (
            <article className="rounded-2xl border border-[var(--rs-border)] bg-white p-4" key={service.id}>
              <div className="flex items-start justify-between">
                <div>
                  <h3 className="font-semibold text-[var(--rs-ink)]">{service.name}</h3>
                  <p className="mt-0.5 text-xs text-[var(--rs-muted)]">{service.categoryKey}</p>
                </div>
                <div className="flex gap-2">
                  <Button onClick={() => { resetVDraft(); loadVariants(service.id); }} variant="ghost">
                    Varyantlar
                  </Button>
                  <Button onClick={() => { setDraft({ name: service.name, categoryKey: service.categoryKey }); setShowEdit(service.id); }} variant="ghost">
                    Düzenle
                  </Button>
                  <Button onClick={() => setShowArchive(service.id)} variant="ghost">
                    Arşivle
                  </Button>
                </div>
              </div>
            </article>
          ))}
        </div>
      )}

      {showVariants ? (() => {
        const service = services.find(s => s.id === showVariants);
        return (
          <DialogOverlay onClose={() => setShowVariants(null)}>
            <DialogFormPanel
              loading={loading}
              onClose={() => setShowVariants(null)}
              onSubmit={() => {}}
              submitLabel={null}
              title={`${service?.name ?? "Hizmet"} — Varyantlar`}
            >
              <div className="space-y-3">
                {variants.length === 0 ? (
                  <p className="text-sm text-[var(--rs-muted)]">Henüz varyant yok.</p>
                ) : (
                  variants.map(v => (
                    <div className="flex items-center justify-between rounded-xl bg-[var(--rs-surface-muted)] p-3" key={v.id}>
                      <div>
                        <span className="font-medium text-sm text-[var(--rs-ink)]">{v.name}</span>
                        <span className="ml-3 text-xs text-[var(--rs-muted)]">
                          {v.durationMinutes} dk · {v.priceAmount} {v.currencyCode}
                        </span>
                      </div>
                      <Button onClick={() => handleDeleteVariant(v.id)} variant="ghost">Sil</Button>
                    </div>
                  ))
                )}
                <div className="pt-2">
                  <Button onClick={() => { resetVDraft(); setShowVariantForm({ serviceId: showVariants }); }}>
                    Varyant ekle
                  </Button>
                </div>
              </div>
            </DialogFormPanel>
          </DialogOverlay>
        );
      })() : null}

      {showCreate ? (
        <DialogOverlay onClose={() => setShowCreate(false)}>
          <DialogFormPanel loading={loading} onClose={() => setShowCreate(false)} onSubmit={handleCreateService} title="Yeni hizmet">
            <div className="grid gap-4">
              <label className="block text-sm font-medium text-[var(--rs-ink)]">
                Hizmet adı
                <input className="mt-1 block w-full rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2.5 text-sm" maxLength={160} onChange={e => setDraft(p => ({ ...p, name: e.target.value }))} placeholder="Örn: Saç Kesimi" required type="text" value={draft.name} />
              </label>
              <label className="block text-sm font-medium text-[var(--rs-ink)]">
                Kategori
                <input className="mt-1 block w-full rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2.5 text-sm" maxLength={80} onChange={e => setDraft(p => ({ ...p, categoryKey: e.target.value }))} placeholder="Örn: hair, skincare" required type="text" value={draft.categoryKey} />
              </label>
            </div>
          </DialogFormPanel>
        </DialogOverlay>
      ) : null}

      {showEdit ? (
        <DialogOverlay onClose={() => setShowEdit(null)}>
          <DialogFormPanel loading={loading} onClose={() => setShowEdit(null)} onSubmit={handleEditService} submitLabel="Kaydet" title="Hizmet düzenle">
            <div className="grid gap-4">
              <label className="block text-sm font-medium text-[var(--rs-ink)]">
                Hizmet adı
                <input className="mt-1 block w-full rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2.5 text-sm" maxLength={160} onChange={e => setDraft(p => ({ ...p, name: e.target.value }))} required type="text" value={draft.name} />
              </label>
              <label className="block text-sm font-medium text-[var(--rs-ink)]">
                Kategori
                <input className="mt-1 block w-full rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2.5 text-sm" maxLength={80} onChange={e => setDraft(p => ({ ...p, categoryKey: e.target.value }))} required type="text" value={draft.categoryKey} />
              </label>
            </div>
          </DialogFormPanel>
        </DialogOverlay>
      ) : null}

      {showVariantForm && !showVariantForm.variantId ? (
        <DialogOverlay onClose={() => setShowVariantForm(null)}>
          <DialogFormPanel loading={loading} onClose={() => setShowVariantForm(null)} onSubmit={handleCreateVariant} title="Yeni varyant">
            <div className="grid gap-4">
              <label className="block text-sm font-medium text-[var(--rs-ink)]">
                Varyant adı
                <input className="mt-1 block w-full rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2.5 text-sm" maxLength={160} onChange={e => setVDraft(p => ({ ...p, name: e.target.value }))} required type="text" value={vDraft.name} />
              </label>
              <div className="grid grid-cols-3 gap-4">
                <label className="block text-sm font-medium text-[var(--rs-ink)]">
                  Süre (dk)
                  <input className="mt-1 block w-full rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2.5 text-sm" min={1} max={1440} onChange={e => setVDraft(p => ({ ...p, durationMinutes: Number(e.target.value) }))} required type="number" value={vDraft.durationMinutes} />
                </label>
                <label className="block text-sm font-medium text-[var(--rs-ink)]">
                  Fiyat
                  <input className="mt-1 block w-full rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2.5 text-sm" min={0} onChange={e => setVDraft(p => ({ ...p, priceAmount: Number(e.target.value) }))} required type="number" value={vDraft.priceAmount} />
                </label>
                <label className="block text-sm font-medium text-[var(--rs-ink)]">
                  Para birimi
                  <input className="mt-1 block w-full rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2.5 text-sm" maxLength={3} onChange={e => setVDraft(p => ({ ...p, currencyCode: e.target.value.toUpperCase() }))} placeholder="TRY" required type="text" value={vDraft.currencyCode} />
                </label>
              </div>
            </div>
          </DialogFormPanel>
        </DialogOverlay>
      ) : null}

      {showArchive ? (
        <DialogOverlay onClose={() => setShowArchive(null)}>
          <DialogFormPanel loading={loading} onClose={() => setShowArchive(null)} onSubmit={handleArchiveService} submitLabel="Arşivle" title="Hizmet arşivle">
            <p className="text-sm text-[var(--rs-muted)]">
              Bu hizmeti arşivlemek istediğinize emin misiniz? Varyant varsa
              arşivleme başarısız olur.
            </p>
          </DialogFormPanel>
        </DialogOverlay>
      ) : null}
    </div>
  );
}
