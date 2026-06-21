import assert from "node:assert/strict";
import test from "node:test";
import {
  getBusinessRequestConflictSignals,
  getPendingApprovalConflictSignals,
  hasPendingApprovalConflict,
  shouldOptimisticallySupersede,
  type BusinessRequestConflictInput
} from "./business-request-conflicts.ts";

function pending(
  overrides: Partial<BusinessRequestConflictInput> = {}
): BusinessRequestConflictInput {
  return {
    branchId: "branch-1",
    id: "request-1",
    requestedEndUtc: "2026-06-15T11:00:00Z",
    requestedStartUtc: "2026-06-15T10:00:00Z",
    resourceId: "resource-1",
    staffMemberId: "staff-1",
    status: "PendingApproval",
    ...overrides
  };
}

test("same staff and overlapping time conflicts even with a different resource", () => {
  const request = pending();
  const candidate = pending({
    id: "request-2",
    requestedEndUtc: "2026-06-15T11:30:00Z",
    requestedStartUtc: "2026-06-15T10:30:00Z",
    resourceId: "resource-2"
  });

  assert.deepEqual(getBusinessRequestConflictSignals(request, candidate), [
    "staff"
  ]);
  assert.equal(hasPendingApprovalConflict(request, [request, candidate]), true);
});

test("same resource and overlapping time conflicts even with a different staff", () => {
  const request = pending();
  const candidate = pending({
    id: "request-2",
    requestedEndUtc: "2026-06-15T10:45:00Z",
    requestedStartUtc: "2026-06-15T09:45:00Z",
    staffMemberId: "staff-2"
  });

  assert.deepEqual(getBusinessRequestConflictSignals(request, candidate), [
    "resource"
  ]);
  assert.deepEqual(getPendingApprovalConflictSignals(request, [candidate]), [
    "resource"
  ]);
});

test("different staff and resource in the same branch window is not a conflict", () => {
  const request = pending();
  const candidate = pending({
    id: "request-2",
    resourceId: "resource-2",
    staffMemberId: "staff-2"
  });

  assert.deepEqual(getBusinessRequestConflictSignals(request, candidate), []);
  assert.equal(hasPendingApprovalConflict(request, [candidate]), false);
});

test("adjacent or cross-branch requests are not marked as conflicts", () => {
  const request = pending();

  assert.equal(
    hasPendingApprovalConflict(request, [
      pending({
        id: "request-2",
        requestedEndUtc: "2026-06-15T12:00:00Z",
        requestedStartUtc: "2026-06-15T11:00:00Z"
      })
    ]),
    false
  );
  assert.equal(
    hasPendingApprovalConflict(request, [
      pending({
        branchId: "branch-2",
        id: "request-3"
      })
    ]),
    false
  );
});

test("approved request supersedes pending candidates with staff or resource overlap", () => {
  const approvedRequest = pending({ status: "Approved" });

  assert.equal(
    shouldOptimisticallySupersede(
      approvedRequest,
      pending({ id: "request-2", resourceId: "resource-2" })
    ),
    true
  );
  assert.equal(
    shouldOptimisticallySupersede(
      approvedRequest,
      pending({ id: "request-3", staffMemberId: "staff-2" })
    ),
    true
  );
  assert.equal(
    shouldOptimisticallySupersede(
      approvedRequest,
      pending({
        id: "request-4",
        resourceId: "resource-2",
        staffMemberId: "staff-2"
      })
    ),
    false
  );
});
