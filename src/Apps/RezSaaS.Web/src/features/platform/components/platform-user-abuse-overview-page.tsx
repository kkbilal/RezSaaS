"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import type {
  PlatformUserAbuseOverview,
  PlatformUserSanction,
  PlatformUserStrike
} from "@/features/platform/api/get-platform-user-abuse-overview";
import { PlatformSanctionApplyDialog } from "@/features/platform/components/platform-sanction-apply-dialog";
import { routes } from "@/shared/config/routes";
import { apiClient } from "@/shared/api/client";
import { Button } from "@/shared/ui/button";
import {
  DialogFormPanel,
  DialogOverlay
} from "@/shared/ui/dialog";
import { Card, CardDescription, CardHeader, CardTitle } from "@/shared/ui/card";
import { StatusBadge } from "@/shared/ui/status-badge";

type PlatformUserAbuseOverviewPageProps = {
  overview: PlatformUserAbuseOverview;
  sessionEmail: string;
  stepUpExpiresAtUtc?: string | null;
};

export function PlatformUserAbuseOverviewPage({
  overview,
  sessionEmail,
  stepUpExpiresAtUtc
}: PlatformUserAbuseOverviewPageProps) {
  const [showSanctionApply, setShowSanctionApply] = useState(false);
  const [revokeTarget, setRevokeTarget] = useState<{
    kind: "strike";
    strike: PlatformUserStrike;
  } | {
    kind: "sanction";
    sanction: PlatformUserSanction;
  } | null>(null);

  const activeStrikes = overview.strikes.filter((s) => s.isActive);
  const activeSanctions = overview.sanctions.filter((s) => s.isActive);

  return (
    <main className="studio-grid min-h-screen px-4 py-6 sm:px-8">
      <div className="mx-auto max-w-5xl space-y-8">
        <header className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <Link
            className="text-lg font-semibold tracking-[-0.04em] text-[var(--rs-ink)]"
            href={routes.platform.abuse}
          >
            ← Abuse
          </Link>
          <div className="flex flex-wrap items-center gap-3">
            <span className="rounded-full border border-[var(--rs-border)] bg-white px-4 py-2 text-sm text-[var(--rs-muted)]">
              {sessionEmail}
            </span>
            {stepUpExpiresAtUtc ? (
              <span className="rounded-full border border-[var(--rs-border)] bg-white px-4 py-2 text-sm text-[var(--rs-muted)]">
                Step-up: {formatUtcDateTime(stepUpExpiresAtUtc)}
              </span>
            ) : null}
          </div>
        </header>

        <section className="fade-up rounded-[2.5rem] border border-[var(--rs-border)] bg-white/76 p-6 shadow-[var(--rs-shadow-card)] backdrop-blur-xl sm:p-8">
          <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
            <div>
              <p className="w-fit rounded-full bg-[var(--rs-accent-soft)] px-4 py-2 text-sm font-medium text-[var(--rs-accent-strong)]">
                Kullanıcı incelemesi
              </p>
              <h1 className="mt-4 text-4xl font-semibold tracking-[-0.06em] text-[var(--rs-ink)]">
                {shortGuid(overview.userAccountId)}
              </h1>
              {overview.risk ? (
                <div className="mt-4 flex flex-wrap items-center gap-3">
                  <StatusBadge
                    status={overview.risk.level ?? "Unknown"}
                  />
                  <span className="text-sm text-[var(--rs-muted)]">
                    {overview.risk.activeStrikeCount ?? 0} aktif strike
                  </span>
                </div>
              ) : null}
            </div>
            <Button onClick={() => setShowSanctionApply(true)} variant="danger">
              Yaptırım uygula
            </Button>
          </div>
        </section>

        <section className="grid gap-6 lg:grid-cols-2">
          <Card className="p-5">
            <CardHeader>
              <CardTitle>Strike kayıtları</CardTitle>
              <CardDescription>
                {activeStrikes.length > 0
                  ? `${activeStrikes.length} aktif strike`
                  : "Aktif strike yok"}
              </CardDescription>
            </CardHeader>
            <div className="mt-5 space-y-3">
              {overview.strikes.length === 0 ? (
                <p className="rounded-2xl border border-dashed border-[var(--rs-border)] bg-white/60 p-4 text-sm text-[var(--rs-muted)]">
                  Strike kaydı yok.
                </p>
              ) : (
                overview.strikes.map((strike) => (
                  <StrikeCard
                    key={strike.strikeId}
                    onRevoke={() =>
                      setRevokeTarget({ kind: "strike", strike })
                    }
                    strike={strike}
                  />
                ))
              )}
            </div>
          </Card>

          <Card className="p-5">
            <CardHeader>
              <CardTitle>Yaptırım kayıtları</CardTitle>
              <CardDescription>
                {activeSanctions.length > 0
                  ? `${activeSanctions.length} aktif yaptırım`
                  : "Aktif yaptırım yok"}
              </CardDescription>
            </CardHeader>
            <div className="mt-5 space-y-3">
              {overview.sanctions.length === 0 ? (
                <p className="rounded-2xl border border-dashed border-[var(--rs-border)] bg-white/60 p-4 text-sm text-[var(--rs-muted)]">
                  Yaptırım kaydı yok.
                </p>
              ) : (
                overview.sanctions.map((sanction) => (
                  <SanctionCard
                    key={sanction.sanctionId}
                    onRevoke={() =>
                      setRevokeTarget({
                        kind: "sanction",
                        sanction
                      })
                    }
                    sanction={sanction}
                  />
                ))
              )}
            </div>
          </Card>
        </section>

        <Card className="p-5">
          <CardHeader>
            <CardTitle>Abuse raporları</CardTitle>
            <CardDescription>
              {overview.reports.length} kayıt
            </CardDescription>
          </CardHeader>
          <div className="mt-5 space-y-3">
            {overview.reports.length === 0 ? (
              <p className="rounded-2xl border border-dashed border-[var(--rs-border)] bg-white/60 p-4 text-sm text-[var(--rs-muted)]">
                Rapor kaydı yok.
              </p>
            ) : (
              overview.reports.map((report) => (
                <div
                  className="rounded-2xl border border-[var(--rs-border)] bg-white p-4 text-sm"
                  key={report.reportId}
                >
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <StatusBadge
                      status={report.status ?? "PendingReview"}
                    />
                    <span className="font-mono text-xs text-[var(--rs-muted)]">
                      {shortGuid(report.reportId)}
                    </span>
                  </div>
                  <p className="mt-3 font-medium text-[var(--rs-ink)]">
                    {report.reasonCode ?? "Bilgi yok"}
                  </p>
                  <p className="mt-2 leading-6 text-[var(--rs-muted)]">
                    {report.note || "Not girilmemiş."}
                  </p>
                </div>
              ))
            )}
          </div>
        </Card>

        <Card className="p-5">
          <CardHeader>
            <CardTitle>Abuse event geçmişi</CardTitle>
          </CardHeader>
          <div className="mt-5 space-y-3">
            {overview.events.length === 0 ? (
              <p className="rounded-2xl border border-dashed border-[var(--rs-border)] bg-white/60 p-4 text-sm text-[var(--rs-muted)]">
                Event kaydı yok.
              </p>
            ) : (
              overview.events.slice(0, 10).map((event) => (
                <div
                  className="rounded-2xl border border-[var(--rs-border)] bg-white p-4 text-sm"
                  key={event.eventId}
                >
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <StatusBadge
                      status={event.severity ?? "Unknown"}
                    />
                    <span className="font-mono text-xs text-[var(--rs-muted)]">
                      {shortGuid(event.eventId)}
                    </span>
                  </div>
                  <p className="mt-3 font-medium text-[var(--rs-ink)]">
                    {event.eventType ?? "Event"}
                  </p>
                  <p className="mt-1 text-xs text-[var(--rs-muted)]">
                    {formatUtcDateTime(event.occurredAtUtc)}
                  </p>
                </div>
              ))
            )}
          </div>
        </Card>
      </div>

      {showSanctionApply ? (
        <PlatformSanctionApplyDialog
          onDismiss={() => setShowSanctionApply(false)}
          userAccountId={overview.userAccountId}
        />
      ) : null}

      {revokeTarget?.kind === "strike" ? (
        <PlatformStrikeRevokeDialog
          onDismiss={() => setRevokeTarget(null)}
          strike={revokeTarget.strike}
          userAccountId={overview.userAccountId}
        />
      ) : null}

      {revokeTarget?.kind === "sanction" ? (
        <PlatformSanctionRevokeDialog
          onDismiss={() => setRevokeTarget(null)}
          sanction={revokeTarget.sanction}
          userAccountId={overview.userAccountId}
        />
      ) : null}
    </main>
  );
}

