"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { apiClient } from "@/shared/api/client";
import { Button } from "@/shared/ui/button";
import { DialogFormPanel, DialogOverlay } from "@/shared/ui/dialog";

const slugMaxLength = 100;
const nameMaxLength = 200;

type PlatformTenantProvisionDialogProps = {
  onDismiss: () => void;
};

export function PlatformTenantProvisionDialog({
  onDismiss
}: PlatformTenantProvisionDialogProps) {
  const router = useRouter();
  const [slug, setSlug] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [ownerUserAccountId, setOwnerUserAccountId] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [toast, setToast] = useState<string | null>(null);

  function showToast(message: string) {
    setToast(message);
    window.setTimeout(() => setToast(null), 3600);
  }

  async function handleProvision(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!slug.trim()) {
      showToast("Slug zorunludur.");
      return;
    }

    if (!displayName.trim()) {
      showToast("İşletme adı zorunludur.");
      return;
    }

    if (!ownerUserAccountId.trim()) {
      showToast("Owner UserAccountId zorunludur.");
      return;
    }

    setIsSubmitting(true);

    try {
      const result = await apiClient.POST("/api/admin/tenants", {
        body: {
          slug: slug.trim().toLowerCase(),
          displayName: displayName.trim(),
          ownerUserAccountId: ownerUserAccountId.trim()
        }
      });

      if (!result.response.ok) {
        showToast(getProvisionErrorCopy(result.response.status));
        return;
      }

      onDismiss();
      showToast("Tenant oluşturuldu.");
      router.refresh();
    } catch {
      showToast("Tenant oluşturma şu anda başarısız oldu.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <DialogOverlay onEscapeKeyDown={onDismiss}>
      <DialogFormPanel onSubmit={(event) => void handleProvision(event)}>
        <div className="space-y-4">
          <h2 className="text-3xl font-semibold tracking-[-0.06em] text-[var(--rs-ink)]">
            Yeni tenant oluştur
          </h2>
          <p className="text-sm leading-7 text-[var(--rs-muted)]">
            Slug global benzersiz olmalıdır; owner kullanıcı aktif UserAccount
            olarak Identity içinde doğrulanır.
          </p>
        </div>

        <div className="mt-6 grid gap-4">
          <label className="grid gap-2 text-sm font-medium text-[var(--rs-ink)]">
            Slug
            <input
              className="min-h-12 rounded-2xl border border-[var(--rs-border)] bg-white px-4 text-sm text-[var(--rs-ink)] outline-none transition focus:border-[var(--rs-border-strong)] focus:ring-4 focus:ring-[rgb(5_26_36_/_0.08)]"
              maxLength={slugMaxLength}
              onChange={(event) => setSlug(event.target.value)}
              pattern="[a-z0-9-]+"
              placeholder="ornek-salon"
              required
              type="text"
              value={slug}
            />
            <span className="text-xs text-[var(--rs-muted)]">
              Sadece küçük harf, rakam ve tire. {slug.length}/{slugMaxLength}
            </span>
          </label>

          <label className="grid gap-2 text-sm font-medium text-[var(--rs-ink)]">
            İşletme adı
            <input
              className="min-h-12 rounded-2xl border border-[var(--rs-border)] bg-white px-4 text-sm text-[var(--rs-ink)] outline-none transition focus:border-[var(--rs-border-strong)] focus:ring-4 focus:ring-[rgb(5_26_36_/_0.08)]"
              maxLength={nameMaxLength}
              onChange={(event) => setDisplayName(event.target.value)}
              placeholder="Örnek Salon"
              required
              type="text"
              value={displayName}
            />
          </label>

          <label className="grid gap-2 text-sm font-medium text-[var(--rs-ink)]">
            Owner UserAccountId
            <input
              className="min-h-12 rounded-2xl border border-[var(--rs-border)] bg-white px-4 font-mono text-sm text-[var(--rs-ink)] outline-none transition focus:border-[var(--rs-border-strong)] focus:ring-4 focus:ring-[rgb(5_26_36_/_0.08)]"
              onChange={(event) => setOwnerUserAccountId(event.target.value)}
              placeholder="GUID"
              required
              type="text"
              value={ownerUserAccountId}
            />
            <span className="text-xs text-[var(--rs-muted)]">
              Identity içinde aktif UserAccount GUID&apos;si.
            </span>
          </label>
        </div>

        <div className="mt-6 flex flex-col gap-3 sm:flex-row sm:justify-end">
          <Button disabled={isSubmitting} onClick={onDismiss} type="button" variant="secondary">
            Vazgeç
          </Button>
          <Button
            disabled={isSubmitting || !slug.trim() || !displayName.trim() || !ownerUserAccountId.trim()}
            type="submit"
            variant="primary"
          >
            {isSubmitting ? "Oluşturuluyor" : "Tenant oluştur"}
          </Button>
        </div>
      </DialogFormPanel>
      {toast ? (
        <div className="fixed bottom-5 left-1/2 z-50 w-[calc(100%-2rem)] max-w-xl -translate-x-1/2 rounded-full border border-[var(--rs-border)] bg-white px-5 py-3 text-sm text-[var(--rs-ink)] shadow-[var(--rs-shadow-card)]">
          {toast}
        </div>
      ) : null}
    </DialogOverlay>
  );
}

function getProvisionErrorCopy(status: number) {
  if (status === 400) {
    return "Tenant oluşturma isteği geçerli değil.";
  }
  if (status === 401) {
    return "Platform oturumu doğrulanamadı; tekrar giriş gerekebilir.";
  }
  if (status === 403) {
    return "Bu aksiyon için PlatformAdmin step-up oturumu gerekiyor.";
  }
  if (status === 409) {
    return "Bu slug veya owner kullanıcı zaten mevcut olabilir.";
  }
  if (status === 422) {
    return "Backend kuralı tenant oluşturmayı reddetti.";
  }
  if (status === 429) {
    return "Platform operasyon rate limit'i devrede.";
  }
  return "Tenant oluşturulamadı.";
}
