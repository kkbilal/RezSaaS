"use client";

import { Info } from "lucide-react";
import { useRouter } from "next/navigation";
import { type FormEvent, useState } from "react";
import { toast } from "sonner";
import { createTenantApiClient } from "@/shared/api/client";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { Textarea } from "@/components/ui/textarea";
import { cn } from "@/shared/lib/cn";

/**
 * PUBLIC PROFIL AYARLARI FORMU.
 *
 * TUZAK 5 ("PATCH ama davranisi PUT"): displayName, description, publicRules, seoTitle,
 * seoDescription, staffDisplayPolicy alanlari KISMI gonderilirse digerlerini SIFIRLAR.
 * Bu yuzden form her kaydetmede TUM alanlari gonderir (initial GET ile okunmus tam govde).
 *
 * ISTISNA: cancellationCutoffHours NULLABLE'dir ve gonderilmezse KORUNUR. Yine de degeri
 * elimizde oldugundan (GET yaniti) her zaman acikca gonderiyoruz -- boylece kullanicinin
 * girdigi deger kesin uygulanir.
 */

const NAME_MIN = 2;
const NAME_MAX = 200;
const DESCRIPTION_MAX = 600;
const RULES_MAX = 1000;
const SEO_TITLE_MAX = 120;
const SEO_DESCRIPTION_MAX = 180;
// Backend Business.MaxCancellationCutoffHours = 168 (7 gun).
const CANCELLATION_MIN = 0;
const CANCELLATION_MAX = 168;

export type BusinessProfileSettingsDraft = {
  description: string;
  displayName: string;
  publicRules: string;
  seoDescription: string;
  seoTitle: string;
  staffDisplayPolicy: string;
  /** String tutulur ki bos girdi + sinir kontrolu kolay olsun; gonderirken sayiya cevrilir. */
  cancellationCutoffHours: string;
};

type BusinessProfileSettingsFormProps = {
  canManage: boolean;
  initial: BusinessProfileSettingsDraft;
  tenantId?: string | null;
};

