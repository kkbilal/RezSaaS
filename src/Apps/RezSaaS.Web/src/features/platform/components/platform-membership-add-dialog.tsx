"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { apiClient } from "@/shared/api/client";
import { Button } from "@/shared/ui/button";
import { DialogFormPanel, DialogOverlay } from "@/shared/ui/dialog";

type PlatformMembershipAddDialogProps = {
  onDismiss: () => void;
  tenantId: string;
};

const roles = [
  { label: "BusinessOwner (İşletme sahibi)", value: "BusinessOwner" },
  { label: "BranchManager (Şube yöneticisi)", value: "BranchManager" },
  { label: "Staff (Personel)", value: "Staff" }
];

export function PlatformMembershipAddDialog({
  onDismiss,
  tenantId
}: PlatformMembershipAddDialogProps) {
  const router = useRouter();
  const [userAccountId, setUserAccountId] = useState("");
  const [role, setRole] = useState("BusinessOwner");
  const [branchId, setBranchId] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [toast, setToast] = useState<string | null>(null);

  function showToast(message: string) {
    setToast(message);
    window.setTimeout(() => setToast(null), 3600);
  }

  async function handleAdd(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!userAccountId.trim()) {
      showToast("UserAccountId zorunludur.");
      return;
    }

    setIsSubmitting(true);

    try {
      const result = await apiClient.POST(
        "/api/admin/tenants/{tenantId}/memberships",
        {
          body: {
            userAccountId: userAccountId.trim(),
            role,
            branchId: branchId.trim() || undefined
          },
          params: {
            path: { tenantId }
          }
        }
      );

      if (!result.response.ok) {
        showToast(getMembershipErrorCopy(result.response.status));
        return;
      }

      onDismiss();
      showToast("Membership eklendi.");
      router.refresh();
    } catch {
      showToast("Membership ekleme şu anda başarısız oldu.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <DialogOverlay onEscapeKeyDown={onDismiss}>
      <DialogFormPanel onSubmit={(event) => void handleAdd(event)}>
        <div className="space-y-4">
          <h2 className="text-3xl font-semibold tracking-[-0.06em] text-[var(--rs-ink)]">
            Membership ekle
          </h2>
          <p className="text-sm leading-7 text-[var(--rs-muted)]">
            Hedef kullanıcı aktif UserAccount olmalıdır; son aktif BusinessOwner
            suspend veya revoke edilemez.
          </p>
        </div>

        <div className="mt-6 grid gap-4">
          <label className="grid gap-2 text-sm font-medium text-[var(--rs-ink)]">
            UserAccountId
            <input
              className="min-h-12 rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface)] px-4 font-mono text-sm text-[var(--rs-ink)] outline-none transition focus:border-[var(--rs-accent)] focus:ring-4 focus:ring-[rgba(99_102_241_/_0.18)]"
              onChange={(event) => setUserAccountId(event.target.value)}
              placeholder="GUID"
              required
              type="text"
              value={userAccountId}
            />
          </label>

          <label className="grid gap-2 text-sm font-medium text-[var(--rs-ink)]">
            Rol
            <select
              className="min-h-12 rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface)] px-4 text-sm text-[var(--rs-ink)] outline-none transition focus:border-[var(--rs-accent)] focus:ring-4 focus:ring-[rgba(99_102_241_/_0.18)]"
              onChange={(event) => setRole(event.target.value)}
              value={role}
            >
              {roles.map((r) => (
                <option key={r.value} value={r.value}>
                  {r.label}
                </option>
              ))}
            </select>
          </label>

          <label className="grid gap-2 text-sm font-medium text-[var(--rs-ink)]">
            BranchId (opsiyonel)
            <input
              className="min-h-12 rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface)] px-4 font-mono text-sm text-[var(--rs-ink)] outline-none transition focus:border-[var(--rs-accent)] focus:ring-4 focus:ring-[rgba(99_102_241_/_0.18)]"
              onChange={(event) => setBranchId(event.target.value)}
              placeholder="GUID — Staff ve BranchManager için önerilir"
              type="text"
              value={branchId}
            />
            <span className="text-xs text-[var(--rs-muted)]">
              BusinessOwner tenant-wide çalışır; branchId genelde boş bırakılır.
            </span>
          </label>
        </div>

        <div className="mt-6 flex flex-col gap-3 sm:flex-row sm:justify-end">
          <Button disabled={isSubmitting} onClick={onDismiss} type="button" variant="secondary">
            Vazgeç
          </Button>
          <Button
            disabled={isSubmitting || !userAccountId.trim()}
            type="submit"
            variant="primary"
          >
            {isSubmitting ? "Ekleniyor" : "Membership ekle"}
          </Button>
        </div>
      </DialogFormPanel>
      {toast ? (
        <div className="fixed bottom-5 left-1/2 z-50 w-[calc(100%-2rem)] max-w-xl -translate-x-1/2 rounded-full border border-[var(--rs-border)] bg-[var(--rs-surface)] px-5 py-3 text-sm text-[var(--rs-ink)] shadow-[var(--rs-shadow-card)]">
          {toast}
        </div>
      ) : null}
    </DialogOverlay>
  );
}

function getMembershipErrorCopy(status: number) {
  if (status === 400) {
    return "Membership isteği geçerli değil.";
  }
  if (status === 401) {
    return "Platform oturumu doğrulanamadı; tekrar giriş gerekebilir.";
  }
  if (status === 403) {
    return "Bu aksiyon için PlatformAdmin step-up oturumu gerekiyor.";
  }
  if (status === 404) {
    return "Tenant veya kullanıcı bulunamadı.";
  }
  if (status === 409) {
    return "Kullanıcı zaten bu tenant&apos;ta membership&apos;e sahip olabilir.";
  }
  if (status === 422) {
    return "Backend kuralı bu membership&apos;i reddetti.";
  }
  if (status === 429) {
    return "Platform operasyon rate limit'i devrede.";
  }
  return "Membership eklenemedi.";
}
