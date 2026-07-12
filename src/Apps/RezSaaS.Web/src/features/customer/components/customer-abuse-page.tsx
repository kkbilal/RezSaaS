"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import type {
  CustomerAbuseAppeal,
  CustomerAbuseOverview,
  CustomerClosureCase,
  CustomerSanction,
  CustomerStrike
} from "@/features/customer/api/get-abuse-overview";
import { apiClient } from "@/shared/api/client";
import { Button } from "@/shared/ui/button";
import { Card, CardDescription, CardTitle } from "@/shared/ui/card";
import { StatusBadge } from "@/shared/ui/status-badge";

type AppealTargetType = "UserStrike" | "UserSanction" | "AccountClosureCase";

type AppealTarget = {
  description: string;
  existingAppeal?: CustomerAbuseAppeal;
  targetId: string;
  targetType: AppealTargetType;
  title: string;
};

type AppealDraft = {
  statement: string;
  target: AppealTarget;
};

type CustomerAbusePageProps = {
  overview: CustomerAbuseOverview;
};

export function CustomerAbusePage({
  overview
}: CustomerAbusePageProps) {
  const router = useRouter();
  const [appeals, setAppeals] = useState<CustomerAbuseAppeal[]>(
    overview.appeals ?? []
  );
  const [draft, setDraft] = useState<AppealDraft | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [toast, setToast] = useState<string | null>(null);

  const sanctions = useMemo(() => overview.sanctions ?? [], [overview.sanctions]);
  const strikes = useMemo(() => overview.strikes ?? [], [overview.strikes]);
  const closureCases = useMemo(
    () => overview.closureCases ?? [],
    [overview.closureCases]
  );
  const targets = useMemo(
    () => buildAppealTargets(strikes, sanctions, closureCases, appeals),
    [appeals, closureCases, sanctions, strikes]
  );
  const activeSanctionCount = sanctions.filter((sanction) => sanction.isActive).length;
  const activeStrikeCount = strikes.filter(isActiveStrike).length;
  const pendingAppealCount = appeals.filter(
    (appeal) => appeal.status === "PendingReview"
  ).length;

  function openAppeal(target: AppealTarget) {
    if (target.existingAppeal) {
      showToast("Bu kayıt için itiraz kaydı zaten var.");
      return;
    }

    setDraft({
      statement: "",
      target
    });
  }

  async function submitAppeal() {
    if (!draft) {
      return;
    }

    const statement = draft.statement.trim();

    if (statement.length === 0) {
      showToast("İtiraz açıklaması boş olamaz.");
      return;
    }

    setIsSubmitting(true);

    try {
      const result = await apiClient.POST("/api/customer/abuse/appeals", {
        body: {
          statement,
          targetId: draft.target.targetId,
          targetType: draft.target.targetType
        }
      });

      if (!result.response.ok || !result.data) {
        showToast(getAppealErrorCopy(result.response.status));
        return;
      }

      setAppeals((current) => mergeAppeal(current, result.data));
      setDraft(null);
      showToast("İtiraz kaydı oluşturuldu ve incelemeye alındı.");
      router.refresh();
    } catch {
      showToast("İtiraz şu anda gönderilemedi. Lütfen tekrar dene.");
    } finally {
      setIsSubmitting(false);
    }
  }

  function showToast(message: string) {
    setToast(message);
    window.setTimeout(() => setToast(null), 3200);
  }

  return (
    <div className="space-y-6">
      <div className="mx-auto max-w-7xl space-y-8">
        <section className="fade-up rounded-[2.5rem] border border-[var(--rs-border)] bg-[var(--rs-glass)] p-6 shadow-[var(--rs-shadow-card)] backdrop-blur-xl sm:p-8">
          <div className="flex flex-col gap-8 lg:flex-row lg:items-end lg:justify-between">
            <div className="max-w-4xl space-y-5">
              <h1 className="text-5xl font-semibold tracking-[-0.07em] text-[var(--rs-ink)] sm:text-7xl">
                İtirazlarım ve güvenlik durumum.
              </h1>
              <p className="max-w-2xl text-lg leading-8 text-[var(--rs-muted-strong)]">
                Yalnızca kendi uyarı, aktif yaptırım veya hesap kapatma
                kayıtlarına itiraz açabilirsin. İç inceleme nedeni, admin kimliği
                veya gizli abuse detayı bu müşteri yüzeyinde gösterilmez.
              </p>
            </div>

            <div className="grid min-w-72 grid-cols-3 gap-3">
              <MetricCard label="Aktif yaptırım" value={activeSanctionCount} />
              <MetricCard label="Aktif uyarı" value={activeStrikeCount} />
              <MetricCard label="Açık itiraz" value={pendingAppealCount} />
            </div>
          </div>
        </section>

        <div className="grid gap-6 xl:grid-cols-[1fr_24rem]">
          <section className="space-y-6">
            <Card className="p-5">
              <CardTitle>İtiraz açılabilir kayıtlar</CardTitle>
              <CardDescription>
                Uygunluk nihai olarak backend tarafından kontrol edilir; süre
                dolmuş veya uygun olmayan hedefler güvenli hata döndürür.
              </CardDescription>
            </Card>

            {targets.length === 0 ? (
              <Card className="border-dashed bg-[var(--rs-glass)] p-10 text-center shadow-none">
                <CardTitle>Şu anda itiraz açılabilir kayıt yok</CardTitle>
                <CardDescription className="mx-auto mt-2 max-w-lg">
                  Aktif yaptırım, geçerli uyarı veya uygun hesap kapatma vakası
                  oluşursa burada görünür.
                </CardDescription>
              </Card>
            ) : (
              <div className="grid gap-4">
                {targets.map((target, index) => (
                  <AppealTargetCard
                    index={index}
                    key={`${target.targetType}-${target.targetId}`}
                    onAppeal={() => openAppeal(target)}
                    target={target}
                  />
                ))}
              </div>
            )}
          </section>

          <aside className="space-y-6">
            <SafeScopeCard />
            <AppealHistoryCard appeals={appeals} />
            <ClosureCasesCard closureCases={closureCases} />
          </aside>
        </div>
      </div>

      {draft ? (
        <AppealDialog
          draft={draft}
          isSubmitting={isSubmitting}
          onCancel={() => setDraft(null)}
          onStatementChange={(statement) =>
            setDraft((current) =>
              current
                ? {
                    ...current,
                    statement
                  }
                : current
            )
          }
          onSubmit={() => void submitAppeal()}
        />
      ) : null}

      {toast ? (
        <div className="fixed bottom-5 left-1/2 z-50 w-[calc(100%-2rem)] max-w-xl -translate-x-1/2 rounded-full border border-[var(--rs-border)] bg-[var(--rs-surface)] px-5 py-3 text-sm text-[var(--rs-ink)] shadow-[var(--rs-shadow-card)]">
          {toast}
        </div>
      ) : null}
    </div>
  );
}