export function BusinessProfileSettingsForm({
  canManage,
  initial,
  tenantId
}: BusinessProfileSettingsFormProps) {
  const router = useRouter();
  const [draft, setDraft] = useState(initial);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const isDisabled = !canManage || !tenantId || isSubmitting;

  const nameTrimmed = draft.displayName.trim();
  const nameInvalid = nameTrimmed.length < NAME_MIN || nameTrimmed.length > NAME_MAX;

  const cutoffRaw = draft.cancellationCutoffHours.trim();
  const cutoffValue = cutoffRaw === "" ? 0 : Number(cutoffRaw);
  const cutoffInvalid =
    !Number.isInteger(cutoffValue) ||
    cutoffValue < CANCELLATION_MIN ||
    cutoffValue > CANCELLATION_MAX;

  const showNames = draft.staffDisplayPolicy !== "HideNames";

  function patch(next: Partial<BusinessProfileSettingsDraft>) {
    setDraft((current) => ({ ...current, ...next }));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!tenantId) {
      toast.error("İşletme tenant bağlamı doğrulanamadı.");
      return;
    }
    if (!canManage) {
      toast.error("Bu ayarı yalnızca işletme sahibi güncelleyebilir.");
      return;
    }
    if (nameInvalid) {
      toast.error(`İşletme adı en az ${NAME_MIN}, en fazla ${NAME_MAX} karakter olmalı.`);
      return;
    }
    if (cutoffInvalid) {
      toast.error(
        `İptal süresi 0 ile ${CANCELLATION_MAX} saat (7 gün) arasında bir tam sayı olmalı.`
      );
      return;
    }

    setIsSubmitting(true);

    try {
      // TUM alanlar her zaman gonderilir (Tuzak 5) -- kismi PATCH digerlerini sifirlar.
      const response = await createTenantApiClient(tenantId).PATCH(
        "/api/business/settings/profile",
        {
          body: {
            displayName: draft.displayName.trim(),
            description: draft.description.trim(),
            publicRules: draft.publicRules.trim(),
            seoTitle: draft.seoTitle.trim(),
            seoDescription: draft.seoDescription.trim(),
            staffDisplayPolicy: draft.staffDisplayPolicy,
            cancellationCutoffHours: cutoffValue
          }
        }
      );

      if (!response.response.ok) {
        toast.error(getSettingsErrorCopy(response.response.status));
        return;
      }

      toast.success("İşletme ayarları kaydedildi.");
      router.refresh();
    } catch {
      toast.error("Ayarlar şu anda kaydedilemedi. Lütfen tekrar dene.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>İşletme profili</CardTitle>
        <CardDescription>
          Public salon profilinde görünen metinler ve müşteri kuralları. Değişiklikler
          auditlenir; PII veya gizli bilgi yazma.
        </CardDescription>
      </CardHeader>

      <CardContent>
        {!canManage ? (
          <p className="mb-6 flex items-start gap-2 rounded-md border bg-muted/40 p-4 text-sm text-muted-foreground">
            <Info aria-hidden className="mt-0.5 size-4 shrink-0" />
            Bu ayarları yalnızca işletme sahibi değiştirebilir. Form salt okunur.
          </p>
        ) : null}

        <form className="space-y-6" onSubmit={handleSubmit}>
          <div className="space-y-2">
            <Label htmlFor="ayar-ad">İşletme adı</Label>
            <Input
              className="min-h-11"
              disabled={isDisabled}
              id="ayar-ad"
              maxLength={NAME_MAX}
              onChange={(event) => patch({ displayName: event.target.value })}
              value={draft.displayName}
            />
            <p
              className={cn(
                "text-sm",
                nameInvalid ? "text-destructive" : "text-muted-foreground"
              )}
            >
              {nameInvalid
                ? `En az ${NAME_MIN}, en fazla ${NAME_MAX} karakter.`
                : "Public profilde görünen işletme adı."}
            </p>
          </div>

          {/* PERSONEL GORUNURLUGU -- switch + GORUNUR aciklama (renk/ikon tek sinyal degil) */}
          <div className="flex items-start justify-between gap-4 rounded-md border p-4">
            <div className="space-y-1">
              <Label htmlFor="ayar-personel">Personel isimleri</Label>
              <p className="text-sm text-muted-foreground">
                Personel isimleri public profilde görünsün mü? Kapatırsan müşteri, randevu
                sırasında personel adını görmez. Kapasite hesabını etkilemez.
              </p>
              <p className="text-sm font-medium">
                {showNames ? "İsimler gösteriliyor" : "İsimler gizli"}
              </p>
            </div>
            <Switch
              aria-label="Personel isimlerini public profilde göster"
              checked={showNames}
              disabled={isDisabled}
              id="ayar-personel"
              onCheckedChange={(checked) =>
                patch({ staffDisplayPolicy: checked ? "ShowNames" : "HideNames" })
              }
            />
          </div>

          <CountedTextarea
            disabled={isDisabled}
            hint="Salonu tanıtan kısa metin."
            id="ayar-aciklama"
            label="Public açıklama"
            max={DESCRIPTION_MAX}
            onChange={(value) => patch({ description: value })}
            rows={4}
            value={draft.description}
          />

          <CountedTextarea
            disabled={isDisabled}
            hint="Randevu öncesi müşteri beklentisini netleştiren kısa kurallar."
            id="ayar-kurallar"
            label="Public kurallar"
            max={RULES_MAX}
            onChange={(value) => patch({ publicRules: value })}
            rows={4}
            value={draft.publicRules}
          />

          {/* IPTAL POLITIKASI (Serit B) -- nullable ama degeri elimizde, hep gonderiyoruz */}
          <div className="space-y-2">
            <Label htmlFor="ayar-iptal">İptal süresi (saat)</Label>
            <Input
              className="min-h-11 sm:max-w-40"
              disabled={isDisabled}
              id="ayar-iptal"
              inputMode="numeric"
              max={CANCELLATION_MAX}
              min={CANCELLATION_MIN}
              onChange={(event) => patch({ cancellationCutoffHours: event.target.value })}
              type="number"
              value={draft.cancellationCutoffHours}
            />
            <p
              className={cn(
                "text-sm",
                cutoffInvalid ? "text-destructive" : "text-muted-foreground"
              )}
            >
              {cutoffInvalid
                ? `0 ile ${CANCELLATION_MAX} saat (7 gün) arasında bir tam sayı gir.`
                : "Randevu saatine kaç saat kala müşteri iptal edebilir. 0 = her zaman iptal edebilir."}
            </p>
          </div>

          <div className="grid gap-6 lg:grid-cols-2">
            <CountedInput
              disabled={isDisabled}
              hint="Arama sonuçlarında görünen başlık."
              id="ayar-seo-baslik"
              label="SEO başlık"
              max={SEO_TITLE_MAX}
              onChange={(value) => patch({ seoTitle: value })}
              value={draft.seoTitle}
            />
            <CountedInput
              disabled={isDisabled}
              hint="Arama sonuçlarında görünen kısa açıklama."
              id="ayar-seo-aciklama"
              label="SEO açıklama"
              max={SEO_DESCRIPTION_MAX}
              onChange={(value) => patch({ seoDescription: value })}
              value={draft.seoDescription}
            />
          </div>

          <div className="flex justify-end">
            <Button className="min-h-11" disabled={isDisabled} type="submit">
              {isSubmitting ? "Kaydediliyor…" : "Ayarları kaydet"}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}

/* ---------------------------------------------------------------------------
   KARAKTER SAYACLI ALANLAR -- sayac GORUNUR etikette
   --------------------------------------------------------------------------- */

function CountedTextarea({
  disabled,
  hint,
  id,
  label,
  max,
  onChange,
  rows,
  value
}: {
  disabled: boolean;
  hint: string;
  id: string;
  label: string;
  max: number;
  onChange: (value: string) => void;
  rows: number;
  value: string;
}) {
  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between gap-2">
        <Label htmlFor={id}>{label}</Label>
        <span className="text-xs tabular-nums text-muted-foreground">
          {value.length} / {max}
        </span>
      </div>
      <Textarea
        disabled={disabled}
        id={id}
        maxLength={max}
        onChange={(event) => onChange(event.target.value)}
        rows={rows}
        value={value}
      />
      <p className="text-sm text-muted-foreground">{hint}</p>
    </div>
  );
}

function CountedInput({
  disabled,
  hint,
  id,
  label,
  max,
  onChange,
  value
}: {
  disabled: boolean;
  hint: string;
  id: string;
  label: string;
  max: number;
  onChange: (value: string) => void;
  value: string;
}) {
  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between gap-2">
        <Label htmlFor={id}>{label}</Label>
        <span className="text-xs tabular-nums text-muted-foreground">
          {value.length} / {max}
        </span>
      </div>
      <Input
        className="min-h-11"
        disabled={disabled}
        id={id}
        maxLength={max}
        onChange={(event) => onChange(event.target.value)}
        value={value}
      />
      <p className="text-sm text-muted-foreground">{hint}</p>
    </div>
  );
}

function getSettingsErrorCopy(status: number) {
  if (status === 400) {
    return "Alan formatı geçerli değil. Uzunlukları ve iptal süresini kontrol et.";
  }
  if (status === 403) {
    return "Bu ayar için işletme sahibi yetkisi gerekiyor.";
  }
  if (status === 404) {
    return "Aktif işletme profili bulunamadı.";
  }
  if (status === 409) {
    return "Bu tenant için birden fazla aktif işletme var; ayar ekranı tek işletme destekler.";
  }
  if (status === 429) {
    return "Çok sık ayar kaydı denendi. Kısa süre sonra tekrar dene.";
  }
  return "Ayarlar kaydedilemedi. Lütfen tekrar dene.";
}
