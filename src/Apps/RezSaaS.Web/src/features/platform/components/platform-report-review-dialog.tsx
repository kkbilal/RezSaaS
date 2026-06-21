"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { apiClient } from "@/shared/api/client";
import { Button } from "@/shared/ui/button";
import { DialogFormPanel, DialogOverlay } from "@/shared/ui/dialog";

const reasonMaxLength = 300;

type PlatformReportReviewDialogProps = {
  onDismiss: () => void;
  reportId: string;
};

export function PlatformReportConfirmDialog({
  onDismiss,
  reportId
}: PlatformReportReviewDialogProps) {
  const router = useRouter();
  const [reason, setReason] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [toast, setToast] = useState<string | null>(null);

  function showToast(message: string) {
    setToast(message);
    window.setTimeout(() => setToast(null), 3600);
  }

  async function handleConfirm(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!reason.trim()) {
      showToast("İnceleme nedeni zorunludur.");
      return;
    }

    setIsSubmitting(true);

    try {
      const result = await apiClient.POST(
        "/api/admin/abuse/reports/{reportId}/confirm",
        {
          body: { reason: reason.trim() },
          params: {
            path: { reportId }
          }
        }
      );

      if (!result.response.ok) {
        showToast(getReviewErrorCopy(result.response.status));
        return;
      }

      onDismiss();
      showToast("Abuse raporu onaylandı; strike kaydı oluşturuldu.");
      router.refresh();
    } catch {
      showToast("Rapor onayı şu anda uygulanamadı.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <DialogOverlay onEscapeKeyDown={onDismiss}>
      <DialogFormPanel
        onSubmit={(event) => void handleConfirm(event)}
      >
        <div className="space-y-4">
          <h2
            className="text-3xl font-semibold tracking-[-0.06em] text-[var(--rs-ink)]"
            id="platform-report-confirm-title"
          >
            Abuse raporunu onayla
          </h2>
          <p
            className="text-sm leading-7 text-[var(--rs-muted)]"
            id="platform-report-confirm-description"
          >
            Bu işlem raporu kapatır ve hedef kullanıcıya bir strike kaydı ekler.
          </p>
        </div>

        <div className="mt-6 grid gap-4">
          <label className="grid gap-2 text-sm font-medium text-[var(--rs-ink)]">
            İnceleme nedeni
            <textarea
              className="min-h-32 rounded-2xl border border-[var(--rs-border)] bg-white px-4 py-3 text-sm leading-6 text-[var(--rs-ink)] outline-none transition focus:border-[var(--rs-border-strong)] focus:ring-4 focus:ring-[rgb(5_26_36_/_0.08)]"
              maxLength={reasonMaxLength}
              onChange={(event) => setReason(event.target.value)}
              placeholder="Strike gerekçesini ve inceleme notunu yaz."
              required
              value={reason}
            />
          </label>
          <div className="flex flex-col gap-2 text-xs text-[var(--rs-muted)] sm:flex-row sm:items-center sm:justify-between">
            <span>Strike kaydına ve denetim günlüğüne yazılır.</span>
            <span>{reason.length}/{reasonMaxLength}</span>
          </div>
        </div>

        <div className="mt-6 flex flex-col gap-3 sm:flex-row sm:justify-end">
          <Button
            disabled={isSubmitting}
            onClick={onDismiss}
            type="button"
            variant="secondary"
          >
            Vazgeç
          </Button>
          <Button
            disabled={isSubmitting || !reason.trim()}
            type="submit"
            variant="danger"
          >
            {isSubmitting ? "Onaylanıyor" : "Strike oluştur"}
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

export function PlatformReportDismissDialog({
  onDismiss,
  reportId
}: PlatformReportReviewDialogProps) {
  const router = useRouter();
  const [reason, setReason] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [toast, setToast] = useState<string | null>(null);

  function showToast(message: string) {
    setToast(message);
    window.setTimeout(() => setToast(null), 3600);
  }

  async function handleDismiss(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!reason.trim()) {
      showToast("Ret nedeni zorunludur.");
      return;
    }

    setIsSubmitting(true);

    try {
      const result = await apiClient.POST(
        "/api/admin/abuse/reports/{reportId}/dismiss",
        {
          body: { reason: reason.trim() },
          params: {
            path: { reportId }
          }
        }
      );

      if (!result.response.ok) {
        showToast(getReviewErrorCopy(result.response.status));
        return;
      }

      onDismiss();
      showToast("Abuse raporu reddedildi.");
      router.refresh();
    } catch {
      showToast("Rapor reddi şu anda uygulanamadı.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <DialogOverlay onEscapeKeyDown={onDismiss}>
      <DialogFormPanel
        onSubmit={(event) => void handleDismiss(event)}
      >
        <div className="space-y-4">
          <h2
            className="text-3xl font-semibold tracking-[-0.06em] text-[var(--rs-ink)]"
            id="platform-report-dismiss-title"
          >
            Abuse raporunu reddet
          </h2>
          <p
            className="text-sm leading-7 text-[var(--rs-muted)]"
            id="platform-report-dismiss-description"
          >
            Rapor kapanır; herhangi bir strike veya yaptırım uygulanmaz.
          </p>
        </div>

        <div className="mt-6 grid gap-4">
          <label className="grid gap-2 text-sm font-medium text-[var(--rs-ink)]">
            Ret nedeni
            <textarea
              className="min-h-32 rounded-2xl border border-[var(--rs-border)] bg-white px-4 py-3 text-sm leading-6 text-[var(--rs-ink)] outline-none transition focus:border-[var(--rs-border-strong)] focus:ring-4 focus:ring-[rgb(5_26_36_/_0.08)]"
              maxLength={reasonMaxLength}
              onChange={(event) => setReason(event.target.value)}
              placeholder="Ret gerekçesini yaz."
              required
              value={reason}
            />
          </label>
          <div className="flex flex-col gap-2 text-xs text-[var(--rs-muted)] sm:flex-row sm:items-center sm:justify-between">
            <span>Denetim günlüğüne yazılır.</span>
            <span>{reason.length}/{reasonMaxLength}</span>
          </div>
        </div>

        <div className="mt-6 flex flex-col gap-3 sm:flex-row sm:justify-end">
          <Button
            disabled={isSubmitting}
            onClick={onDismiss}
            type="button"
            variant="secondary"
          >
            Vazgeç
          </Button>
          <Button
            disabled={isSubmitting || !reason.trim()}
            type="submit"
            variant="secondary"
          >
            {isSubmitting ? "Reddediliyor" : "Raporu reddet"}
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

function getReviewErrorCopy(status: number) {
  if (status === 400) {
    return "Rapor inceleme isteği geçerli değil.";
  }
  if (status === 401) {
    return "Platform oturumu doğrulanamadı; tekrar giriş gerekebilir.";
  }
  if (status === 403) {
    return "Bu aksiyon için PlatformAdmin step-up oturumu gerekiyor.";
  }
  if (status === 404) {
    return "Rapor bulunamadı veya artık görüntülenemiyor.";
  }
  if (status === 409) {
    return "Rapor daha önce karara bağlanmış olabilir.";
  }
  if (status === 422) {
    return "Backend kuralı bu rapor incelemesini reddetti.";
  }
  if (status === 429) {
    return "Platform operasyon rate limit'i devrede.";
  }
  return "Rapor incelemesi uygulanamadı.";
}
