"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import type { PlatformClosureCase } from "@/features/platform/api/get-platform-appeals-overview";
import { apiClient } from "@/shared/api/client";
import { Button } from "@/shared/ui/button";
import { DialogFormPanel, DialogOverlay } from "@/shared/ui/dialog";

const reasonMaxLength = 300;

type PlatformClosureApproveDialogProps = {
  closureCase: PlatformClosureCase;
  onDismiss: () => void;
};

export function PlatformClosureApproveDialog({
  closureCase,
  onDismiss
}: PlatformClosureApproveDialogProps) {
  const router = useRouter();
  const [reason, setReason] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [toast, setToast] = useState<string | null>(null);

  function showToast(message: string) {
    setToast(message);
    window.setTimeout(() => setToast(null), 3600);
  }

  async function handleApprove(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!reason.trim()) {
      showToast("Onay nedeni zorunludur.");
      return;
    }

    setIsSubmitting(true);

    try {
      const result = await apiClient.POST(
        "/api/admin/abuse/closure-cases/{closureCaseId}/approve",
        {
          body: { reason: reason.trim() },
          params: {
            path: {
              closureCaseId: closureCase.closureCaseId ?? ""
            }
          }
        }
      );

      if (!result.response.ok) {
        showToast(getClosureReviewErrorCopy(result.response.status));
        return;
      }

      onDismiss();
      showToast("Closure case onaylandı.");
      router.refresh();
    } catch {
      showToast("Closure onayı şu anda uygulanamadı.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <DialogOverlay onEscapeKeyDown={onDismiss}>
      <DialogFormPanel onSubmit={(event) => void handleApprove(event)}>
        <div className="space-y-4">
          <h2 className="text-3xl font-semibold tracking-[-0.06em] text-[var(--rs-ink)]">
            Closure case onayla
          </h2>
          <p className="text-sm leading-7 text-[var(--rs-muted)]">
            İki farklı PlatformAdmin with step-up onayı gerekir. Onayınız
            ikinci admin olarak kaydedilebilir.
          </p>
        </div>
        <div className="mt-6 grid gap-4">
          <label className="grid gap-2 text-sm font-medium text-[var(--rs-ink)]">
            Onay nedeni
            <textarea
              className="min-h-32 rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface)] px-4 py-3 text-sm leading-6 text-[var(--rs-ink)] outline-none transition focus:border-[var(--rs-accent)] focus:ring-4 focus:ring-[rgba(99_102_241_/_0.18)]"
              maxLength={reasonMaxLength}
              onChange={(event) => setReason(event.target.value)}
              placeholder="Onay gerekçesini yaz."
              required
              value={reason}
            />
          </label>
          <div className="flex justify-end text-xs text-[var(--rs-muted)]">
            {reason.length}/{reasonMaxLength}
          </div>
        </div>
        <div className="mt-6 flex flex-col gap-3 sm:flex-row sm:justify-end">
          <Button disabled={isSubmitting} onClick={onDismiss} type="button" variant="secondary">
            Vazgeç
          </Button>
          <Button disabled={isSubmitting || !reason.trim()} type="submit" variant="danger">
            {isSubmitting ? "Onaylanıyor" : "Closure onayla"}
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

type PlatformClosureRejectDialogProps = {
  closureCase: PlatformClosureCase;
  onDismiss: () => void;
};

export function PlatformClosureRejectDialog({
  closureCase,
  onDismiss
}: PlatformClosureRejectDialogProps) {
  const router = useRouter();
  const [reason, setReason] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [toast, setToast] = useState<string | null>(null);

  function showToast(message: string) {
    setToast(message);
    window.setTimeout(() => setToast(null), 3600);
  }

  async function handleReject(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!reason.trim()) {
      showToast("Ret nedeni zorunludur.");
      return;
    }

    setIsSubmitting(true);

    try {
      const result = await apiClient.POST(
        "/api/admin/abuse/closure-cases/{closureCaseId}/reject",
        {
          body: { reason: reason.trim() },
          params: {
            path: {
              closureCaseId: closureCase.closureCaseId ?? ""
            }
          }
        }
      );

      if (!result.response.ok) {
        showToast(getClosureReviewErrorCopy(result.response.status));
        return;
      }

      onDismiss();
      showToast("Closure case reddedildi.");
      router.refresh();
    } catch {
      showToast("Closure reddi şu anda uygulanamadı.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <DialogOverlay onEscapeKeyDown={onDismiss}>
      <DialogFormPanel onSubmit={(event) => void handleReject(event)}>
        <div className="space-y-4">
          <h2 className="text-3xl font-semibold tracking-[-0.06em] text-[var(--rs-ink)]">
            Closure case reddet
          </h2>
          <p className="text-sm leading-7 text-[var(--rs-muted)]">
            Case kapatılır; kullanıcıya yeni closure proposal açılabilir.
          </p>
        </div>
        <div className="mt-6 grid gap-4">
          <label className="grid gap-2 text-sm font-medium text-[var(--rs-ink)]">
            Ret nedeni
            <textarea
              className="min-h-32 rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface)] px-4 py-3 text-sm leading-6 text-[var(--rs-ink)] outline-none transition focus:border-[var(--rs-accent)] focus:ring-4 focus:ring-[rgba(99_102_241_/_0.18)]"
              maxLength={reasonMaxLength}
              onChange={(event) => setReason(event.target.value)}
              placeholder="Ret gerekçesini yaz."
              required
              value={reason}
            />
          </label>
          <div className="flex justify-end text-xs text-[var(--rs-muted)]">
            {reason.length}/{reasonMaxLength}
          </div>
        </div>
        <div className="mt-6 flex flex-col gap-3 sm:flex-row sm:justify-end">
          <Button disabled={isSubmitting} onClick={onDismiss} type="button" variant="secondary">
            Vazgeç
          </Button>
          <Button disabled={isSubmitting || !reason.trim()} type="submit" variant="secondary">
            {isSubmitting ? "Reddediliyor" : "Closure reddet"}
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

type PlatformClosureExecuteDialogProps = {
  closureCase: PlatformClosureCase;
  onDismiss: () => void;
};

export function PlatformClosureExecuteDialog({
  closureCase,
  onDismiss
}: PlatformClosureExecuteDialogProps) {
  const router = useRouter();
  const [confirmation, setConfirmation] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [toast, setToast] = useState<string | null>(null);

  function showToast(message: string) {
    setToast(message);
    window.setTimeout(() => setToast(null), 3600);
  }

  const confirmPhrase = `KAPAT ${shortGuid(closureCase.userAccountId)}`;

  async function handleExecute(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (confirmation !== confirmPhrase) {
      showToast("Onay metni eşleşmiyor.");
      return;
    }

    setIsSubmitting(true);

    try {
      const result = await apiClient.POST(
        "/api/admin/abuse/closure-cases/{closureCaseId}/execute",
        {
          params: {
            path: {
              closureCaseId: closureCase.closureCaseId ?? ""
            }
          }
        }
      );

      if (!result.response.ok) {
        showToast(getClosureReviewErrorCopy(result.response.status));
        return;
      }

      onDismiss();
      showToast("Closure execution başlatıldı.");
      router.refresh();
    } catch {
      showToast("Closure execution şu anda başlatılamadı.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <DialogOverlay onEscapeKeyDown={onDismiss}>
      <DialogFormPanel onSubmit={(event) => void handleExecute(event)}>
        <div className="space-y-4">
          <h2 className="text-3xl font-semibold tracking-[-0.06em] text-[var(--rs-ink)]">
            Closure execution
          </h2>
          <p className="text-sm leading-7 text-[var(--rs-muted)]">
            Bu işlem Identity hesabını kapatır, aktif tenant membership&apos;leri
            revoke eder ve Admin completion kaydı oluşturur. Geri alınamaz.
          </p>
        </div>
        <div className="mt-6 grid gap-4">
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
            disabled={isSubmitting || confirmation !== confirmPhrase}
            type="submit"
            variant="danger"
          >
            {isSubmitting ? "Yürütülüyor" : "Hesabı kalıcı kapat"}
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

function getClosureReviewErrorCopy(status: number) {
  if (status === 400) {
    return "Closure inceleme isteği geçerli değil.";
  }
  if (status === 401) {
    return "Platform oturumu doğrulanamadı; tekrar giriş gerekebilir.";
  }
  if (status === 403) {
    return "Bu aksiyon için PlatformAdmin step-up oturumu gerekiyor.";
  }
  if (status === 404) {
    return "Closure case bulunamadı.";
  }
  if (status === 409) {
    return "Closure durumu bu aksiyona izin vermiyor.";
  }
  if (status === 422) {
    return "Backend kuralı bu aksiyonu reddetti.";
  }
  if (status === 429) {
    return "Platform operasyon rate limit'i devrede.";
  }
  return "Closure aksiyonu uygulanamadı.";
}

function shortGuid(value?: string | null) {
  if (!value) {
    return "Bilgi yok";
  }
  return `${value.slice(0, 8)}...`;
}
