import assert from "node:assert/strict";
import test from "node:test";
import {
  getTenantLifecycleActionAvailability,
  getTenantLifecycleActionConfig,
  normalizeReason,
  tenantLifecycleReasonMaxLength,
  validateTenantLifecycleActionDraft
} from "./platform-action-policy.ts";

const activeTenant = {
  displayName: "RezSaaS Merkez",
  slug: "rezsaas-merkez",
  status: "Active",
  tenantId: "018f5e2c-46ca-7a41-9105-5b7d84a92d35"
};

test("tenant lifecycle config uses exact slug confirmation and terminal close phrase", () => {
  assert.equal(
    getTenantLifecycleActionConfig("suspend", activeTenant).confirmPhrase,
    "rezsaas-merkez"
  );
  assert.equal(
    getTenantLifecycleActionConfig("close", activeTenant).confirmPhrase,
    "KAPAT rezsaas-merkez"
  );
});

test("tenant lifecycle availability keeps terminal and status-specific actions closed", () => {
  assert.equal(getTenantLifecycleActionAvailability("suspend", activeTenant), null);
  assert.equal(
    getTenantLifecycleActionAvailability("reactivate", activeTenant),
    "Reactivate yalnız Suspended tenant için açılır."
  );
  assert.equal(
    getTenantLifecycleActionAvailability("close", {
      ...activeTenant,
      status: "Closed"
    }),
    "Closed tenant terminaldir."
  );
});

test("validateTenantLifecycleActionDraft accepts clean reason and exact confirmation", () => {
  const result = validateTenantLifecycleActionDraft("suspend", activeTenant, {
    confirmation: "rezsaas-merkez",
    reason: "  Operasyonel inceleme tamamlanana kadar geçici askıya alma. "
  });

  assert.deepEqual(result.errors, []);
  assert.equal(
    result.normalizedReason,
    "Operasyonel inceleme tamamlanana kadar geçici askıya alma."
  );
});

test("validateTenantLifecycleActionDraft rejects missing reason, wrong confirmation and sensitive text", () => {
  const result = validateTenantLifecycleActionDraft("close", activeTenant, {
    confirmation: "rezsaas-merkez",
    reason: "owner@example.com token sızdı"
  });

  assert.equal(result.errors.length, 2);
  assert.equal(
    result.errors.includes(
      "Operasyon nedeni e-posta, telefon, token, parola veya secret içermemeli."
    ),
    true
  );
  assert.equal(
    result.errors.includes("Onay metni tam olarak 'KAPAT rezsaas-merkez' olmalı."),
    true
  );

  const emptyResult = validateTenantLifecycleActionDraft("suspend", activeTenant, {
    confirmation: "rezsaas-merkez",
    reason: " "
  });
  assert.equal(emptyResult.errors.includes("Operasyon nedeni zorunlu."), true);
});

test("validateTenantLifecycleActionDraft enforces reason length", () => {
  const result = validateTenantLifecycleActionDraft("suspend", activeTenant, {
    confirmation: "rezsaas-merkez",
    reason: "a".repeat(tenantLifecycleReasonMaxLength + 1)
  });

  assert.equal(
    result.errors.includes(
      `Operasyon nedeni ${tenantLifecycleReasonMaxLength} karakteri aşamaz.`
    ),
    true
  );
});

test("normalizeReason collapses whitespace", () => {
  assert.equal(normalizeReason("  bir   iki\nüç  "), "bir iki üç");
});
