export type BusinessRequestConflictInput = {
  branchId?: string | null;
  id?: string | null;
  requestedEndUtc?: string | null;
  requestedStartUtc?: string | null;
  resourceId?: string | null;
  staffMemberId?: string | null;
  status?: string | null;
};

export type BusinessRequestConflictSignal = "resource" | "staff";

export function getBusinessRequestConflictSignals(
  request: BusinessRequestConflictInput,
  candidate: BusinessRequestConflictInput
): BusinessRequestConflictSignal[] {
  if (request.id && candidate.id && request.id === candidate.id) {
    return [];
  }

  if (!hasComparableBranch(request, candidate) || !hasOverlappingWindow(request, candidate)) {
    return [];
  }

  const signals: BusinessRequestConflictSignal[] = [];

  if (
    request.staffMemberId &&
    candidate.staffMemberId &&
    request.staffMemberId === candidate.staffMemberId
  ) {
    signals.push("staff");
  }

  if (
    request.resourceId &&
    candidate.resourceId &&
    request.resourceId === candidate.resourceId
  ) {
    signals.push("resource");
  }

  return signals;
}

export function getPendingApprovalConflictSignals(
  request: BusinessRequestConflictInput,
  allRequests: BusinessRequestConflictInput[]
): BusinessRequestConflictSignal[] {
  if (getRequestStatus(request) !== "PendingApproval") {
    return [];
  }

  const signalSet = new Set<BusinessRequestConflictSignal>();

  for (const candidate of allRequests) {
    if (getRequestStatus(candidate) !== "PendingApproval") {
      continue;
    }

    for (const signal of getBusinessRequestConflictSignals(request, candidate)) {
      signalSet.add(signal);
    }
  }

  const orderedSignals: BusinessRequestConflictSignal[] = ["staff", "resource"];

  return orderedSignals.filter((signal) => signalSet.has(signal));
}

export function hasPendingApprovalConflict(
  request: BusinessRequestConflictInput,
  allRequests: BusinessRequestConflictInput[]
) {
  return getPendingApprovalConflictSignals(request, allRequests).length > 0;
}

export function shouldOptimisticallySupersede(
  approvedRequest: BusinessRequestConflictInput,
  candidate: BusinessRequestConflictInput
) {
  return (
    getRequestStatus(candidate) === "PendingApproval" &&
    getBusinessRequestConflictSignals(approvedRequest, candidate).length > 0
  );
}

function getRequestStatus(request: BusinessRequestConflictInput) {
  return request.status ?? "Unknown";
}

function hasComparableBranch(
  request: BusinessRequestConflictInput,
  candidate: BusinessRequestConflictInput
) {
  if (!request.branchId || !candidate.branchId) {
    return true;
  }

  return request.branchId === candidate.branchId;
}

function hasOverlappingWindow(
  request: BusinessRequestConflictInput,
  candidate: BusinessRequestConflictInput
) {
  const requestWindow = getUtcWindow(request);
  const candidateWindow = getUtcWindow(candidate);

  if (!requestWindow || !candidateWindow) {
    return false;
  }

  return requestWindow.startMs < candidateWindow.endMs &&
    candidateWindow.startMs < requestWindow.endMs;
}

function getUtcWindow(request: BusinessRequestConflictInput) {
  if (!request.requestedStartUtc || !request.requestedEndUtc) {
    return null;
  }

  const startMs = Date.parse(request.requestedStartUtc);
  const endMs = Date.parse(request.requestedEndUtc);

  if (Number.isNaN(startMs) || Number.isNaN(endMs) || endMs <= startMs) {
    return null;
  }

  return {
    endMs,
    startMs
  };
}
