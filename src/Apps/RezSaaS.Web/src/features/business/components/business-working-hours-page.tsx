"use client";

import { Clock, Copy, TriangleAlert } from "lucide-react";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import {
  WEEK_DAYS,
  listWorkingHours,
  upsertWorkingHours,
  type DayKey,
  type WorkingHoursResult
} from "@/features/business/api/business-working-hours-client";
import type { BusinessBranch } from "@/features/business/api/get-business-branches-server";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue
} from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import { Switch } from "@/components/ui/switch";
import { cn } from "@/shared/lib/cn";

/**
 * CALISMA SAATLERI (Serit E) -- SUBE seviyesinde, personel bazli DEGIL (Tuzak 5).
 *
 * Model: her gun icin { acilis, kanapis, kapali }. Backend GET yaniti mevcut gunleri
 * doner; eksik gunleri "kapali" varsayilaniyla 7 gune tamamlariz. Kaydet, SADECE
 * DEGISEN gunleri gun gun PUT eder (backend ucu gun bazli: .../working-hours/{dayOfWeek}).
 *
 * "Pazartesiyi tum haftaya kopyala": Pazartesi'nin degerlerini yerel taslaga kopyalar
 * (henuz kaydetmez); kullanici sonra "Kaydet" ile 7 gunu birden yazar.
 */

const DEFAULT_OPENS = "09:00";
const DEFAULT_CLOSES = "18:00";

type DayState = {
  opensAt: string;
  closesAt: string;
  isClosed: boolean;
};

type WeekState = Record<DayKey, DayState>;

function defaultDay(): DayState {
  return { opensAt: DEFAULT_OPENS, closesAt: DEFAULT_CLOSES, isClosed: true };
}

function emptyWeek(): WeekState {
  return WEEK_DAYS.reduce((acc, day) => {
    acc[day.key] = defaultDay();
    return acc;
  }, {} as WeekState);
}

/** Backend "HH:mm:ss" de dondurebilir; time input "HH:mm" ister -> kirp. */
function normalizeTime(value: string | null | undefined, fallback: string): string {
  if (!value) {
    return fallback;
  }
  return value.slice(0, 5);
}

function sameDay(a: DayState, b: DayState): boolean {
  return (
    a.isClosed === b.isClosed &&
    a.opensAt === b.opensAt &&
    a.closesAt === b.closesAt
  );
}

/** Acik gunde saatler dolu ve kapanis > acilis olmali (backend de bunu kosar). */
function dayInvalid(day: DayState): boolean {
  if (day.isClosed) {
    return false;
  }
  if (!day.opensAt || !day.closesAt) {
    return true;
  }
  // "HH:mm" sifir dolgulu -> sozel siralama = kronolojik siralama.
  return day.closesAt <= day.opensAt;
}

export type BusinessWorkingHoursPageProps = {
  tenantId: string;
  branches: BusinessBranch[];
};