function MetricCard({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-[1.5rem] bg-[var(--rs-accent)] p-4 text-white shadow-[var(--rs-shadow-card)]">
      <p className="text-[0.65rem] uppercase tracking-[0.18em] text-white/50">
        {label}
      </p>
      <p className="mt-5 text-4xl font-semibold tracking-[-0.07em]">{value}</p>
    </div>
  );
}

function AppealTargetCard({
  index,
  onAppeal,
  target
}: {
  index: number;
  onAppeal: () => void;
  target: AppealTarget;
}) {
  return (
    <article
      className="fade-up rounded-[2rem] border border-[var(--rs-border)] bg-[var(--rs-glass)] p-5 shadow-[var(--rs-shadow-soft)] backdrop-blur-xl"
      style={{ animationDelay: `${index * 45}ms` }}
    >
      <div className="grid gap-5 lg:grid-cols-[1fr_auto] lg:items-start">
        <div className="space-y-3">
          <div className="flex flex-wrap items-center gap-3">
            <span className="rounded-full bg-[var(--rs-neutral-soft)] px-3 py-1 text-xs font-medium text-[var(--rs-muted)]">
              {getTargetTypeCopy(target.targetType)}
            </span>
            {target.existingAppeal ? (
              <StatusBadge status={target.existingAppeal.status ?? "PendingReview"} />
            ) : null}
          </div>
          <h2 className="text-2xl font-semibold tracking-[-0.05em] text-[var(--rs-ink)]">
            {target.title}
          </h2>
          <p className="text-sm leading-6 text-[var(--rs-muted)]">
            {target.description}
          </p>
          <p className="font-mono text-xs text-[var(--rs-muted)]">
            Kayıt: {shortGuid(target.targetId)}
          </p>
        </div>

        <Button
          disabled={Boolean(target.existingAppeal)}
          onClick={onAppeal}
          type="button"
          variant={target.existingAppeal ? "secondary" : "primary"}
        >
          {target.existingAppeal ? "İtiraz kaydı var" : "İtiraz aç"}
        </Button>
      </div>
    </article>
  );
}

