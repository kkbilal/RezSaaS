"use client";

import { useRouter } from "next/navigation";
import { type FormEvent, useState } from "react";
import { createTenantApiClient } from "@/shared/api/client";
import type { ApiSchema } from "@/shared/api/types";
import { Button } from "@/shared/ui/button";
import { Card, CardDescription, CardHeader, CardTitle } from "@/shared/ui/card";
import { FormField, TextInput } from "@/shared/ui/form-field";
import { StatusBadge } from "@/shared/ui/status-badge";

type BusinessProfileSettingsRequest =
  ApiSchema<"BusinessProfileSettingsRequest">;

export type BusinessProfileSettingsDraft = Required<
  {
    description: NonNullable<BusinessProfileSettingsRequest["description"]>;
    displayName: NonNullable<BusinessProfileSettingsRequest["displayName"]>;
    publicRules: NonNullable<BusinessProfileSettingsRequest["publicRules"]>;
    seoDescription: NonNullable<
      BusinessProfileSettingsRequest["seoDescription"]
    >;
    seoTitle: NonNullable<BusinessProfileSettingsRequest["seoTitle"]>;
    staffDisplayPolicy: NonNullable<
      BusinessProfileSettingsRequest["staffDisplayPolicy"]
    >;
  }
>;

type BusinessProfileSettingsFormProps = {
  canManage: boolean;
  initial: BusinessProfileSettingsDraft;
  tenantId?: string | null;
};

const textAreaClassName =
  "min-h-28 w-full rounded-2xl border border-[var(--rs-border)] bg-white px-4 py-3 text-sm leading-6 text-[var(--rs-ink)] shadow-[var(--rs-shadow-soft)] outline-none transition placeholder:text-[var(--rs-muted)] focus:border-[var(--rs-border-strong)] focus:ring-4 focus:ring-[rgb(5_26_36_/_0.08)] disabled:cursor-not-allowed disabled:opacity-60";

