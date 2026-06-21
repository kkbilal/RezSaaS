"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import {
  listStaff,
  createStaff,
  archiveStaff,
  type StaffResponse
} from "@/features/business/api/business-staff-client";
import type { BranchResponse } from "@/features/business/api/business-branch-client";
import { Button } from "@/shared/ui/button";
import { EmptyState } from "@/shared/ui/empty-state";
import { DialogFormPanel, DialogOverlay } from "@/shared/ui/dialog";

type BusinessStaffManagementPageProps = {
  initialStaff: StaffResponse[];
  branches: BranchResponse[];
};

export function BusinessStaffManagementPage({
  initialStaff,
  branches
}: BusinessStaffManagementPageProps) {
  const router = useRouter();
  const [staff, setStaff] = useState<StaffResponse[]>(initialStaff);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedBranchId, setSelectedBranchId] = useState<string>(
    branches.length > 0 ? branches[0].id : ""
  );
  const [showCreate, setShowCreate] = useState(false);
  const [showArchive, setShowArchive] = useState<string | null>(null);
  const [displayName, setDisplayName] = useState("");

  const selectedBranch = branches.find((b) => b.id === selectedBranchId);

  async function loadStaff(branchId: string) {
    setLoading(true);
    setError(null);

    try {
      const result = await listStaff(branchId);
      setStaff(result);
    } catch {
      setError("Personel listesi alınamadı.");
    } finally {
      setLoading(false);
    }
  }

  function handleBranchChange(branchId: string) {
    setSelectedBranchId(branchId);
    loadStaff(branchId);
  }

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    if (!selectedBranchId) return;

    setLoading(true);
    setError(null);

    try {
      const result = await createStaff(selectedBranchId, {
        displayName
      });

      if (result) {
        setStaff((prev) => [...prev, result]);
        setShowCreate(false);
        setDisplayName("");
        router.refresh();
      } else {
        setError("Personel oluşturulamadı.");
      }
    } catch {
      setError("Personel oluşturulurken hata oluştu.");
    } finally {
      setLoading(false);
    }
  }

  async function handleArchive() {
    if (!showArchive || !selectedBranchId) return;

    setLoading(true);
    setError(null);

    try {
      await archiveStaff(selectedBranchId, showArchive);
      setStaff((prev) => prev.filter((s) => s.id !== showArchive));
      setShowArchive(null);
      router.refresh();
    } catch {
      setError("Personel arşivlenirken hata oluştu.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div>
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h2 className="text-xl font-semibold text-[var(--rs-ink)]">Personel</h2>
          <p className="mt-1 text-sm text-[var(--rs-muted)]">
            Şubelere bağlı personeli yönetin.
          </p>
        </div>
        {selectedBranchId ? (
          <Button onClick={() => { setDisplayName(""); setShowCreate(true); }}>
            Personel ekle
          </Button>
        ) : null}
      </div>

      {error ? (
        <p className="mb-4 text-sm text-[var(--rs-danger)]">{error}</p>
      ) : null}

      {branches.length > 0 ? (
        <div className="mb-6">
          <label className="block text-sm font-medium text-[var(--rs-ink)]">
            Şube seçin
            <select
              className="mt-1 block w-full max-w-sm rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2.5 text-sm text-[var(--rs-ink)] focus:border-[var(--rs-focus)] focus:outline-none"
              onChange={(e) => handleBranchChange(e.target.value)}
              value={selectedBranchId}
            >
              {branches.map((branch) => (
                <option key={branch.id} value={branch.id}>
                  {branch.displayName}
                </option>
              ))}
            </select>
          </label>
        </div>
      ) : (
        <EmptyState text="Önce bir şube tanımlayın." />
      )}

      {selectedBranch ? (
        staff.length === 0 ? (
          <EmptyState text={`${selectedBranch.displayName} şubesinde henüz personel yok.`} />
        ) : (
          <div className="grid gap-3">
            {staff.map((member) => (
              <article
                className="flex items-center justify-between rounded-2xl border border-[var(--rs-border)] bg-white p-4"
                key={member.id}
              >
                <div>
                  <span className="font-medium text-[var(--rs-ink)]">
                    {member.displayName}
                  </span>
                  <span className="ml-3 text-xs text-[var(--rs-muted)]">
                    {member.status === "Active" ? "Aktif" : member.status}
                  </span>
                </div>
                <Button
                  onClick={() => setShowArchive(member.id)}
                  variant="ghost"
                >
                  Arşivle
                </Button>
              </article>
            ))}
          </div>
        )
      ) : null}

      {showCreate ? (
        <DialogOverlay onClose={() => setShowCreate(false)}>
          <DialogFormPanel
            loading={loading}
            onClose={() => setShowCreate(false)}
            onSubmit={handleCreate}
            title="Yeni personel"
          >
            <label className="block text-sm font-medium text-[var(--rs-ink)]">
              Personel adı
              <input
                autoFocus
                className="mt-1 block w-full rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2.5 text-sm text-[var(--rs-ink)] placeholder:text-[var(--rs-muted)] focus:border-[var(--rs-focus)] focus:outline-none"
                maxLength={200}
                onChange={(e) => setDisplayName(e.target.value)}
                placeholder="Örn: Ahmet Yılmaz"
                required
                type="text"
                value={displayName}
              />
            </label>
            {selectedBranch ? (
              <p className="mt-2 text-xs text-[var(--rs-muted)]">
                Şube: {selectedBranch.displayName}
              </p>
            ) : null}
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
            title="Personel arşivle"
          >
            <p className="text-sm text-[var(--rs-muted)]">
              Bu personeli arşivlemek istediğinize emin misiniz? Arşivlenen personel
              yeni randevularda kullanılamaz.
            </p>
          </DialogFormPanel>
        </DialogOverlay>
      ) : null}
    </div>
  );
}
