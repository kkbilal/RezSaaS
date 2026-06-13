import assert from "node:assert/strict";
import test from "node:test";
import {
  parseBranchDateTimeLocalValue,
  parseUtcDateTimeLocalValue,
  toBranchDateTimeLocalValue,
  toUtcDateTimeLocalValue
} from "./date-time.ts";

test("toUtcDateTimeLocalValue formats an ISO instant as UTC datetime-local", () => {
  assert.equal(
    toUtcDateTimeLocalValue("2026-06-14T09:05:00.000Z"),
    "2026-06-14T09:05"
  );
});

test("parseUtcDateTimeLocalValue treats datetime-local input as UTC", () => {
  assert.equal(
    parseUtcDateTimeLocalValue("2026-06-14T09:05"),
    "2026-06-14T09:05:00.000Z"
  );
});

test("branch local conversion preserves the branch wall clock", () => {
  assert.equal(
    toBranchDateTimeLocalValue(
      "2026-06-14T09:05:00.000Z",
      "Europe/Istanbul"
    ),
    "2026-06-14T12:05"
  );

  assert.equal(
    parseBranchDateTimeLocalValue("2026-06-14T12:05", "Europe/Istanbul"),
    "2026-06-14T09:05:00.000Z"
  );
});

test("parseBranchDateTimeLocalValue rejects invalid local input", () => {
  assert.equal(
    parseBranchDateTimeLocalValue("not-a-date", "Europe/Istanbul"),
    null
  );
});