function StrikeCard({
  onRevoke,
  strike
}: {
  onRevoke: () => void;
  strike: PlatformUserStrike;
}) {
  return (
    <div className="rounded-2xl border border-[var(--rs-border)] bg-white p-4 text-sm">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <StatusBadge
          status={strike.isActive ? "Active" : "Revoked"}
        />
        <span className="font-mono text-xs text-[var(--rs-muted)]">
          {shortGuid(strike.strikeId)}
        </span>
      </div>
      <p className="mt-3 font-medium text-[var(--rs-ink)]">
        {strike.reasonCode ?? "Bilgi yok"}
      </p>
      <div className="mt-2 grid gap-1 text-xs text-[var(--rs-muted)]">
        <span>Oluşturma: {formatUtcDateTime(strike.issuedAtUtc)}</span>
        <span>Bitiş: {formatUtcDateTime(strike.expiresAtUtc)}</span>
        {strike.revokedAtUtc ? (
          <span>
            Geri alınma: {formatUtcDateTime(strike.revokedAtUtc)} ·{" "}
            {strike.revocationReason}
          </span>
        ) : null}
      </div>
      {strike.isActive ? (
        <Button
          className="mt-3"
          onClick={onRevoke}
          variant="ghost"
        >
          Geri al
        </Button>
      ) : null}
    </div>
  );
}