export function BusinessProfileSettingsForm({
  canManage,
  initial,
  tenantId
}: BusinessProfileSettingsFormProps) {
  const router = useRouter();
  const [draft, setDraft] = useState(initial);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [success, setSuccess] = useState<string | null>(null);
  const isDisabled = !canManage || !tenantId || isSubmitting;

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setSuccess(null);

    if (!tenantId) {
      setError("İşletme tenant bağlamı doğrulanamadı.");
      return;
    }

    if (!canManage) {
      setError("Bu ayarı yalnızca BusinessOwner rolü güncelleyebilir.");
      return;
    }

    if (draft.displayName.trim().length < 2) {
      setError("İşletme adı en az 2 karakter olmalı.");
      return;
    }

    setIsSubmitting(true);

    try {
      const response = await createTenantApiClient(tenantId).PATCH(
        "/api/business/settings/profile",
        {
          body: {
            description: draft.description.trim(),
            displayName: draft.displayName.trim(),
            publicRules: draft.publicRules.trim(),
            seoDescription: draft.seoDescription.trim(),
            seoTitle: draft.seoTitle.trim(),
            staffDisplayPolicy: draft.staffDisplayPolicy
          }
        }
      );

      if (!response.response.ok) {
        setError(getSettingsErrorCopy(response.response.status));
        return;
      }

      setSuccess("Public profil ayarları kaydedildi.");
      router.refresh();
    } catch {
      setError("Ayarlar şu anda kaydedilemedi. Lütfen tekrar dene.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <Card className="p-5">
      <CardHeader>
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <CardTitle>Public profil ayarları</CardTitle>
            <CardDescription className="mt-2">
              Bu form gerçek backend kontratına bağlıdır; tenant header merkezi
              client tarafından eklenir ve endpoint BusinessOwner authz uygular.
            </CardDescription>
          </div>
          <StatusBadge status={canManage ? "Healthy" : "Degraded"} />
        </div>
      </CardHeader>

      {!canManage ? (
        <p className="mt-5 rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface-muted)] p-4 text-sm leading-6 text-[var(--rs-muted)]">
          BranchManager ve Staff rolleri tenant-wide public profil metnini
          değiştiremez. Şube/personel kapsamlı ayar formları ayrı endpointlerle
          açılacak.
        </p>
      ) : null}

      <form className="mt-6 space-y-5" onSubmit={handleSubmit}>
        <div className="grid gap-4 lg:grid-cols-2">
          <FormField
            hint="Public profilde görünen işletme adı."
            label="İşletme adı"
          >
            <TextInput
              disabled={isDisabled}
              maxLength={200}
              onChange={(event) =>
                setDraft((current) => ({
                  ...current,
                  displayName: event.target.value
                }))
              }
              value={draft.displayName}
            />
          </FormField>

          <FormField
            hint="Public personel görünürlüğü; kapasite hesabını etkilemez."
            label="Personel görünürlüğü"
          >
            <select
              className="min-h-12 w-full rounded-2xl border border-[var(--rs-border)] bg-white px-4 text-sm text-[var(--rs-ink)] shadow-[var(--rs-shadow-soft)] outline-none transition focus:border-[var(--rs-border-strong)] focus:ring-4 focus:ring-[rgb(5_26_36_/_0.08)] disabled:cursor-not-allowed disabled:opacity-60"
              disabled={isDisabled}
              onChange={(event) =>
                setDraft((current) => ({
                  ...current,
                  staffDisplayPolicy: event.target.value
                }))
              }
              value={draft.staffDisplayPolicy}
            >
              <option value="ShowNames">Personel isimlerini göster</option>
              <option value="HideNames">Personel isimlerini gizle</option>
            </select>
          </FormField>
        </div>

        <FormField
          hint="En fazla 600 karakter; PII veya secret yazma."
          label="Public açıklama"
        >
          <textarea
            className={textAreaClassName}
            disabled={isDisabled}
            maxLength={600}
            onChange={(event) =>
              setDraft((current) => ({
                ...current,
                description: event.target.value
              }))
            }
            value={draft.description}
          />
        </FormField>

        <FormField
          hint="Randevu öncesi müşteri beklentisini netleştiren kısa kurallar."
          label="Public kurallar"
        >
          <textarea
            className={textAreaClassName}
            disabled={isDisabled}
            maxLength={1000}
            onChange={(event) =>
              setDraft((current) => ({
                ...current,
                publicRules: event.target.value
              }))
            }
            value={draft.publicRules}
          />
        </FormField>

        <div className="grid gap-4 lg:grid-cols-2">
          <FormField hint="En fazla 120 karakter." label="SEO başlık">
            <TextInput
              disabled={isDisabled}
              maxLength={120}
              onChange={(event) =>
                setDraft((current) => ({
                  ...current,
                  seoTitle: event.target.value
                }))
              }
              value={draft.seoTitle}
            />
          </FormField>

          <FormField hint="En fazla 180 karakter." label="SEO açıklama">
            <TextInput
              disabled={isDisabled}
              maxLength={180}
              onChange={(event) =>
                setDraft((current) => ({
                  ...current,
                  seoDescription: event.target.value
                }))
              }
              value={draft.seoDescription}
            />
          </FormField>
        </div>

        {error ? (
          <p className="rounded-2xl border border-[rgb(175_63_63_/_0.22)] bg-[var(--rs-danger-soft)] p-4 text-sm text-[var(--rs-danger)]">
            {error}
          </p>
        ) : null}

        {success ? (
          <p className="rounded-2xl border border-[rgb(47_122_78_/_0.22)] bg-[var(--rs-success-soft)] p-4 text-sm text-[var(--rs-success)]">
            {success}
          </p>
        ) : null}

        <div className="flex flex-wrap items-center justify-between gap-3">
          <p className="text-xs leading-5 text-[var(--rs-muted)]">
            Kayıt auditlenir; response veya log içinde raw secret/PII
            taşınmamalıdır.
          </p>
          <Button disabled={isDisabled} type="submit">
            {isSubmitting ? "Kaydediliyor..." : "Profili kaydet"}
          </Button>
        </div>
      </form>
    </Card>
  );
}

function getSettingsErrorCopy(status: number) {
  if (status === 400) {
    return "Alan formatı geçerli değil. Uzunlukları ve personel politikasını kontrol et.";
  }

  if (status === 403) {
    return "Bu ayar için BusinessOwner yetkisi gerekiyor.";
  }

  if (status === 404) {
    return "Aktif işletme profili bulunamadı.";
  }

  if (status === 409) {
    return "Bu tenant için birden fazla aktif business var; MVP ayar ekranı tek business destekler.";
  }

  if (status === 429) {
    return "Çok sık ayar kaydı denendi. Kısa süre sonra tekrar dene.";
  }

  return "Ayarlar kaydedilemedi. Lütfen tekrar dene.";
}
