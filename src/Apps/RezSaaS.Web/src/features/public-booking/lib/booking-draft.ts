export type BookingDraft = {
  branchSlug: string;
  businessSlug: string;
  date: string;
  expiresAt: number;
  idempotencyKey: string;
  serviceVariantIds: string[];
  staffMemberId?: string;
  startUtc: string;
  version: 1;
};

export const bookingDraftStorageKey = "rezsaas.bookingDraft.v1";
export const bookingDraftTtlMs = 30 * 60 * 1000;

export function createBookingDraft(
  draft: Omit<BookingDraft, "expiresAt" | "version">,
  nowMs: number = Date.now()
): BookingDraft {
  return {
    ...draft,
    expiresAt: nowMs + bookingDraftTtlMs,
    version: 1
  };
}

export function parseBookingDraft(
  rawDraft: string | null,
  businessSlug: string,
  nowMs: number = Date.now()
) {
  if (!rawDraft) {
    return null;
  }

  try {
    const draft = JSON.parse(rawDraft) as Partial<BookingDraft>;

    if (!isBookingDraft(draft)) {
      return null;
    }

    if (draft.businessSlug !== businessSlug || draft.expiresAt <= nowMs) {
      return null;
    }

    return draft;
  } catch {
    return null;
  }
}

export function shouldRecoverSlotSelection(status: number) {
  return status === 409 || status === 422;
}

function isBookingDraft(value: Partial<BookingDraft>): value is BookingDraft {
  return (
    value.version === 1 &&
    typeof value.branchSlug === "string" &&
    value.branchSlug.length > 0 &&
    typeof value.businessSlug === "string" &&
    value.businessSlug.length > 0 &&
    typeof value.date === "string" &&
    value.date.length > 0 &&
    typeof value.expiresAt === "number" &&
    Number.isFinite(value.expiresAt) &&
    typeof value.idempotencyKey === "string" &&
    value.idempotencyKey.length > 0 &&
    Array.isArray(value.serviceVariantIds) &&
    value.serviceVariantIds.every((item) => typeof item === "string" && item.length > 0) &&
    typeof value.startUtc === "string" &&
    value.startUtc.length > 0 &&
    (value.staffMemberId === undefined || typeof value.staffMemberId === "string")
  );
}
