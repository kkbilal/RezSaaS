"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { apiClient } from "@/shared/api/client";
import { Button } from "@/shared/ui/button";
import { DialogFormPanel, DialogOverlay } from "@/shared/ui/dialog";

const reasonMaxLength = 300;

type PlatformAppealReviewDialogProps = {
  appealId: string;
  onDismiss: () => void;
};

export function PlatformAppealAcceptDialog({
  appealId,
  onDismiss
}: PlatformAppealReviewDialogProps) {
  const router = useRouter();
  const [reason, setReason] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [toast, setToast] = useState<string | null>(null);

  function showToast(message: string) {
    setToast(message);
    window.setTimeout(() => setToast(null), 3600);
  }

  async function handleAccept(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!reason.trim()) {
      showToast("Kabul nedeni zorunludur.");
      return;
    }

    setIsSubmitting(true);

    try {
      const result = await apiClient.POST(
        "/api/admin/abuse/appeals/{appealId}/accept",
        {
          body: { reason: reason.trim() },
          params: {
            path: { appealId }
          }
        }
      );

      if (!result.response.ok) {
        showToast(getAppealErrorCopy(result.response.status));
        return;
      }

      onDismiss();
      showToast("İtiraz kabul edildi; hedef kayıt güncellendi.");
      router.refresh();
    } catch {
      showToast("İtiraz kabulü şu anda uygulanamadı.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <DialogOverlay onEscapeKeyDown={onDismiss}>
      <DialogFormPanel onSubmit={(event) => void handleAccept(event)}>
        <div className="space-y-4">
          <h2 className="text-3xl font-semibold tracking-[-0.06em] text-[var(--rs-ink)]">
            İtirazı kabul et
          </h2>
          <p className="text-sm leading-7 text-[var(--rs-muted)]">
            Kabul edilen itiraz hedef strike/sanction&apos;ı revoke eder veya
            closure case&apos;i iptal eder.
          </p>
        </div>
        <div className="mt-6 grid gap-4">
          <label className="grid gap-2 text-sm font-medium text-[var(--rs-ink)]">
            Kabul nedeni
            <textarea
              className="min-h-32 rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface)] px-4 py-3 text-sm leading-6 text-[var(--rs-ink)] outline-none transition focus:border-[var(--rs-accent)] focus:ring-4 focus:ring-[rgba(99_102_241_/_0.18)]"
              maxLength={reasonMaxLength}
              onChange={(event) => setReason(event.target.value)}
              placeholder="İtiraz kabul gerekçesini yaz."
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
          <Button disabled={isSubmitting || !reason.trim()} type="submit" variant="primary">
            {isSubmitting ? "Kabul ediliyor" : "İtirazı kabul et"}
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

export function PlatformAppealRejectDialog({
  appealId,
  onDismiss
}: PlatformAppealReviewDialogProps) {
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
        "/api/admin/abuse/appeals/{appealId}/reject",
        {
          body: { reason: reason.trim() },
          params: {
            path: { appealId }
          }
        }
      );

      if (!result.response.ok) {
        showToast(getAppealErrorCopy(result.response.status));
        return;
      }

      onDismiss();
      showToast("İtiraz reddedildi.");
      router.refresh();
    } catch {
      showToast("İtiraz reddi şu anda uygulanamadı.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <DialogOverlay onEscapeKeyDown={onDismiss}>
      <DialogFormPanel onSubmit={(event) => void handleReject(event)}>
        <div className="space-y-4">
          <h2 className="text-3xl font-semibold tracking-[-0.06em] text-[var(--rs-ink)]">
            İtirazı reddet
          </h2>
          <p className="text-sm leading-7 text-[var(--rs-muted)]">
            Mevcut strike, sanction veya closure case kararı korunur.
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
            {isSubmitting ? "Reddediliyor" : "İtirazı reddet"}
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

function getAppealErrorCopy(status: number) {
  if (status === 400) {
    return "İtiraz inceleme isteği geçerli değil.";
  }
  if (status === 401) {
    return "Platform oturumu doğrulanamadı; tekrar giriş gerekebilir.";
  }
  if (status === 403) {
    return "Bu aksiyon için PlatformAdmin step-up oturumu gerekiyor.";
  }
  if (status === 404) {
    return "İtiraz bulunamadı.";
  }
  if (status === 409) {
    return "İtiraz daha önce karara bağlanmış olabilir.";
  }
  if (status === 422) {
    return "Backend kuralı bu incelemeyi reddetti.";
  }
  if (status === 429) {
    return "Platform operasyon rate limit'i devrede.";
  }
  return "İtiraz incelemesi uygulanamadı.";
}
