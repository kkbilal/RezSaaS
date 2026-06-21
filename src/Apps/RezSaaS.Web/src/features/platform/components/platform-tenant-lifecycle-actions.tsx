"use client";

import { useMemo, useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import type { PlatformTenantDetail } from "@/features/platform/api/get-platform-tenants-overview";
import {
  getTenantLifecycleActionAvailability,
  getTenantLifecycleActionConfig,
  tenantLifecycleReasonMaxLength,
  validateTenantLifecycleActionDraft,
  type PlatformTenantLifecycleAction
} from "@/features/platform/lib/platform-action-policy";
import { apiClient } from "@/shared/api/client";
import { Button } from "@/shared/ui/button";
import { DialogFormPanel, DialogOverlay } from "@/shared/ui/dialog";
import { StatusBadge } from "@/shared/ui/status-badge";

type LifecycleDialogState = {
  action: PlatformTenantLifecycleAction;
  confirmation: string;
  reason: string;
};

type PlatformTenantLifecycleActionsProps = {
  tenant: PlatformTenantDetail;
};

const lifecycleActions: PlatformTenantLifecycleAction[] = [
  "suspend",
  "reactivate",
  "close"
];

export function PlatformTenantLifecycleActions({
  tenant
}: PlatformTenantLifecycleActionsProps) {
  const router = useRouter();
  const [dialog, setDialog] = useState<LifecycleDialogState | null>(null);
  const [toast, setToast] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const availableActions = useMemo(
    () =>
      lifecycleActions.map((action) => ({
        action,
        availabilityError: getTenantLifecycleActionAvailability(action, tenant),
        config: getTenantLifecycleActionConfig(action, tenant)
      })),
    [tenant]
  );

  function showToast(message: string) {
    setToast(message);
    window.setTimeout(() => setToast(null), 3600);
  }

  function openDialog(action: PlatformTenantLifecycleAction) {
    setDialog({
      action,
      confirmation: "",
      reason: ""
    });
  }

  async function submitLifecycleAction(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!dialog) {
      return;
    }

    const validation = validateTenantLifecycleActionDraft(dialog.action, tenant, {
      confirmation: dialog.confirmation,
      reason: dialog.reason
    });

    if (validation.errors.length > 0) {
      showToast(validation.errors[0] ?? "Lifecycle aksiyonu doğrulanamadı.");
      return;
    }

    if (!tenant.tenantId) {
      showToast("Tenant kimliği doğrulanamadı.");
      return;
    }

    setIsSubmitting(true);

    try {
      const result = await postLifecycleAction(
        dialog.action,
        tenant.tenantId,
        validation.normalizedReason
      );

      if (!result.response.ok || !result.data) {
        showToast(getLifecycleErrorCopy(result.response.status));
        return;
      }

      setDialog(null);
      showToast("Tenant lifecycle aksiyonu uygulandı; snapshot yenileniyor.");
      router.refresh();
    } catch {
      showToast("Tenant lifecycle aksiyonu şu anda uygulanamadı.");
    } finally {
      setIsSubmitting(false);
    }
  }

  const activeDialogConfig = dialog
    ? getTenantLifecycleActionConfig(dialog.action, tenant)
    : null;
  const activeValidation =
    dialog && activeDialogConfig
      ? validateTenantLifecycleActionDraft(dialog.action, tenant, {
          confirmation: dialog.confirmation,
          reason: dialog.reason
        })
      : null;

  return (
    <section className="mt-6 rounded-[1.5rem] border border-[var(--rs-border)] bg-white p-4">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div className="max-w-2xl">
          <div className="flex flex-wrap items-center gap-3">
            <h3 className="font-semibold tracking-[-0.03em] text-[var(--rs-ink)]">
              Lifecycle aksiyonları
            </h3>
            <StatusBadge status={tenant.status ?? "Unknown"} />
          </div>
          <p className="mt-2 text-sm leading-6 text-[var(--rs-muted)]">
            Bu aksiyonlar `PlatformAdminWithStepUp` ile gerçek admin API çağrılarına
            gider. Reason zorunludur; tenant header gönderilmez ve Closed durum
            geri alınabilir gibi gösterilmez.
          </p>
        </div>

        <div className="flex flex-wrap gap-2">
          {availableActions.map(({ action, availabilityError, config }) => (
            <Button
              disabled={Boolean(availabilityError) || isSubmitting}
              key={action}
              onClick={() => openDialog(action)}
              title={availabilityError ?? config.description}
              type="button"
              variant={config.isDangerous ? "danger" : "secondary"}
            >
              {config.label}
            </Button>
          ))}
        </div>
      </div>

      <div className="mt-4 grid gap-3 text-xs text-[var(--rs-muted)] md:grid-cols-3">
        {availableActions.map(({ action, availabilityError, config }) => (
          <p
            className="rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface-muted)] p-3"
            key={action}
          >
            <span className="font-medium text-[var(--rs-ink)]">{config.label}: </span>
            {availabilityError ?? "Açık"}
          </p>
        ))}
      </div>

      {dialog && activeDialogConfig ? (
        <DialogOverlay onEscapeKeyDown={() => setDialog(null)}>
          <DialogFormPanel
            descriptionId="platform-tenant-lifecycle-description"
            onSubmit={(event) => void submitLifecycleAction(event)}
            titleId="platform-tenant-lifecycle-title"
          >
            <div className="space-y-4">
              <StatusBadge status={tenant.status ?? "Unknown"} />
              <h2
                className="text-3xl font-semibold tracking-[-0.06em] text-[var(--rs-ink)]"
                id="platform-tenant-lifecycle-title"
              >
                {activeDialogConfig.title}
              </h2>
              <p
                className="text-sm leading-7 text-[var(--rs-muted)]"
                id="platform-tenant-lifecycle-description"
              >
                {activeDialogConfig.description}
              </p>
            </div>

            <div className="mt-6 grid gap-4">
              <label className="grid gap-2 text-sm font-medium text-[var(--rs-ink)]">
                Operasyon nedeni
                <textarea
                  className="min-h-32 rounded-2xl border border-[var(--rs-border)] bg-white px-4 py-3 text-sm leading-6 text-[var(--rs-ink)] outline-none transition focus:border-[var(--rs-border-strong)] focus:ring-4 focus:ring-[rgb(5_26_36_/_0.08)]"
                  maxLength={tenantLifecycleReasonMaxLength}
                  onChange={(event) =>
                    setDialog({
                      ...dialog,
                      reason: event.target.value
                    })
                  }
                  placeholder="PII, secret, token veya ham iletişim bilgisi içermeyen kısa gerekçe yaz."
                  required
                  value={dialog.reason}
                />
              </label>
              <div className="flex flex-col gap-2 text-xs text-[var(--rs-muted)] sm:flex-row sm:items-center sm:justify-between">
                <span>Bu metin auditlenir; müşteri-facing içerik değildir.</span>
                <span>
                  {dialog.reason.length}/{tenantLifecycleReasonMaxLength}
                </span>
              </div>

              <label className="grid gap-2 text-sm font-medium text-[var(--rs-ink)]">
                Onay metni
                <input
                  className="min-h-12 rounded-2xl border border-[var(--rs-border)] bg-white px-4 text-sm text-[var(--rs-ink)] outline-none transition focus:border-[var(--rs-border-strong)] focus:ring-4 focus:ring-[rgb(5_26_36_/_0.08)]"
                  onChange={(event) =>
                    setDialog({
                      ...dialog,
                      confirmation: event.target.value
                    })
                  }
                  placeholder={activeDialogConfig.confirmPhrase}
                  value={dialog.confirmation}
                />
                <span className="text-xs text-[var(--rs-muted)]">
                  Tam olarak{" "}
                  <code className="rounded bg-[var(--rs-surface-muted)] px-1.5 py-0.5 font-mono text-[var(--rs-ink)]">
                    {activeDialogConfig.confirmPhrase}
                  </code>{" "}
                  yaz.
                </span>
              </label>

              {activeValidation && activeValidation.errors.length > 0 ? (
                <div className="rounded-2xl border border-[var(--rs-warning-border)] bg-[var(--rs-warning-soft)] px-4 py-3 text-sm leading-6 text-[var(--rs-warning)]">
                  {activeValidation.errors.map((error) => (
                    <p key={error}>{error}</p>
                  ))}
                </div>
              ) : null}
            </div>

            <div className="mt-6 flex flex-col gap-3 sm:flex-row sm:justify-end">
              <Button
                disabled={isSubmitting}
                onClick={() => setDialog(null)}
                type="button"
                variant="secondary"
              >
                Vazgeç
              </Button>
              <Button
                disabled={
                  isSubmitting ||
                  Boolean(activeValidation && activeValidation.errors.length > 0)
                }
                type="submit"
                variant={activeDialogConfig.isDangerous ? "danger" : "primary"}
              >
                {isSubmitting ? "Uygulanıyor" : activeDialogConfig.label}
              </Button>
            </div>
          </DialogFormPanel>
        </DialogOverlay>
      ) : null}

      {toast ? (
        <div className="fixed bottom-5 left-1/2 z-50 w-[calc(100%-2rem)] max-w-xl -translate-x-1/2 rounded-full border border-[var(--rs-border)] bg-white px-5 py-3 text-sm text-[var(--rs-ink)] shadow-[var(--rs-shadow-card)]">
          {toast}
        </div>
      ) : null}
    </section>
  );
}