function SafeScopeCard() {
  return (
    <Card className="p-6">
      <CardTitle>Güvenli kapsam</CardTitle>
      <CardDescription className="mt-2">
        Bu ekran yalnızca sana ait customer kayıtlarını gösterir. İşletme
        yetkilisi veya platform iç notu burada paylaşılmaz.
      </CardDescription>
      <div className="mt-5 space-y-3 text-sm leading-6 text-[var(--rs-muted)]">
        <p className="rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface)] p-4">
          Açık itiraz limiti ve itiraz penceresi backend tarafından korunur.
        </p>
        <p className="rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface)] p-4">
          Kabul edilen itiraz ilgili strike, yaptırım veya closure case kaydını
          güvenli workflow içinde düzeltir.
        </p>
      </div>
    </Card>
  );
}

function AppealHistoryCard({ appeals }: { appeals: CustomerAbuseAppeal[] }) {
  return (
    <Card className="p-6">
      <CardTitle>İtiraz geçmişi</CardTitle>
      <CardDescription className="mt-2">
        İnceleme sonucu varsa yalnızca güvenli durum bilgisi gösterilir.
      </CardDescription>

      <div className="mt-5 space-y-3">
        {appeals.length === 0 ? (
          <p className="rounded-2xl border border-dashed border-[var(--rs-border)] bg-[var(--rs-glass)] p-4 text-sm text-[var(--rs-muted)]">
            Henüz itiraz kaydı yok.
          </p>
        ) : (
          appeals.map((appeal) => (
            <div
              className="rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface)] p-4 text-sm"
              key={appeal.appealId ?? `${appeal.targetType}-${appeal.targetId}`}
            >
              <div className="flex flex-wrap items-center justify-between gap-3">
                <StatusBadge status={appeal.status ?? "PendingReview"} />
                <span className="font-mono text-xs text-[var(--rs-muted)]">
                  {shortGuid(appeal.appealId)}
                </span>
              </div>
              <p className="mt-3 font-medium text-[var(--rs-ink)]">
                {getTargetTypeCopy(appeal.targetType)}
              </p>
              <p className="mt-1 text-xs text-[var(--rs-muted)]">
                Oluşturma: {formatUtcDateTime(appeal.createdAtUtc)}
              </p>
              {appeal.reviewedAtUtc ? (
                <p className="mt-1 text-xs text-[var(--rs-muted)]">
                  İnceleme: {formatUtcDateTime(appeal.reviewedAtUtc)}
                </p>
              ) : null}
            </div>
          ))
        )}
      </div>
    </Card>
  );
}

