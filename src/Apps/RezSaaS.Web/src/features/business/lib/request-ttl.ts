export type RequestTtlLevel = "critical" | "warning" | "normal" | "expired";

export type RequestTtlStatus = {
  level: RequestTtlLevel;
  /** Negative when expired. */
  secondsLeft: number;
  /** Human-friendly Turkish label, e.g. "12 dk kaldı". */
  label: string;
};

const criticalThresholdSeconds = 5 * 60;
const warningThresholdSeconds = 15 * 60;

export function getRequestTtlStatus(
  expiresAtUtc: string | null | undefined,
  nowUtc: string
): RequestTtlStatus | null {
  if (!expiresAtUtc) {
    return null;
  }

  const expires = Date.parse(expiresAtUtc);
  const now = Date.parse(nowUtc);

  if (Number.isNaN(expires) || Number.isNaN(now)) {
    return null;
  }

  const secondsLeft = Math.round((expires - now) / 1000);

  if (secondsLeft <= 0) {
    return {
      level: "expired",
      secondsLeft,
      label: "Süresi doldu"
    };
  }

  if (secondsLeft <= criticalThresholdSeconds) {
    return {
      level: "critical",
      secondsLeft,
      label: formatCountdown(secondsLeft)
    };
  }

  if (secondsLeft <= warningThresholdSeconds) {
    return {
      level: "warning",
      secondsLeft,
      label: formatCountdown(secondsLeft)
    };
  }

  return {
    level: "normal",
    secondsLeft,
    label: formatCountdown(secondsLeft)
  };
}

export function isRequestUrgent(
  expiresAtUtc: string | null | undefined,
  nowUtc: string
): boolean {
  const status = getRequestTtlStatus(expiresAtUtc, nowUtc);
  return status?.level === "critical" || status?.level === "warning" || false;
}

function formatCountdown(seconds: number): string {
  if (seconds < 60) {
    return `${seconds} sn kaldı`;
  }

  const minutes = Math.floor(seconds / 60);

  if (minutes < 60) {
    return `${minutes} dk kaldı`;
  }

  const hours = Math.floor(minutes / 60);
  const remainingMinutes = minutes % 60;

  if (remainingMinutes === 0) {
    return `${hours} sa kaldı`;
  }

  return `${hours} sa ${remainingMinutes} dk kaldı`;
}
