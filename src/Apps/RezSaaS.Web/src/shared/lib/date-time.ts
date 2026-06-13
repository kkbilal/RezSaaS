export function formatBranchDateTime(
  valueUtc: string,
  branchTimeZoneId: string
) {
  const value = new Date(valueUtc);

  if (Number.isNaN(value.getTime())) {
    return "Zaman bilgisi okunamıyor";
  }

  return new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: branchTimeZoneId
  }).format(value);
}

type DateTimeLocalParts = {
  day: number;
  hour: number;
  minute: number;
  month: number;
  year: number;
};

const dateTimeLocalPattern =
  /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})$/;

export function toUtcDateTimeLocalValue(valueUtc?: string | null) {
  if (!valueUtc) {
    return "";
  }

  const date = new Date(valueUtc);

  if (Number.isNaN(date.getTime())) {
    return "";
  }

  return formatDateTimeLocalParts({
    day: date.getUTCDate(),
    hour: date.getUTCHours(),
    minute: date.getUTCMinutes(),
    month: date.getUTCMonth() + 1,
    year: date.getUTCFullYear()
  });
}

export function toBranchDateTimeLocalValue(
  valueUtc?: string | null,
  branchTimeZoneId?: string | null
) {
  if (!valueUtc || !branchTimeZoneId) {
    return toUtcDateTimeLocalValue(valueUtc);
  }

  const date = new Date(valueUtc);

  if (Number.isNaN(date.getTime())) {
    return "";
  }

  try {
    return formatDateTimeLocalParts(
      getDateTimeLocalParts(date.getTime(), branchTimeZoneId)
    );
  } catch {
    return toUtcDateTimeLocalValue(valueUtc);
  }
}

export function parseUtcDateTimeLocalValue(value: string) {
  const parts = parseDateTimeLocalParts(value);

  if (!parts) {
    return null;
  }

  const utcMilliseconds = Date.UTC(
    parts.year,
    parts.month - 1,
    parts.day,
    parts.hour,
    parts.minute
  );
  const date = new Date(utcMilliseconds);

  return Number.isNaN(date.getTime()) ? null : date.toISOString();
}

export function parseBranchDateTimeLocalValue(
  value: string,
  branchTimeZoneId?: string | null
) {
  if (!branchTimeZoneId) {
    return parseUtcDateTimeLocalValue(value);
  }

  const requestedParts = parseDateTimeLocalParts(value);

  if (!requestedParts) {
    return null;
  }

  const requestedAsUtcMilliseconds = Date.UTC(
    requestedParts.year,
    requestedParts.month - 1,
    requestedParts.day,
    requestedParts.hour,
    requestedParts.minute
  );

  try {
    let guessedUtcMilliseconds = requestedAsUtcMilliseconds;

    for (let attempt = 0; attempt < 4; attempt += 1) {
      const observedParts = getDateTimeLocalParts(
        guessedUtcMilliseconds,
        branchTimeZoneId
      );
      const observedAsUtcMilliseconds = Date.UTC(
        observedParts.year,
        observedParts.month - 1,
        observedParts.day,
        observedParts.hour,
        observedParts.minute
      );
      const difference =
        requestedAsUtcMilliseconds - observedAsUtcMilliseconds;

      if (difference === 0) {
        break;
      }

      guessedUtcMilliseconds += difference;
    }

    const finalParts = getDateTimeLocalParts(
      guessedUtcMilliseconds,
      branchTimeZoneId
    );

    if (!dateTimeLocalPartsEqual(requestedParts, finalParts)) {
      return null;
    }

    return new Date(guessedUtcMilliseconds).toISOString();
  } catch {
    return null;
  }
}

function getDateTimeLocalParts(
  utcMilliseconds: number,
  branchTimeZoneId: string
): DateTimeLocalParts {
  const parts = new Intl.DateTimeFormat("en-GB", {
    day: "2-digit",
    hour: "2-digit",
    hour12: false,
    minute: "2-digit",
    month: "2-digit",
    timeZone: branchTimeZoneId,
    year: "numeric"
  }).formatToParts(new Date(utcMilliseconds));

  return {
    day: getNumberPart(parts, "day"),
    hour: getNumberPart(parts, "hour"),
    minute: getNumberPart(parts, "minute"),
    month: getNumberPart(parts, "month"),
    year: getNumberPart(parts, "year")
  };
}

function parseDateTimeLocalParts(value: string): DateTimeLocalParts | null {
  const match = dateTimeLocalPattern.exec(value);

  if (!match) {
    return null;
  }

  const [, rawYear, rawMonth, rawDay, rawHour, rawMinute] = match;
  const parts = {
    day: Number(rawDay),
    hour: Number(rawHour),
    minute: Number(rawMinute),
    month: Number(rawMonth),
    year: Number(rawYear)
  };

  if (
    parts.month < 1 ||
    parts.month > 12 ||
    parts.day < 1 ||
    parts.day > 31 ||
    parts.hour < 0 ||
    parts.hour > 23 ||
    parts.minute < 0 ||
    parts.minute > 59
  ) {
    return null;
  }

  return parts;
}

function formatDateTimeLocalParts(parts: DateTimeLocalParts) {
  return `${parts.year}-${padDatePart(parts.month)}-${padDatePart(
    parts.day
  )}T${padDatePart(parts.hour)}:${padDatePart(parts.minute)}`;
}

function getNumberPart(
  parts: Intl.DateTimeFormatPart[],
  type: Intl.DateTimeFormatPartTypes
) {
  const value = parts.find((part) => part.type === type)?.value;

  if (!value) {
    throw new Error(`Missing ${type} date-time part.`);
  }

  return Number(value);
}

function dateTimeLocalPartsEqual(
  left: DateTimeLocalParts,
  right: DateTimeLocalParts
) {
  return (
    left.year === right.year &&
    left.month === right.month &&
    left.day === right.day &&
    left.hour === right.hour &&
    left.minute === right.minute
  );
}

function padDatePart(value: number) {
  return value.toString().padStart(2, "0");
}
