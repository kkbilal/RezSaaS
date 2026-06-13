import assert from "node:assert/strict";
import test from "node:test";
import {
  clearIntentIdempotencyKey,
  createWebIdempotencyKey,
  getOrCreateIntentIdempotencyKey,
  type IdempotencyKeyCache
} from "./idempotency.ts";

test("createWebIdempotencyKey prefixes keys with the web scope", () => {
  const key = createWebIdempotencyKey("customer-cancel", () => "fixed");

  assert.equal(key, "web-customer-cancel-fixed");
});

test("getOrCreateIntentIdempotencyKey keeps the same key for the same intent", () => {
  const cache: IdempotencyKeyCache = {};
  const first = getOrCreateIntentIdempotencyKey(
    cache,
    "request-1",
    "business-approve"
  );
  const second = getOrCreateIntentIdempotencyKey(
    cache,
    "request-1",
    "business-approve"
  );

  assert.equal(first, second);
});

test("clearIntentIdempotencyKey allows a completed intent to get a fresh key", () => {
  const cache: IdempotencyKeyCache = {};
  const first = getOrCreateIntentIdempotencyKey(
    cache,
    "request-1",
    "business-approve"
  );

  clearIntentIdempotencyKey(cache, "request-1");

  const second = getOrCreateIntentIdempotencyKey(
    cache,
    "request-1",
    "business-approve"
  );

  assert.notEqual(first, second);
});
