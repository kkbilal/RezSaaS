import assert from "node:assert/strict";
import test from "node:test";
import {
  bookingDraftTtlMs,
  createBookingDraft,
  parseBookingDraft,
  shouldRecoverSlotSelection
} from "./booking-draft.ts";

const nowMs = Date.UTC(2026, 5, 14, 9, 0, 0);

test("createBookingDraft stores only booking intent fields with a short TTL", () => {
  const draft = createBookingDraft(
    {
      branchSlug: "kadikoy",
      businessSlug: "rezsaas-merkez",
      date: "2026-06-15",
      idempotencyKey: "web_public-booking_123",
      serviceVariantIds: ["variant-1", "variant-2"],
      staffMemberId: "staff-1",
      startUtc: "2026-06-15T09:00:00Z"
    },
    nowMs
  );

  assert.equal(draft.expiresAt, nowMs + bookingDraftTtlMs);
  assert.equal(draft.version, 1);
  assert.equal("customerEmail" in draft, false);
  assert.equal("customerPhone" in draft, false);
});

test("parseBookingDraft rejects expired, cross-business and malformed drafts", () => {
  const validDraft = createBookingDraft(
    {
      branchSlug: "kadikoy",
      businessSlug: "rezsaas-merkez",
      date: "2026-06-15",
      idempotencyKey: "web_public-booking_123",
      serviceVariantIds: ["variant-1"],
      startUtc: "2026-06-15T09:00:00Z"
    },
    nowMs
  );

  assert.deepEqual(
    parseBookingDraft(JSON.stringify(validDraft), "rezsaas-merkez", nowMs),
    validDraft
  );
  assert.equal(
    parseBookingDraft(JSON.stringify(validDraft), "other-business", nowMs),
    null
  );
  assert.equal(
    parseBookingDraft(
      JSON.stringify(validDraft),
      "rezsaas-merkez",
      nowMs + bookingDraftTtlMs + 1
    ),
    null
  );
  assert.equal(parseBookingDraft("{broken", "rezsaas-merkez", nowMs), null);
});

test("shouldRecoverSlotSelection only marks slot-changing create failures", () => {
  assert.equal(shouldRecoverSlotSelection(409), true);
  assert.equal(shouldRecoverSlotSelection(422), true);
  assert.equal(shouldRecoverSlotSelection(401), false);
  assert.equal(shouldRecoverSlotSelection(429), false);
});
