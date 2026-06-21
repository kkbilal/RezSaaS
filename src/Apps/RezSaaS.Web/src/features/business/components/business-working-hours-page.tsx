"use client";

import { useState, type FormEvent } from "react";
import {
  listWorkingHours,
  upsertWorkingHours,
  clearWorkingHours,
  type WorkingHoursResponse,
  type UpsertWorkingHoursRequest
} from "@/features/business/api/business-working-hours-client";
import { listBranches, type BranchResponse } from "@/features/business/api/business-branch-client";
import { Button } from "@/shared/ui/button";
import { EmptyState } from "@/shared/ui/empty-state";
import { DialogFormPanel, DialogOverlay } from "@/shared/ui/dialog";

const dayOrder: Record<string, number> = {
  Monday: 1, Tuesday: 2, Wednesday: 3, Thursday: 4,
  Friday: 5, Saturday: 6, Sunday: 7
};

const dayLabels: Record<string, string> = {
  Monday: "Pazartesi", Tuesday: "Salı", Wednesday: "Çarşamba",
  Thursday: "Perşembe", Friday: "Cuma", Saturday: "Cumartesi", Sunday: "Pazar"
};

type BusinessWorkingHoursPageProps = {
  initialBranches: BranchResponse[];
};

export function BusinessWorkingHoursPage({ initialBranches }: BusinessWorkingHoursPageProps) {
  const [branches] = useState<BranchResponse[]>(initialBranches);
  const [selectedBranchId, setSelectedBranchId] = useState<string | null>(null);
  const [hours, setHours] = useState<WorkingHoursResponse[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [editDay, setEditDay] = useState<string | null>(null);
  const [draft, setDraft] = useState<UpsertWorkingHoursRequest>({
    opensAt: "09:00", closesAt: "18:00", isClosed: false
  });

  async function loadHours(branchId: string) {
    setLoading(true);
    setError(null);

    try {
      const result = await listWorkingHours(branchId);
      setHours(result);
    } catch {
      setError("Çalışma saatleri yüklenemedi.");
    } finally {
      setLoading(false);
    }
  }

  function handleBranchSelect(branchId: string) {
    setSelectedBranchId(branchId);
    loadHours(branchId);
  }

  function openEdit(day: string) {
    const existing = hours.find((h) => h.dayOfWeek === day);
    setDraft({
      opensAt: existing?.opensAt ?? "09:00",
      closesAt: existing?.closesAt ?? "18:00",
      isClosed: existing?.isClosed ?? false
    });
    setEditDay(day);
  }

  async function handleSave(e: FormEvent) {
    e.preventDefault();
    if (!selectedBranchId || !editDay) return;
    setLoading(true);
    setError(null);

    try {
      const result = await upsertWorkingHours(selectedBranchId, editDay, draft);
      if (result) {
        setHours((prev) => {
          const filtered = prev.filter((h) => h.dayOfWeek !== editDay);
          return [...filtered, result].sort(
            (a, b) => (dayOrder[a.dayOfWeek] ?? 99) - (dayOrder[b.dayOfWeek] ?? 99)
          );
        });
        setEditDay(null);
      }
    } catch {
      setError("Çalışma saati kaydedilemedi.");
    } finally {
      setLoading(false);
    }
  }

  async function handleClear() {
    if (!selectedBranchId) return;
    setLoading(true);
    setError(null);

    try {
      await clearWorkingHours(selectedBranchId);
      setHours([]);
    } catch {
      setError("Çalışma saatleri temizlenemedi.");
    } finally {
      setLoading(false);
    }
  }

  function getHoursForDay(day: string): WorkingHoursResponse | undefined {
    return hours.find((h) => h.dayOfWeek === day);
  }

  return (
    <main className="mx-auto max-w-4xl px-4 py-10">
      <div className="mb-8">
        <h1 className="text-3xl font-semibold tracking-[-0.04em] text-[var(--rs-ink)]">
          Çalışma Saatleri
        </h1>
        <p className="mt-2 text-sm text-[var(--rs-muted)]">
          Şube bazında haftalık çalışma saatlerini düzenleyin.
        </p>
      </div>

      <div className="mb-8">
        <label className="block text-sm font-medium text-[var(--rs-ink)]">Şube seçin</label>
        <select
          className="mt-1 w-full rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2 text-sm outline-none focus:border-[var(--rs-accent-strong)]"
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
          <div className="mb-6 flex justify-end gap-3">
            <Button variant="secondary" onClick={handleClear} disabled={loading || hours.length === 0}>
              Tümünü Temizle
            </Button>
          </div>

          <div className="space-y-3">
            {loading && hours.length === 0 ? (
              <p className="text-sm text-[var(--rs-muted)]">Yükleniyor...</p>
            ) : (
              Object.entries(dayLabels)
                .sort(([aKey], [bKey]) => (dayOrder[aKey] ?? 99) - (dayOrder[bKey] ?? 99))
                .map(([dayKey, dayLabel]) => {
                  const record = getHoursForDay(dayKey);

                  return (
                    <div
                      className="flex items-center justify-between rounded-2xl border border-[var(--rs-border)] bg-white p-4"
                      key={dayKey}
                    >
                      <div>
                        <p className="font-medium text-[var(--rs-ink)]">{dayLabel}</p>
                        <p className="mt-1 text-sm text-[var(--rs-muted)]">
                          {record
                            ? record.isClosed
                              ? "Kapalı"
                              : `${record.opensAt.slice(0, 5)} - ${record.closesAt.slice(0, 5)}`
                            : "Ayarlanmamış"}
                        </p>
                      </div>
                      <Button variant="secondary" onClick={() => openEdit(dayKey)}>
                        {record ? "Düzenle" : "Ekle"}
                      </Button>
                    </div>
                  );
                })
            )}
          </div>
        </>
      )}

      {editDay && (
        <DialogOverlay onClose={() => setEditDay(null)}>
          <DialogFormPanel
            label={`${dayLabels[editDay]} - Çalışma Saati`}
            onClose={() => setEditDay(null)}
          >
            <form className="space-y-4" onSubmit={handleSave}>
              <div className="flex items-center gap-3">
                <input
                  checked={draft.isClosed}
                  id="isClosed"
                  onChange={(e) => setDraft((prev) => ({ ...prev, isClosed: e.target.checked }))}
                  type="checkbox"
                />
                <label className="text-sm text-[var(--rs-ink)]" htmlFor="isClosed">
                  Kapalı
                </label>
              </div>

              {!draft.isClosed && (
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-[var(--rs-ink)]">Açılış</label>
                    <input
                      className="mt-1 w-full rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2 text-sm outline-none focus:border-[var(--rs-accent-strong)]"
                      onChange={(e) => setDraft((prev) => ({ ...prev, opensAt: e.target.value }))}
                      type="time"
                      value={draft.opensAt}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-[var(--rs-ink)]">Kapanış</label>
                    <input
                      className="mt-1 w-full rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2 text-sm outline-none focus:border-[var(--rs-accent-strong)]"
                      onChange={(e) => setDraft((prev) => ({ ...prev, closesAt: e.target.value }))}
                      type="time"
                      value={draft.closesAt}
                    />
                  </div>
                </div>
              )}

              {error && <p className="text-sm text-red-600">{error}</p>}
              <div className="flex justify-end gap-3">
                <Button type="button" variant="secondary" onClick={() => setEditDay(null)}>
                  İptal
                </Button>
                <Button disabled={loading} type="submit">
                  {loading ? "Kaydediliyor..." : "Kaydet"}
                </Button>
              </div>
            </form>
          </DialogFormPanel>
        </DialogOverlay>
      )}
    </main>
  );
}
