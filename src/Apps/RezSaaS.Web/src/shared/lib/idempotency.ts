export type IdempotencyKeyCache = Record<string, string>;

export function createWebIdempotencyKey(
  scope: string,
  entropy: () => string = createDefaultEntropy
) {
  return `web-${scope}-${entropy()}`;
}

export function getOrCreateIntentIdempotencyKey(
  cache: IdempotencyKeyCache,
  intentId: string,
  scope: string
) {
  cache[intentId] ??= createWebIdempotencyKey(scope);

  return cache[intentId];
}

export function clearIntentIdempotencyKey(
  cache: IdempotencyKeyCache,
  intentId: string
) {
  delete cache[intentId];
}

function createDefaultEntropy() {
  return (
    globalThis.crypto?.randomUUID?.() ??
    `${Date.now().toString(36)}-${Math.random().toString(36).slice(2)}`
  );
}