function ClosureCasesCard({
  closureCases
}: {
  closureCases: CustomerClosureCase[];
}) {
  if (closureCases.length === 0) {
    return null;
  }

  return (
    <Card className="p-6">
      <CardTitle>Hesap kapatma bildirimleri</CardTitle>
      <CardDescription className="mt-2">
        Bu bölüm yalnızca müşteriye gösterilebilir güvenli notice metnini taşır.
      </CardDescription>
      <div className="mt-5 space-y-3">
        {closureCases.map((closureCase) => (
          <div
            className="rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface)] p-4 text-sm"
            key={closureCase.closureCaseId}
          >
            <StatusBadge status={closureCase.status ?? "Unknown"} />
            <p className="mt-3 leading-6 text-[var(--rs-muted-strong)]">
              {closureCase.customerNotice ?? "Müşteri notice metni bekleniyor."}
            </p>
            <p className="mt-2 text-xs text-[var(--rs-muted)]">
              Öneri: {formatUtcDateTime(closureCase.proposedAtUtc)}
            </p>
          </div>
        ))}
      </div>
    </Card>
  );
}

function AppealDialog({
  draft,
  isSubmitting,
  onCancel,
  onStatementChange,
  onSubmit
}: {
  draft: AppealDraft;
  isSubmitting: boolean;
  onCancel: () => void;
  onStatementChange: (statement: string) => void;
  onSubmit: () => void;
}) {
  return (
    <div className="fixed inset-0 z-40 grid place-items-center bg-black/70 p-4 backdrop-blur-sm">
      <section className="fade-up w-full max-w-2xl rounded-[2rem] border border-[var(--rs-border)] bg-[var(--rs-surface)] p-6 shadow-[var(--rs-shadow-card)]">
        <div className="space-y-4">
          <span className="rounded-full bg-[var(--rs-accent-soft)] px-3 py-1 text-xs font-medium text-[var(--rs-accent-strong)]">
            {getTargetTypeCopy(draft.target.targetType)}
          </span>
          <h2 className="text-3xl font-semibold tracking-[-0.06em] text-[var(--rs-ink)]">
            İtiraz açıklaması
          </h2>
          <p className="text-sm leading-7 text-[var(--rs-muted)]">
            {draft.target.title}. Kişisel iletişim bilgisi, şifre, ödeme bilgisi
            veya gereksiz hassas detay paylaşma.
          </p>
        </div>

        <label className="mt-6 block text-sm font-medium text-[var(--rs-ink)]">
          Açıklama
          <textarea
            className="mt-3 min-h-40 w-full resize-y rounded-[1.25rem] border border-[var(--rs-border)] bg-[var(--rs-surface)] px-4 py-3 text-sm leading-6 text-[var(--rs-ink)] shadow-[var(--rs-shadow-soft)] outline-none transition focus:border-[var(--rs-accent)] focus:ring-4 focus:ring-[rgba(99_102_241_/_0.18)]"
            maxLength={1000}
            onChange={(event) => onStatementChange(event.target.value)}
            placeholder="Bu kararın neden tekrar incelenmesi gerektiğini kısa ve net anlat."
            value={draft.statement}
          />
        </label>

        <div className="mt-6 flex flex-col gap-3 sm:flex-row sm:justify-end">
          <Button disabled={isSubmitting} onClick={onCancel} type="button" variant="secondary">
            Geri dön
          </Button>
          <Button disabled={isSubmitting} onClick={onSubmit} type="button">
            {isSubmitting ? "Gönderiliyor" : "İtirazı gönder"}
          </Button>
        </div>
      </section>
    </div>
  );
}

