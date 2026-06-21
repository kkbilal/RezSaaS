"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { apiClient } from "@/shared/api/client";
import { Button } from "@/shared/ui/button";
import { DialogFormPanel, DialogOverlay } from "@/shared/ui/dialog";

const reasonMaxLength = 300;

type SanctionType = "Warning" | "Cooldown" | "TemporaryBan";

type PlatformSanctionApplyDialogProps = {
  onDismiss: () => void;
  userAccountId: string;
};

const sanctionOptions: { label: string; value: SanctionType; description: string; maxHours: number }[] = [
  {
    label: "Warning (Uyarı)",
    value: "Warning",
    description: "Booking'i bloklamaz; kullanıcıya uyarı gösterilir.",
    maxHours: 0
  },
  {
    label: "Cooldown (Soğutma)",
    value: "Cooldown",
    description: "En fazla 24 saat boyunca yeni booking request engellenir.",
    maxHours: 24
  },
  {
    label: "Temporary Ban (Geçici yasak)",
    value: "TemporaryBan",
    description: "24-72 saat boyunca hesap kısıtlanır.",
    maxHours: 72
  }
];

export function PlatformSanctionApplyDialog({
  onDismiss,
  userAccountId
}: PlatformSanctionApplyDialogProps) {
  const router = useRouter();
  const [sanctionType, setSanctionType] = useState<SanctionType>("Warning");
  const [reason, setReason] = useState("");
  const [endsAtUtc, setEndsAtUtc] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [toast, setToast] = useState<string | null>(null);

  function showToast(message: string) {
    setToast(message);
    window.setTimeout(() => setToast(null), 3600);
  }

  const selectedConfig = sanctionOptions.find((o) => o.value === sanctionType);

  async function handleApply(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!reason.trim()) {
      showToast("Yaptırım nedeni zorunludur.");
      return;
    }

    if (sanctionType !== "Warning" && !endsAtUtc) {
      showToast("Bitiş zamanı zorunludur.");
      return;
    }

    setIsSubmitting(true);

    try {
      const result = await apiClient.POST(
        "/api/admin/abuse/users/{userAccountId}/sanctions",
        {
          body: {
            type: sanctionType,
            reason: reason.trim(),
            endsAtUtc: endsAtUtc ? `${endsAtUtc}:00Z` : undefined
          },
          params: {
            path: { userAccountId }
          }
        }
      );

      if (!result.response.ok) {
        showToast(getSanctionErrorCopy(result.response.status));
        return;
      }

      onDismiss();
      showToast("Yaptırım uygulandı.");
      router.refresh();
    } catch {
      showToast("Yaptırım şu anda uygulanamadı.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <DialogOverlay onEscapeKeyDown={onDismiss}>
      <DialogFormPanel
        onSubmit={(event) => void handleApply(event)}
      >
        <div className="space-y-4">
          <h2
            className="text-3xl font-semibold tracking-[-0.06em] text-[var(--rs-ink)]"
            id="platform-sanction-title"
          >
            Yaptırım uygula
          </h2>
          <p
            className="text-sm leading-7 text-[var(--rs-muted)]"
            id="platform-sanction-description"
          >
            Kalıcı kapatma bu ekrandan yapılmaz; closure case workflow'u kullanılır.
          </p>
        </div>

        <div className="mt-6 grid gap-4">
          <label className="grid gap-2 text-sm font-medium text-[var(--rs-ink)]">
            Yaptırım türü
            <select
              className="min-h-12 rounded-2xl border border-[var(--rs-border)] bg-white px-4 text-sm text-[var(--rs-ink)] outline-none transition focus:border-[var(--rs-border-strong)] focus:ring-4 focus:ring-[rgb(5_26_36_/_0.08)]"
              onChange={(event) => {
                setSanctionType(event.target.value as SanctionType);
                if (event.target.value === "Warning") {
                  setEndsAtUtc("");
                }
              }}
              value={sanctionType}
            >
              {sanctionOptions.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>
            {selectedConfig ? (
              <span className="text-xs text-[var(--rs-muted)]">
                {selectedConfig.description}
              </span>
            ) : null}
          </label>

          {sanctionType !== "Warning" ? (
            <label className="grid gap-2 text-sm font-medium text-[var(--rs-ink)]">
              Bitiş zamanı (UTC)
              <input
                className="min-h-12 rounded-2xl border border-[var(--rs-border)] bg-white px-4 text-sm text-[var(--rs-ink)] outline-none transition focus:border-[var(--rs-border-strong)] focus:ring-4 focus:ring-[rgb(5_26_36_/_0.08)]"
                max={selectedConfig ? getMaxEndDate(selectedConfig.maxHours) : ""}
                onChange={(event) => setEndsAtUtc(event.target.value)}
                required
                type="datetime-local"
                value={endsAtUtc}
              />
              {selectedConfig && endsAtUtc ? (
                <span className="text-xs text-[var(--rs-muted)]">
                  En geç: {getMaxEndDate(selectedConfig.maxHours)}
                </span>
              ) : null}
            </label>
          ) : null}

          <label className="grid gap-2 text-sm font-medium text-[var(--rs-ink)]">
            Yaptırım nedeni
            <textarea
              className="min-h-32 rounded-2xl border border-[var(--rs-border)] bg-white px-4 py-3 text-sm leading-6 text-[var(--rs-ink)] outline-none transition focus:border-[var(--rs-border-strong)] focus:ring-4 focus:ring-[rgb(5_26_36_/_0.08)]"
              maxLength={reasonMaxLength}
              onChange={(event) => setReason(event.target.value)}
              placeholder="Yaptırım gerekçesini yaz."
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
            variant="danger"
          >
            {isSubmitting ? "Uygulanıyor" : "Yaptırım uygula"}
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

function getMaxEndDate(maxHours: number): string {
  const date = new Date(Date.now() + maxHours * 60 * 60 * 1000);
  return date.toISOString().slice(0, 16);
}

function getSanctionErrorCopy(status: number) {
  if (status === 400) {
    return "Yaptırım isteği geçerli değil.";
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
    return "Kullanıcının zaten aktif bir yaptırımı olabilir.";
  }
  if (status === 422) {
    return "Backend kuralı bu yaptırımı reddetti.";
  }
  if (status === 429) {
    return "Platform operasyon rate limit'i devrede.";
  }
  return "Yaptırım uygulanamadı.";
}
