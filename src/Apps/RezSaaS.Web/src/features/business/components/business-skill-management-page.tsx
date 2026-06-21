"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { listSkills, createSkill, deleteSkill, type SkillResponse } from "@/features/business/api/business-skill-client";
import { Button } from "@/shared/ui/button";
import { Card, CardDescription, CardHeader, CardTitle } from "@/shared/ui/card";
import { DialogFormPanel, DialogOverlay } from "@/shared/ui/dialog";
import { EmptyState } from "@/shared/ui/empty-state";

type BusinessSkillManagementPageProps = {
  initialSkills: SkillResponse[];
};

export function BusinessSkillManagementPage({
  initialSkills
}: BusinessSkillManagementPageProps) {
  const router = useRouter();
  const [skills, setSkills] = useState<SkillResponse[]>(initialSkills);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showCreate, setShowCreate] = useState(false);
  const [showDelete, setShowDelete] = useState<string | null>(null);
  const [createName, setCreateName] = useState("");

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      const result = await createSkill({ name: createName });

      if (result) {
        setSkills((prev) => [...prev, result].sort((a, b) => a.name.localeCompare(b.name)));
        setShowCreate(false);
        setCreateName("");
        router.refresh();
      } else {
        setError("Beceri oluşturulamadı.");
      }
    } catch {
      setError("Beceri oluşturulurken hata oluştu.");
    } finally {
      setLoading(false);
    }
  }

  async function handleDelete() {
    if (!showDelete) return;

    setLoading(true);
    setError(null);

    try {
      await deleteSkill(showDelete);
      setSkills((prev) => prev.filter((s) => s.id !== showDelete));
      setShowDelete(null);
      router.refresh();
    } catch {
      setError("Beceri silinirken hata oluştu.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div>
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h2 className="text-xl font-semibold text-[var(--rs-ink)]">Yetenekler</h2>
          <p className="mt-1 text-sm text-[var(--rs-muted)]">
            Personel yeteneklerini (becerilerini) yönetin.
          </p>
        </div>
        <Button onClick={() => setShowCreate(true)}>Yetenek ekle</Button>
      </div>

      {error ? (
        <p className="mb-4 text-sm text-[var(--rs-danger)]">{error}</p>
      ) : null}

      {skills.length === 0 ? (
        <EmptyState text="Henüz yetenek tanımlanmamış." />
      ) : (
        <div className="grid gap-3">
          {skills.map((skill) => (
            <article
              className="flex items-center justify-between rounded-2xl border border-[var(--rs-border)] bg-white p-4"
              key={skill.id}
            >
              <span className="font-medium text-[var(--rs-ink)]">{skill.name}</span>
              <Button
                onClick={() => setShowDelete(skill.id)}
                variant="ghost"
              >
                Sil
              </Button>
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
            title="Yeni yetenek"
          >
            <label className="block text-sm font-medium text-[var(--rs-ink)]">
              Yetenek adı
              <input
                className="mt-1 block w-full rounded-xl border border-[var(--rs-border)] bg-white px-4 py-2.5 text-sm text-[var(--rs-ink)] placeholder:text-[var(--rs-muted)] focus:border-[var(--rs-focus)] focus:outline-none"
                maxLength={120}
                onChange={(e) => setCreateName(e.target.value)}
                placeholder="Örn: Saç Kesimi, Cilt Bakımı"
                required
                type="text"
                value={createName}
              />
            </label>
          </DialogFormPanel>
        </DialogOverlay>
      ) : null}

      {showDelete ? (
        <DialogOverlay onClose={() => setShowDelete(null)}>
          <DialogFormPanel
            loading={loading}
            onClose={() => setShowDelete(null)}
            onSubmit={handleDelete}
            submitLabel="Sil"
            title="Yetenek sil"
          >
            <p className="text-sm text-[var(--rs-muted)]">
              Bu yeteneği silmek istediğinize emin misiniz? Bir personele atanmışsa
              silinemez.
            </p>
          </DialogFormPanel>
        </DialogOverlay>
      ) : null}
    </div>
  );
}
