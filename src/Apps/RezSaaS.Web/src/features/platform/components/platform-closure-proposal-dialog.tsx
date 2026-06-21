"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { apiClient } from "@/shared/api/client";
import { Button } from "@/shared/ui/button";
import { DialogFormPanel, DialogOverlay } from "@/shared/ui/dialog";

const fieldMaxLength = 300;

type PlatformClosureProposalDialogProps = {
  onDismiss: () => void;
  userAccountId: string;
};

export function PlatformClosureProposalDialog({
  onDismiss,
  userAccountId
}: PlatformClosureProposalDialogProps) {
  const router = useRouter();
  const [internalReason, setInternalReason] = useState("");
  const [customerNotice, setCustomerNotice] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [toast, setToast] = useState<string | null>(null);

  function showToast(message: string) {
    setToast(message);
    window.setTimeout(() => setToast(null), 3600);
  }

  async function handlePropose(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!internalReason.trim()) {
      showToast("Internal reason zorunludur.");
      return;
    }

    if (!customerNotice.trim()) {
      showToast("Customer notice zorunludur.");
      return;
    }

    setIsSubmitting(true);

    try {
      const result = await apiClient.POST(
        "/api/admin/abuse/users/{userAccountId}/closure-cases",
        {
          body: {
            internalReason: internalReason.trim(),
            customerNotice: customerNotice.trim()
          },
          params: {
            path: { userAccountId }
          }
        }
      );

      if (!result.response.ok) {
        showToast(getClosureErrorCopy(result.response.status));
        return;
      }

      onDismiss();
      showToast("Closure case proposal oluşturuldu.");
      router.refresh();
    } catch {
      showToast("Closure proposal şu anda oluşturulamadı.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <DialogOverlay onEscapeKeyDown={onDismiss}>
      <DialogFormPanel onSubmit={(event) => void handlePropose(event)}>
        <div className="space-y-4">
          <h2 className="text-3xl font-semibold tracking-[-0.06em] text-[var(--rs-ink)]">
            Kalıcı hesap kapatma öner
          </h2>
          <p className="text-sm leading-7 text-[var(--rs-muted)]">
            İki farklı PlatformAdmin with step-up onayı, bildirim teslimatı ve
            itiraz penceresinden sonra execution mümkün olur.
          </p>
        </div>

        <div className="mt-6 grid gap-4">
          <label className="grid gap-2 text-sm font-medium text-[var(--rs-ink)]">
            Internal reason
            <textarea
              className="min-h-32 rounded-2xl border border-[var(--rs-border)] bg-white px-4 py-3 text-sm leading-6 text-[var(--rs-ink)] outline-none transition focus:border-[var(--rs-border-strong)] focus:ring-4 focus:ring-[rgb(5_26_36_/_0.08)]"
              maxLength={fieldMaxLength}
              onChange={(event) => setInternalReason(event.target.value)}
              placeholder="Platform iç gerekçe; müşteriye gösterilmez."
              required
              value={internalReason}
            />
          </label>
          <div className="flex justify-end text-xs text-[var(--rs-muted)]">
            {internalReason.length}/{fieldMaxLength}
          </div>

          <label className="grid gap-2 text-sm font-medium text-[var(--rs-ink)]">
            Customer notice
            <textarea
              className="min-h-32 rounded-2xl border border-[var(--rs-border)] bg-white px-4 py-3 text-sm leading-6 text-[var(--rs-ink)] outline-none transition focus:border-[var(--rs-border-strong)] focus:ring-4 focus:ring-[rgb(5_26_36_/_0.08)]"
              maxLength={fieldMaxLength}
              onChange={(event) => setCustomerNotice(event.target.value)}
              placeholder="Müşteriye e-postayla gidecek bildirim metni."
              required
              value={customerNotice}
            />
          </label>
          <div className="flex justify-end text-xs text-[var(--rs-muted)]">
            {customerNotice.length}/{fieldMaxLength}
          </div>
        </div>

        <div className="mt-6 flex flex-col gap-3 sm:flex-row sm:justify-end">
          <Button disabled={isSubmitting} onClick={onDismiss} type="button" variant="secondary">
            Vazgeç
          </Button>
          <Button
            disabled={isSubmitting || !internalReason.trim() || !customerNotice.trim()}
            type="submit"
            variant="danger"
          >
            {isSubmitting ? "Oluşturuluyor" : "Closure case oluştur"}
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

function getClosureErrorCopy(status: number) {
  if (status === 400) {
    return "Closure isteği geçerli değil.";
  }
  if (status === 401) {
    return "Platform oturumu doğrulanamadı; tekrar giriş gerekebilir.";
  }
  if (status === 403) {
    return "Bu aksiyon için PlatformAdmin step-up oturumu gerekiyor.";
  }
  if (status === 404) {
    return "Kullanıcı bulunamadı.";
  }
  if (status === 409) {
    return "Kullanıcının zaten aktif closure case kaydı olabilir.";
  }
  if (status === 422) {
    return "Backend kuralı bu proposal&apos;ı reddetti.";
  }
  if (status === 429) {
    return "Platform operasyon rate limit'i devrede.";
  }
  return "Closure proposal oluşturulamadı.";
}
