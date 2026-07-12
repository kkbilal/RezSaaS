"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import type { PlatformTenantMembership } from "@/features/platform/api/get-platform-tenants-overview";
import { apiClient } from "@/shared/api/client";
import { Button } from "@/shared/ui/button";
import { DialogFormPanel, DialogOverlay } from "@/shared/ui/dialog";

const reasonMaxLength = 300;

type PlatformMembershipActionDialogProps = {
  onDismiss: () => void;
  membership: PlatformTenantMembership;
  tenantId: string;
};

export function PlatformMembershipSuspendDialog({
  onDismiss,
  membership,
  tenantId
}: PlatformMembershipActionDialogProps) {
  const router = useRouter();
  const [reason, setReason] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [toast, setToast] = useState<string | null>(null);

  function showToast(message: string) {
    setToast(message);
    window.setTimeout(() => setToast(null), 3600);
  }

  async function handleSuspend(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!reason.trim()) {
      showToast("Askıya alma nedeni zorunludur.");
      return;
    }

    setIsSubmitting(true);

    try {
      const result = await apiClient.POST(
        "/api/admin/tenants/{tenantId}/memberships/{membershipId}/suspend",
        {
          body: { reason: reason.trim() } as never,
          params: {
            path: {
              membershipId: membership.membershipId ?? "",
              tenantId
            }
          }
        }
      );

      if (!result.response.ok) {
        showToast(getMembershipErrorCopy(result.response.status));
        return;
      }

      onDismiss();
      showToast("Membership askıya alındı.");
      router.refresh();
    } catch {
      showToast("Membership askıya alma şu anda başarısız oldu.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <DialogOverlay onEscapeKeyDown={onDismiss}>
      <DialogFormPanel onSubmit={(event) => void handleSuspend(event)}>
        <div className="space-y-4">
          <h2 className="text-3xl font-semibold tracking-[-0.06em] text-[var(--rs-ink)]">
            Membership askıya al
          </h2>
          <p className="text-sm leading-7 text-[var(--rs-muted)]">
            {membership.role} · {shortGuid(membership.userAccountId)}
          </p>
        </div>
        <div className="mt-6 grid gap-4">
          <label className="grid gap-2 text-sm font-medium text-[var(--rs-ink)]">
            Askıya alma nedeni
            <textarea
              className="min-h-32 rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface)] px-4 py-3 text-sm leading-6 text-[var(--rs-ink)] outline-none transition focus:border-[var(--rs-accent)] focus:ring-4 focus:ring-[rgba(99_102_241_/_0.18)]"
              maxLength={reasonMaxLength}
              onChange={(event) => setReason(event.target.value)}
              placeholder="Askıya alma gerekçesini yaz."
              required
              value={reason}
            />
          </label>
        </div>
        <div className="mt-6 flex flex-col gap-3 sm:flex-row sm:justify-end">
          <Button disabled={isSubmitting} onClick={onDismiss} type="button" variant="secondary">
            Vazgeç
          </Button>
          <Button disabled={isSubmitting || !reason.trim()} type="submit" variant="secondary">
            {isSubmitting ? "Askıya alınıyor" : "Askıya al"}
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

export function PlatformMembershipRevokeDialog({
  onDismiss,
  membership,
  tenantId
}: PlatformMembershipActionDialogProps) {
  const router = useRouter();
  const [reason, setReason] = useState("");
  const [confirmation, setConfirmation] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [toast, setToast] = useState<string | null>(null);

  function showToast(message: string) {
    setToast(message);
    window.setTimeout(() => setToast(null), 3600);
  }

  const confirmPhrase = `REVOKE ${shortGuid(membership.userAccountId)}`;

  async function handleRevoke(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!reason.trim()) {
      showToast("İptal nedeni zorunludur.");
      return;
    }

    if (confirmation !== confirmPhrase) {
      showToast("Onay metni eşleşmiyor.");
      return;
    }

    setIsSubmitting(true);

    try {
      const result = await apiClient.POST(
        "/api/admin/tenants/{tenantId}/memberships/{membershipId}/revoke",
        {
          body: { reason: reason.trim() } as never,
          params: {
            path: {
              membershipId: membership.membershipId ?? "",
              tenantId
            }
          }
        }
      );

      if (!result.response.ok) {
        showToast(getMembershipErrorCopy(result.response.status));
        return;
      }

      onDismiss();
      showToast("Membership iptal edildi (terminal).");
      router.refresh();
    } catch {
      showToast("Membership iptali şu anda başarısız oldu.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <DialogOverlay onEscapeKeyDown={onDismiss}>
      <DialogFormPanel onSubmit={(event) => void handleRevoke(event)}>
        <div className="space-y-4">
          <h2 className="text-3xl font-semibold tracking-[-0.06em] text-[var(--rs-ink)]">
            Membership iptal et
          </h2>
          <p className="text-sm leading-7 text-[var(--rs-muted)]">
            {membership.role} · {shortGuid(membership.userAccountId)} · Bu işlem
            terminaldir; geri alınamaz.
          </p>
        </div>
        <div className="mt-6 grid gap-4">
          <label className="grid gap-2 text-sm font-medium text-[var(--rs-ink)]">
            İptal nedeni
            <textarea
              className="min-h-32 rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface)] px-4 py-3 text-sm leading-6 text-[var(--rs-ink)] outline-none transition focus:border-[var(--rs-accent)] focus:ring-4 focus:ring-[rgba(99_102_241_/_0.18)]"
              maxLength={reasonMaxLength}
              onChange={(event) => setReason(event.target.value)}
              placeholder="İptal gerekçesini yaz."
              required
              value={reason}
            />
          </label>
          <label className="grid gap-2 text-sm font-medium text-[var(--rs-ink)]">
            Onay metni
            <input
              className="min-h-12 rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface)] px-4 text-sm text-[var(--rs-ink)] outline-none transition focus:border-[var(--rs-accent)] focus:ring-4 focus:ring-[rgba(99_102_241_/_0.18)]"
              onChange={(event) => setConfirmation(event.target.value)}
              placeholder={confirmPhrase}
              value={confirmation}
            />
            <span className="text-xs text-[var(--rs-muted)]">
              Tam olarak <code className="rounded bg-[var(--rs-surface-muted)] px-1.5 py-0.5 font-mono text-[var(--rs-ink)]">{confirmPhrase}</code> yaz.
            </span>
          </label>
        </div>
        <div className="mt-6 flex flex-col gap-3 sm:flex-row sm:justify-end">
          <Button disabled={isSubmitting} onClick={onDismiss} type="button" variant="secondary">
            Vazgeç
          </Button>
          <Button
            disabled={isSubmitting || !reason.trim() || confirmation !== confirmPhrase}
            type="submit"
            variant="danger"
          >
            {isSubmitting ? "İptal ediliyor" : "Membership iptal et"}
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
    return "Membership veya tenant bulunamadı.";
  }
  if (status === 409) {
    return "Son aktif BusinessOwner iptal/askıya alınamaz.";
  }
  if (status === 422) {
    return "Backend kuralı bu aksiyonu reddetti.";
  }
  if (status === 429) {
    return "Platform operasyon rate limit'i devrede.";
  }
  return "Membership aksiyonu uygulanamadı.";
}

function shortGuid(value?: string | null) {
  if (!value) {
    return "Bilgi yok";
  }
  return `${value.slice(0, 8)}...`;
}