export function BusinessWorkingHoursPage({
  branches,
  tenantId
}: BusinessWorkingHoursPageProps) {
  const router = useRouter();

  // Tek sube OTOMATIK secilir (Tuzak 1). Coksa kullanici secer.
  const [selectedBranchId, setSelectedBranchId] = useState<string>(
    branches.length === 1 ? (branches[0]?.id ?? "") : ""
  );

  const [baseline, setBaseline] = useState<WeekState>(emptyWeek);
  const [draft, setDraft] = useState<WeekState>(emptyWeek);
  const [status, setStatus] = useState<"idle" | "loading" | "ready" | "error">(
    "idle"
  );
  const [saving, setSaving] = useState(false);

  const selectedBranch = branches.find((branch) => branch.id === selectedBranchId);

  const load = useCallback(
    async (branchId: string) => {
      setStatus("loading");
      const result = await listWorkingHours(tenantId, branchId);

      if (result.kind !== "success") {
        setStatus("error");
        toast.error(result.message);
        return;
      }

      const week = emptyWeek();
      for (const entry of result.data ?? []) {
        const key = entry.dayOfWeek as DayKey | null;
        if (key && key in week) {
          week[key] = {
            opensAt: normalizeTime(entry.opensAt, DEFAULT_OPENS),
            closesAt: normalizeTime(entry.closesAt, DEFAULT_CLOSES),
            // isClosed sozlesmede boolean ama openapi-typescript tum alanlari optional
            // uretir; gelen kayitta yoksa "acik" varsay.
            isClosed: entry.isClosed ?? false
          };
        }
      }

      setBaseline(week);
      setDraft(week);
      setStatus("ready");
    },
    [tenantId]
  );

  useEffect(() => {
    if (!selectedBranchId) {
      setStatus("idle");
      return;
    }
    void load(selectedBranchId);
  }, [selectedBranchId, load]);

  function patchDay(key: DayKey, patch: Partial<DayState>) {
    setDraft((current) => ({ ...current, [key]: { ...current[key], ...patch } }));
  }

  function copyMondayToAll() {
    const monday = draft.Monday;
    setDraft((current) => {
      const next = { ...current };
      for (const day of WEEK_DAYS) {
        next[day.key] = { ...monday };
      }
      return next;
    });
    // Yerel taslak degisti; kalici olmasi icin "Kaydet" gerekir. Kullaniciya soyle.
    toast.info("Pazartesi tüm günlere kopyalandı. Kaydetmeyi unutma.");
  }

  const dirtyKeys = WEEK_DAYS.filter(
    (day) => !sameDay(draft[day.key], baseline[day.key])
  ).map((day) => day.key);

  const hasInvalid = dirtyKeys.some((key) => dayInvalid(draft[key]));

  async function handleSave() {
    if (!selectedBranchId || dirtyKeys.length === 0) {
      return;
    }

    if (hasInvalid) {
      toast.error("Açık günlerde kapanış saati açılıştan sonra olmalı.");
      return;
    }

    setSaving(true);

    // Gun bazli PUT: her degisen gun icin ayri istek (backend ucu gun bazli).
    const results = await Promise.all(
      dirtyKeys.map(async (key) => {
        const day = draft[key];
        const result = await upsertWorkingHours(tenantId, selectedBranchId, key, {
          opensAt: day.opensAt,
          closesAt: day.closesAt,
          isClosed: day.isClosed
        });
        return { key, result } as {
          key: DayKey;
          result: WorkingHoursResult<unknown>;
        };
      })
    );

    setSaving(false);

    const failed = results.filter((entry) => entry.result.kind !== "success");
    const succeeded = results.filter((entry) => entry.result.kind === "success");

    // Basarili gunlerin baseline'ini guncelle (kismi basari da olsa tutarli kalsin).
    if (succeeded.length > 0) {
      setBaseline((current) => {
        const next = { ...current };
        for (const entry of succeeded) {
          next[entry.key] = { ...draft[entry.key] };
        }
        return next;
      });
    }

    if (failed.length === 0) {
      toast.success("Çalışma saatleri kaydedildi.");
      router.refresh();
      return;
    }

    // Ilk hatayi goster. "rejected" -> yerel durum bayat olabilir, sunucudan tazele.
    const firstFailure = failed[0]?.result;
    if (firstFailure && firstFailure.kind !== "success") {
      toast.error(firstFailure.message);
      if (firstFailure.kind === "rejected") {
        void load(selectedBranchId);
      }
    }
  }

  return (
    <div className="space-y-6">
      <header className="flex flex-col gap-2">
        <h1 className="text-3xl font-semibold tracking-tight">Çalışma saatleri</h1>
        <p className="max-w-2xl text-sm text-muted-foreground">
          Şubenin haftalık açık/kapalı saatlerini belirle. Randevu slotları bu saatlere
          göre hesaplanır. Saatler şube geneline uygulanır — personel bazlı değildir.
        </p>
      </header>

      {/* SUBE SECICI -- birden fazla sube varsa. Tek subede otomatik secili. */}
      {branches.length > 1 ? (
        <div className="flex flex-col gap-2 sm:max-w-sm">
          <Label htmlFor="sube-secici">Şube</Label>
          <Select onValueChange={setSelectedBranchId} value={selectedBranchId}>
            <SelectTrigger className="min-h-11" id="sube-secici">
              <SelectValue placeholder="Saatlerini görmek için bir şube seç" />
            </SelectTrigger>
            <SelectContent>
              {branches.map((branch) => (
                <SelectItem key={branch.id} value={branch.id ?? ""}>
                  {branch.displayName}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      ) : selectedBranch ? (
        <p className="text-sm text-muted-foreground">
          Şube:{" "}
          <span className="font-medium text-foreground">
            {selectedBranch.displayName}
          </span>
        </p>
      ) : null}

      {branches.length === 0 ? (
        <NoBranch />
      ) : !selectedBranchId ? (
        <Card>
          <CardContent className="py-14 text-center text-sm text-muted-foreground">
            Saatleri görmek için yukarıdan bir şube seç.
          </CardContent>
        </Card>
      ) : status === "loading" || status === "idle" ? (
        <div className="space-y-3">
          {WEEK_DAYS.map((day) => (
            <Skeleton className="h-20 w-full rounded-xl" key={day.key} />
          ))}
        </div>
      ) : status === "error" ? (
        <Card>
          <CardContent className="flex flex-col items-center gap-2 py-14 text-center">
            <TriangleAlert aria-hidden className="size-6 text-muted-foreground" />
            <p className="text-sm text-muted-foreground">
              Çalışma saatleri yüklenemedi. Sayfayı yenileyip tekrar dene.
            </p>
          </CardContent>
        </Card>
      ) : (
        <>
          <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
            <Button
              className="min-h-11 sm:w-auto"
              onClick={copyMondayToAll}
              type="button"
              variant="outline"
            >
              <Copy aria-hidden className="size-4" />
              Pazartesiyi tüm haftaya kopyala
            </Button>
            {dirtyKeys.length > 0 ? (
              <p className="text-sm text-muted-foreground">
                {dirtyKeys.length} günde kaydedilmemiş değişiklik var.
              </p>
            ) : null}
          </div>

          <Card className="overflow-hidden">
            <CardContent className="divide-y p-0">
              {WEEK_DAYS.map((day) => (
                <DayRow
                  day={draft[day.key]}
                  invalid={dayInvalid(draft[day.key])}
                  key={day.key}
                  label={day.label}
                  onChange={(patch) => patchDay(day.key, patch)}
                />
              ))}
            </CardContent>
          </Card>

          <div className="flex justify-end">
            <Button
              className="min-h-11"
              disabled={saving || dirtyKeys.length === 0 || hasInvalid}
              onClick={handleSave}
            >
              {saving ? "Kaydediliyor…" : "Değişiklikleri kaydet"}
            </Button>
          </div>
        </>
      )}
    </div>
  );
}

/* ---------------------------------------------------------------------------
   GUN SATIRI -- responsive: mobilde dikey, sm+ yatay
   --------------------------------------------------------------------------- */

function DayRow({
  day,
  invalid,
  label,
  onChange
}: {
  day: DayState;
  invalid: boolean;
  label: string;
  onChange: (patch: Partial<DayState>) => void;
}) {
  const switchId = `acik-${label}`;

  return (
    <div className="flex flex-col gap-4 p-4 sm:flex-row sm:items-center">
      <div className="flex items-center justify-between gap-3 sm:w-48 sm:shrink-0">
        <span className="font-medium">{label}</span>
        <label className="flex min-h-11 items-center gap-2" htmlFor={switchId}>
          <Switch
            checked={!day.isClosed}
            id={switchId}
            onCheckedChange={(open) => onChange({ isClosed: !open })}
          />
          {/* Renk TEK sinyal degil -- durumu METIN de tasir. */}
          <span
            className={cn(
              "text-sm",
              day.isClosed ? "text-muted-foreground" : "text-foreground"
            )}
          >
            {day.isClosed ? "Kapalı" : "Açık"}
          </span>
        </label>
      </div>

      {day.isClosed ? (
        <p className="text-sm text-muted-foreground sm:flex-1">
          Bu gün kapalı — randevu alınmaz.
        </p>
      ) : (
        <div className="flex flex-1 flex-col gap-3 sm:flex-row sm:items-end">
          <div className="flex flex-1 items-end gap-3">
            <div className="flex-1 space-y-1.5">
              <Label htmlFor={`acilis-${label}`}>Açılış</Label>
              <Input
                aria-invalid={invalid}
                className="min-h-11"
                id={`acilis-${label}`}
                onChange={(event) => onChange({ opensAt: event.target.value })}
                type="time"
                value={day.opensAt}
              />
            </div>
            <div className="flex-1 space-y-1.5">
              <Label htmlFor={`kapanis-${label}`}>Kapanış</Label>
              <Input
                aria-invalid={invalid}
                className="min-h-11"
                id={`kapanis-${label}`}
                onChange={(event) => onChange({ closesAt: event.target.value })}
                type="time"
                value={day.closesAt}
              />
            </div>
          </div>
          {invalid ? (
            <p className="text-sm text-destructive sm:pb-2.5">
              Kapanış açılıştan sonra olmalı.
            </p>
          ) : null}
        </div>
      )}
    </div>
  );
}

/* ---------------------------------------------------------------------------
   SUBE YOK DURUMU
   --------------------------------------------------------------------------- */

function NoBranch() {
  return (
    <Card>
      <CardContent className="flex flex-col items-center gap-4 py-14 text-center">
        <div className="flex size-14 items-center justify-center rounded-full bg-muted">
          <Clock aria-hidden className="size-6 text-muted-foreground" />
        </div>
        <div className="space-y-1">
          <h2 className="text-lg font-semibold">Önce bir şube ekle</h2>
          <p className="mx-auto max-w-sm text-sm text-muted-foreground">
            Çalışma saatleri her zaman bir şubeye bağlıdır. Saat girmeden önce en az
            bir şube tanımlaman gerekiyor.
          </p>
        </div>
      </CardContent>
    </Card>
  );
}