function buildAppealTargets(
  strikes: CustomerStrike[],
  sanctions: CustomerSanction[],
  closureCases: CustomerClosureCase[],
  appeals: CustomerAbuseAppeal[]
): AppealTarget[] {
  const strikeTargets = strikes.filter(isActiveStrike).map((strike) => ({
    description: `Geçerlilik: ${formatUtcDateTime(strike.issuedAtUtc)} - ${formatUtcDateTime(
      strike.expiresAtUtc
    )}`,
    existingAppeal: findExistingAppeal(appeals, "UserStrike", strike.strikeId),
    targetId: strike.strikeId ?? "",
    targetType: "UserStrike" as const,
    title: `Uyarı kodu: ${strike.reasonCode ?? "Belirtilmedi"}`
  }));

  const sanctionTargets = sanctions
    .filter((sanction) => sanction.isActive)
    .map((sanction) => ({
      description: `Başlangıç: ${formatUtcDateTime(
        sanction.startsAtUtc
      )}. Bitiş: ${formatUtcDateTime(sanction.endsAtUtc)}`,
      existingAppeal: findExistingAppeal(
        appeals,
        "UserSanction",
        sanction.sanctionId
      ),
      targetId: sanction.sanctionId ?? "",
      targetType: "UserSanction" as const,
      title: `Aktif yaptırım: ${sanction.type ?? "Belirtilmedi"}`
    }));

  const closureTargets = closureCases
    .filter((closureCase) =>
      ["PendingApproval", "Approved"].includes(closureCase.status ?? "")
    )
    .map((closureCase) => ({
      description:
        closureCase.customerNotice ??
        "Hesap kapatma vakası için müşteri bildirimi bekleniyor.",
      existingAppeal: findExistingAppeal(
        appeals,
        "AccountClosureCase",
        closureCase.closureCaseId
      ),
      targetId: closureCase.closureCaseId ?? "",
      targetType: "AccountClosureCase" as const,
      title: `Hesap kapatma vakası: ${closureCase.status ?? "Bilinmiyor"}`
    }));

  return [...sanctionTargets, ...strikeTargets, ...closureTargets].filter(
    (target) => target.targetId.length > 0
  );
}

function findExistingAppeal(
  appeals: CustomerAbuseAppeal[],
  targetType: string,
  targetId?: string | null
) {
  if (!targetId) {
    return undefined;
  }

  return appeals.find(
    (appeal) => appeal.targetType === targetType && appeal.targetId === targetId
  );
}

function mergeAppeal(
  currentAppeals: CustomerAbuseAppeal[],
  nextAppeal: CustomerAbuseAppeal
) {
  const nextAppealId = nextAppeal.appealId;
  const existingIndex = currentAppeals.findIndex(
    (appeal) =>
      (nextAppealId && appeal.appealId === nextAppealId) ||
      (appeal.targetType === nextAppeal.targetType &&
        appeal.targetId === nextAppeal.targetId)
  );

  if (existingIndex === -1) {
    return [nextAppeal, ...currentAppeals];
  }

  return currentAppeals.map((appeal, index) =>
    index === existingIndex ? nextAppeal : appeal
  );
}

function isActiveStrike(strike: CustomerStrike) {
  if (strike.revokedAtUtc || !strike.expiresAtUtc) {
    return false;
  }

  const expires = new Date(strike.expiresAtUtc).getTime();

  return !Number.isNaN(expires) && expires > Date.now();
}

function getTargetTypeCopy(targetType?: string | null) {
  if (targetType === "UserStrike") {
    return "Uyarı";
  }

  if (targetType === "UserSanction") {
    return "Yaptırım";
  }

  if (targetType === "AccountClosureCase") {
    return "Hesap kapatma";
  }

  return targetType ?? "Kayıt";
}

function formatUtcDateTime(value?: string | null) {
  if (!value) {
    return "Zaman yok";
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return "Zaman okunamıyor";
  }

  return `${new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: "UTC"
  }).format(date)} UTC`;
}

function shortGuid(value?: string | null) {
  if (!value) {
    return "Bilgi yok";
  }

  return `${value.slice(0, 8)}...`;
}

function getAppealErrorCopy(status: number) {
  if (status === 401) {
    return "Oturum doğrulanamadı. Lütfen yeniden giriş yap.";
  }

  if (status === 404) {
    return "İtiraz hedefi bulunamadı veya bu hesapla görüntülenemiyor.";
  }

  if (status === 409) {
    return "Bu kayıt için itiraz penceresi kapalı olabilir veya açık itiraz limitine ulaşıldı.";
  }

  if (status === 429) {
    return "Çok kısa sürede fazla itiraz denemesi yapıldı. Lütfen sonra tekrar dene.";
  }

  return "İtiraz gönderilemedi. Lütfen tekrar dene.";
}