function postLifecycleAction(
  action: PlatformTenantLifecycleAction,
  tenantId: string,
  reason: string
) {
  if (action === "suspend") {
    return apiClient.POST("/api/admin/tenants/{tenantId}/suspend", {
      body: { reason },
      params: {
        path: {
          tenantId
        }
      }
    });
  }

  if (action === "reactivate") {
    return apiClient.POST("/api/admin/tenants/{tenantId}/reactivate", {
      body: { reason },
      params: {
        path: {
          tenantId
        }
      }
    });
  }

  return apiClient.POST("/api/admin/tenants/{tenantId}/close", {
    body: { reason },
    params: {
      path: {
        tenantId
      }
    }
  });
}

function getLifecycleErrorCopy(status: number) {
  if (status === 400) {
    return "Reason veya lifecycle isteği geçerli değil.";
  }

  if (status === 401) {
    return "Platform oturumu doğrulanamadı; tekrar giriş gerekebilir.";
  }

  if (status === 403) {
    return "Bu aksiyon için PlatformAdmin step-up oturumu gerekiyor.";
  }

  if (status === 404) {
    return "Tenant bulunamadı veya artık görüntülenemiyor.";
  }

  if (status === 409) {
    return "Tenant lifecycle durumu bu aksiyona izin vermiyor.";
  }

  if (status === 422) {
    return "Backend lifecycle kuralı aksiyonu reddetti.";
  }

  if (status === 429) {
    return "Platform operasyon rate limit'i devrede.";
  }

  return "Tenant lifecycle aksiyonu uygulanamadı.";
}