function SanctionCard({
  onRevoke,
  sanction
}: {
  onRevoke: () => void;
  sanction: PlatformUserSanction;
}) {
  return (
    <div className="rounded-2xl border border-[var(--rs-border)] bg-white p-4 text-sm">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <StatusBadge
          status={sanction.type ?? "Unknown"}
        />
        <StatusBadge
          status={sanction.isActive ? "Active" : "Revoked"}
        />
      </div>
      <p className="mt-3 font-medium text-[var(--rs-ink)]">
        {sanction.reason ?? "Bilgi yok"}
      </p>
      <div className="mt-2 grid gap-1 text-xs text-[var(--rs-muted)]">
        <span>Başlangıç: {formatUtcDateTime(sanction.startsAtUtc)}</span>
        {sanction.endsAtUtc ? (
          <span>Bitiş: {formatUtcDateTime(sanction.endsAtUtc)}</span>
        ) : null}
        {sanction.revokedAtUtc ? (
          <span>
            Geri alınma: {formatUtcDateTime(sanction.revokedAtUtc)} ·{" "}
            {sanction.revocationReason}
          </span>
        ) : null}
      </div>
      {sanction.isActive ? (
        <Button
          className="mt-3"
          onClick={onRevoke}
          variant="ghost"
        >
          Geri al
        </Button>
      ) : null}
    </div>
  );
}

function PlatformStrikeRevokeDialog({
  onDismiss,
  strike,
  userAccountId
}: {
  onDismiss: () => void;
  strike: PlatformUserStrike;
  userAccountId: string;
}) {
  const router = useRouter();
  const [reason, setReason] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [toast, setToast] = useState<string | null>(null);

  function showToast(message: string) {
    setToast(message);
    window.setTimeout(() => setToast(null), 3600);
  }

  async function handleRevoke(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!reason.trim()) {
      showToast("Geri alma nedeni zorunludur.");
      return;
    }

    setIsSubmitting(true);

    try {
      const result = await apiClient.POST(
        "/api/admin/abuse/users/{userAccountId}/strikes/{strikeId}/revoke",
        {
          body: { reason: reason.trim() },
          params: {
            path: {
              userAccountId,
              strikeId: strike.strikeId
            }
          }
        }
      );

      if (!result.response.ok) {
        showToast(getRevokeErrorCopy(result.response.status));
        return;
      }

      onDismiss();
      showToast("Strike geri alındı.");
      router.refresh();
    } catch {
      showToast("Strike geri alma şu anda uygulanamadı.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <DialogOverlay onEscapeKeyDown={onDismiss}>
      <DialogFormPanel onSubmit={(event) => void handleRevoke(event)}>
        <div className="space-y-4">
          <h2 className="text-3xl font-semibold tracking-[-0.06em] text-[var(--rs-ink)]">
            Strike geri al
          </h2>
          <p className="text-sm leading-7 text-[var(--rs-muted)]">
            Strike: {shortGuid(strike.strikeId)} · {strike.reasonCode}
          </p>
        </div>
        <div className="mt-6 grid gap-4">
          <label className="grid gap-2 text-sm font-medium text-[var(--rs-ink)]">
            Geri alma nedeni
            <textarea
              className="min-h-32 rounded-2xl border border-[var(--rs-border)] bg-white px-4 py-3 text-sm leading-6 text-[var(--rs-ink)] outline-none transition focus:border-[var(--rs-border-strong)] focus:ring-4 focus:ring-[rgb(5_26_36_/_0.08)]"
              maxLength={300}
              onChange={(event) => setReason(event.target.value)}
              placeholder="Geri alma gerekçesini yaz."
              required
              value={reason}
            />
          </label>
        </div>
        <div className="mt-6 flex flex-col gap-3 sm:flex-row sm:justify-end">
          <Button disabled={isSubmitting} onClick={onDismiss} type="button" variant="secondary">
            Vazgeç
          </Button>
          <Button disabled={isSubmitting || !reason.trim()} type="submit" variant="secondary">
            {isSubmitting ? "Geri alınıyor" : "Strike geri al"}
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

function PlatformSanctionRevokeDialog({
  onDismiss,
  sanction,
  userAccountId
}: {
  onDismiss: () => void;
  sanction: PlatformUserSanction;
  userAccountId: string;
}) {
  const router = useRouter();
  const [reason, setReason] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [toast, setToast] = useState<string | null>(null);

  function showToast(message: string) {
    setToast(message);
    window.setTimeout(() => setToast(null), 3600);
  }

  async function handleRevoke(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!reason.trim()) {
      showToast("Geri alma nedeni zorunludur.");
      return;
    }

    setIsSubmitting(true);

    try {
      const result = await apiClient.POST(
        "/api/admin/abuse/users/{userAccountId}/sanctions/{sanctionId}/revoke",
        {
          body: { reason: reason.trim() },
          params: {
            path: {
              sanctionId: sanction.sanctionId,
              userAccountId
            }
          }
        }
      );

      if (!result.response.ok) {
        showToast(getRevokeErrorCopy(result.response.status));
        return;
      }

      onDismiss();
      showToast("Yaptırım geri alındı.");
      router.refresh();
    } catch {
      showToast("Yaptırım geri alma şu anda uygulanamadı.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <DialogOverlay onEscapeKeyDown={onDismiss}>
      <DialogFormPanel onSubmit={(event) => void handleRevoke(event)}>
        <div className="space-y-4">
          <h2 className="text-3xl font-semibold tracking-[-0.06em] text-[var(--rs-ink)]">
            Yaptırım geri al
          </h2>
          <p className="text-sm leading-7 text-[var(--rs-muted)]">
            {sanction.type}: {sanction.reason}
          </p>
        </div>
        <div className="mt-6 grid gap-4">
          <label className="grid gap-2 text-sm font-medium text-[var(--rs-ink)]">
            Geri alma nedeni
            <textarea
              className="min-h-32 rounded-2xl border border-[var(--rs-border)] bg-white px-4 py-3 text-sm leading-6 text-[var(--rs-ink)] outline-none transition focus:border-[var(--rs-border-strong)] focus:ring-4 focus:ring-[rgb(5_26_36_/_0.08)]"
              maxLength={300}
              onChange={(event) => setReason(event.target.value)}
              placeholder="Geri alma gerekçesini yaz."
              required
              value={reason}
            />
          </label>
        </div>
        <div className="mt-6 flex flex-col gap-3 sm:flex-row sm:justify-end">
          <Button disabled={isSubmitting} onClick={onDismiss} type="button" variant="secondary">
            Vazgeç
          </Button>
          <Button disabled={isSubmitting || !reason.trim()} type="submit" variant="secondary">
            {isSubmitting ? "Geri alınıyor" : "Yaptırımı geri al"}
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

function getRevokeErrorCopy(status: number) {
  if (status === 400) {
    return "Geri alma isteği geçerli değil.";
  }
  if (status === 401) {
    return "Platform oturumu doğrulanamadı; tekrar giriş gerekebilir.";
  }
  if (status === 403) {
    return "Bu aksiyon için PlatformAdmin step-up oturumu gerekiyor.";
  }
  if (status === 404) {
    return "Kayıt bulunamadı.";
  }
  if (status === 409) {
    return "Kayıt zaten geri alınmış olabilir.";
  }
  if (status === 422) {
    return "Backend kuralı geri almayı reddetti.";
  }
  if (status === 429) {
    return "Platform operasyon rate limit'i devrede.";
  }
  return "Geri alma uygulanamadı.";
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
