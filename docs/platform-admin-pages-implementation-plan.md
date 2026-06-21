# Platform Admin Pages Implementation Plan

Last updated: 2026-06-20

This document outlines the implementation plan for all critical platform admin pages that complete F5 (Platform Control-plane).

## Overview

Based on `docs/frontend-implementation-status.md`, the following platform admin pages are CRITICAL and backend-ready:

1. **Tenant Provisioning** - Create new tenant
2. **Membership Management** - Add/suspend/revoke tenant members
3. **Abuse Decision UI** - Review and decide on abuse events
4. **Appeal Review UI** - Process customer appeals
5. **Closure Case Management** - Handle account closure proposals

---

## Page 1: Tenant Provisioning (`/platform/tenantlar/yeni`)

### Purpose
Allow PlatformAdminWithStepUp to create new tenants with validated business information.

### Features
- Business name and slug input
- Owner email validation (must be active UserAccount)
- Slug availability check (no conflicts)
- Initial business category selection
- Onboarding checklist display
- Step-up auth enforcement

### Form Fields
- Business Name (required)
- Business Slug (required, auto-suggested from name, validated for uniqueness)
- Owner Email (required, validated against existing accounts)
- Category (optional, dropdown)
- Initial Notes (optional, max 500 chars)

### API Endpoints
- `GET /api/admin/users?email={email}` - Validate owner email exists
- `POST /api/admin/tenants` - Create new tenant (requires PlatformAdminWithStepUp)

### Validation Rules
- Business name: 3-100 chars
- Business slug: 3-50 chars, alphanumeric + hyphens, no leading/trailing hyphens
- Owner email: must exist and be active UserAccount
- Slug must not conflict with existing tenant slugs

### Component Structure
```typescript
// src/Apps/RezSaaS.Web/src/app/platform/tenantlar/yeni/page.tsx
export default function TenantProvisioningPage() {
  // Form state for business info
  // Slug generation and validation
  // Owner email lookup
  // Step-up verification
  // Create tenant API call
  // Success/error handling
  // Redirect to tenant detail
}
```

### UX Requirements
- Real-time slug validation debounced (300ms)
- Owner email lookup shows user display name if found
- Step-up dialog if not already authenticated with step-up
- Clear error messages for conflicts
- Success toast with link to new tenant detail

### Security
- Step-up auth required (PlatformAdminWithStepUp policy)
- Rate limiting on create endpoint
- Audit trail for all provisionings
- No tenant header (platform-global operation)

---

## Page 2: Membership Management (`/platform/tenantlar/{tenantId}/uyeler`)

### Purpose
Manage tenant memberships (add, suspend, revoke members).

### Features
- List current tenant memberships
- Add new member (email lookup, role selection)
- Suspend member (with reason)
- Revoke membership (with reason)
- Member activity log
- Role: BusinessOwner, BranchManager, Staff

### List View Columns
- Member name and email
- Role
- Status (Active, Suspended, Revoked)
- Created date
- Last activity
- Actions (suspend/revoke)

### API Endpoints
- `GET /api/admin/tenants/{tenantId}/memberships` - List memberships
- `POST /api/admin/tenants/{tenantId}/memberships` - Add member
- `POST /api/admin/tenants/{tenantId}/memberships/{membershipId}/suspend` - Suspend member
- `POST /api/admin/tenants/{tenantId}/memberships/{membershipId}/revoke` - Revoke membership

### Validation Rules
- Last active BusinessOwner cannot be suspended/revoked
- Target user must be active UserAccount
- Target user cannot have active membership in same tenant
- Reason required for suspend/revoke (min 10 chars, max 500)

### Component Structure
```typescript
// src/Apps/RezSaaS.Web/src/app/platform/tenantlar/[tenantId]/uyeler/page.tsx
export default function TenantMembershipPage({ params }: { params: { tenantId: string } }) {
  // List current memberships
  // Add member form (email lookup, role dropdown)
  // Suspend modal (reason input)
  // Revoke modal (reason input, warning)
  // Activity log drawer
}
```

### UX Requirements
- Email lookup shows existing user display name
- Warning when trying to suspend/revoke last BusinessOwner
- Confirmation dialogs for suspend/revoke
- Clear status badges
- Activity log shows membership changes

### Security
- Step-up auth required
- Tenant context verified via path param
- Cannot modify own membership
- Audit trail for all membership changes
- No tenant header (platform operation via path)

---

## Page 3: Abuse Decision UI (`/platform/abuse/kararlar/{eventId}`)

### Purpose
Review abuse events and apply strikes/sanctions.

### Features
- Abuse event details display
- Related reports and evidence
- User abuse history overview
- Strike counter display
- Risk level calculation
- Confirm/reject abuse report
- Apply strike (if confirmed)
- Apply sanction (if risk level appropriate)
- Reason logging and audit trail

### Display Sections
1. **Event Details**
   - Event ID, type, timestamp
   - Business context (if applicable)
   - Appointment/request context

2. **User Overview**
   - User display name (PII masked in logs)
   - Strike count and history
   - Active sanctions
   - Risk level calculation

3. **Reports & Evidence**
   - Business abuse reports
   - System-detected abuse signals
   - Related events

4. **Decision Actions**
   - Confirm report → Strike confirmation dialog
   - Reject report → Reason required
   - Apply sanction → Sanction type selection

### API Endpoints
- `GET /api/admin/abuse/events/{eventId}` - Event details
- `GET /api/admin/abuse/user/{userAccountId}/overview` - User abuse history
- `POST /api/admin/abuse/events/{eventId}/confirm` - Confirm abuse
- `POST /api/admin/abuse/events/{eventId}/reject` - Reject abuse
- `POST /api/admin/abuse/user/{userAccountId}/strikes` - Apply strike
- `POST /api/admin/abuse/user/{userAccountId}/sanctions` - Apply sanction

### Validation Rules
- Confirm: must verify abuse evidence is sufficient
- Reject: reason required (min 10 chars)
- Strike: duration required (24-72 hours), reason required
- Sanction: type required (TemporaryBan), duration required, reason required
- Cannot apply overlapping sanctions
- Cannot sanction if closure case pending

### Component Structure
```typescript
// src/Apps/RezSaaS.Web/src/app/platform/abuse/kararlar/[eventId]/page.tsx
export default function AbuseDecisionPage({ params }: { params: { eventId: string } }) {
  // Load event details
  // Load user abuse overview
  // Display reports and evidence
  // Confirm/reject dialogs
  // Strike apply dialog
  // Sanction apply dialog
  // Risk level calculation
}
```

### UX Requirements
- Clear separation between InternalReason and CustomerNotice
- User PII masked in all displays
- Risk level color-coded (Low=green, Medium=yellow, High=red)
- Warning if closure case pending
- Confirmation dialogs for all decisions
- Audit trail visibility

### Security
- Step-up auth required
- PII redaction enforced
- Audit trail for all decisions
- No tenant header (platform-global)
- InternalReason never shown to customer

---

## Page 4: Appeal Review UI (`/platform/itirazlar/inceleme/{appealId}`)

### Purpose
Review and decide on customer abuse appeals.

### Features
- Appeal details and customer statement
- Related abuse evidence
- Strike/sanction history
- Accept/reject decision
- Sanction reversal (if accepted)
- Customer notice composition
- Appeal window eligibility check

### Display Sections
1. **Appeal Details**
   - Appeal ID, submitted timestamp
   - Customer statement (max 1000 chars)
   - Related abuse event/strike/sanction

2. **Abuse Evidence**
   - Original abuse report
   - Applied strike/sanction details
   - Risk assessment at time of decision

3. **Decision Actions**
   - Accept appeal → Revoke strike/sanction dialog
   - Reject appeal → Reason required
   - Customer notice composition (if accept)

### API Endpoints
- `GET /api/admin/abuse/appeals/{appealId}` - Appeal details
- `POST /api/admin/abuse/appeals/{appealId}/accept` - Accept appeal
- `POST /api/admin/abuse/appeals/{appealId}/reject` - Reject appeal
- `POST /api/admin/abuse/user/{userAccountId}/strikes/{strikeId}/revoke` - Revoke strike
- `POST /api/admin/abuse/user/{userAccountId}/sanctions/{sanctionId}/revoke` - Revoke sanction

### Validation Rules
- Accept: CustomerNotice required (max 500 chars)
- Reject: InternalReason required (min 10 chars, max 500)
- Cannot accept appeal if closure case already executed
- Revoking must include actor info and reason in audit

### Component Structure
```typescript
// src/Apps/RezSaaS.Web/src/app/platform/itirazlar/inceleme/[appealId]/page.tsx
export.tsx
export default function AppealReviewPage({ params }: { params: { appealId: string } }) {
  // Load appeal details
  // Display customer statement
  // Display abuse evidence
  // Accept/reject dialogs
  // Revoke dialogs (if accept)
  // Customer notice editor
}
```

### UX Requirements
- Customer statement displayed as submitted
- InternalReason never shown to customer
- CustomerNotice composed separately
- Clear distinction between revoke reason and customer notice
- Warning if closure case pending

### Security
- Step-up auth required
- PII redaction enforced
- InternalReason never in response/logs
- Audit trail for all decisions
- No tenant header

---

## Page 5: Closure Case Management (`/platform/kapamalar/{caseId}`)

### Purpose
Review and execute permanent account closure cases.

### Features
- Closure proposal details
- Strike count and risk assessment
- Second admin review UI
- Approve/reject proposal
- Execute closure (if approved and eligible)
- Customer notice preview
- Appeal window display
- Execution eligibility checks

### Display Sections
1. **Case Details**
   - Case ID, proposed timestamp
   - Proposed reason (internal + customer)
   - Strike count and risk level
   - Related sanctions

2. **Risk Assessment**
   - Current strike count
   - Risk level calculation
   - High-risk evidence summary

3. **Appeal Window**
   - Start time (when CustomerNoticeDeliveredAtUtc)
   - End time (proposal validity)
   - Open appeals count

4. **Second Review**
   - Review by different PlatformAdminWithStepUp
   - Review notes (internal)
   - Approve/reject decision

5. **Execution**
   - Eligibility check (no open appeals, risk still High)
   - Customer notice preview
   - Execute button (if eligible)
   - Execution status and timestamp

### API Endpoints
- `GET /api/admin/abuse/closure-cases/{caseId}` - Case details
- `POST /api/admin/abuse/closure-cases/{caseId}/review` - Second admin review
- `POST /api/admin/abuse/closure-cases/{caseId}/approve` - Approve proposal
- `POST /api/admin/abuse/closure-cases/{caseId}/reject` - Reject proposal
- `POST /api/admin/abuse/closure-cases/{caseId}/execute` - Execute closure

### Validation Rules
- Review: requires different PlatformAdminWithStepUp, InternalReason required
- Approve: requires second review, InternalReason required
- Reject: InternalReason required (min 10 chars)
- Execute: must have no open appeals, risk still High, CustomerNotice delivered
- Cannot execute if status not Approved
- Cannot execute if Identity closure already in progress

### Component Structure
```typescript
// src/Apps/RezSaaS.Web/src/app/platform/kapamalar/[caseId]/page.tsx
export default function ClosureCasePage({ params }: { params: { caseId: string } }) {
  // Load case details
  // Display proposal and evidence
  // Display strike count and risk
  // Second review UI (if not reviewed)
  // Approve/reject dialogs
  // Execute button (if eligible)
  // Customer notice preview
  // Execution status
}
```

### UX Requirements
- Second admin requirement clearly displayed
- Appeal window timer/countdown
- Execution eligibility clearly shown
- Warning before execute
- Customer notice preview
- Execution status with timestamp
- Cannot close case if Identity closure in progress

### Security
- **Two different PlatformAdminWithStepUp required** (proposal + review)
- Step-up auth required for all actions
- PII redaction enforced
- InternalReason never in response/logs
- Audit trail for all decisions
- No tenant header
- Row lock during execution to prevent race conditions

---

## Common Platform Admin Patterns

### Step-Up Auth
All platform admin pages require step-up authentication. Pattern:
```typescript
// Check step-up status
if (!session.stepUpVerified) {
  // Show step-up form or redirect
}
```

### Audit Trail
All mutations require reason logging:
```typescript
// InternalReason: For admin audit (never shown to customer)
// CustomerNotice: For customer communication
```

### PII Redaction
User PII must be masked:
```typescript
// Email: b***@gmail.com
// Phone: +90 5** *** 1234
// Never log raw PII
```

### Toast Messages
All operations show toast feedback:
```typescript
// Success: "Tenant oluşturuldu"
// Error: "Tenant oluşturulamadı: {reason}"
// Loading: "İşleniyor..."
```

### Confirmation Dialogs
All destructive actions require confirmation:
```typescript
// Two-step confirmation for high-risk actions
// Exact confirmation text required
// Warning text with consequences
```

---

## Implementation Order

### Priority 1 - Tenant Management (Foundation)
1. **Tenant Provisioning** - Core platform capability
2. **Membership Management** - Completes tenant lifecycle

### Priority 2 - Abuse/Appeal/Closure (Safety)
3. **Abuse Decision UI** - Immediate abuse response
4. **Appeal Review UI** - Customer fairness
5. **Closure Case Management** - Permanent sanctions

---

## Technical Considerations

### Client Components
All platform admin pages are client components ("use client") due to:
- Complex form state
- Dialog/modal interactions
- Real-time validation
- Toast notifications

### API Client
Use `apiClient` from `@/shared/api/client` for all API calls.

### Navigation
Use `useRouter` from `next/navigation` for redirects and refresh.

### Error Handling
- Try-catch all API calls
- Show user-friendly error messages
- Log technical errors (without PII)
- Retry on network errors with exponential backoff

### Loading States
- Show loading skeletons during data fetch
- Disable buttons during mutations
- Show spinner for async operations

---

## Testing Checklist

Each page should have:
- [ ] Happy path tests (all success scenarios)
- [ ] Validation error tests
- [ ] Step-up auth enforcement
- [ ] PII redaction verification
- [ ] Audit trail verification
- [ ] Rate limiting (where applicable)
- [ ] Cross-tenant isolation (no tenant header)
- [ ] Permission checks (PlatformAdminWithStepUp)

---

## Documentation Updates

After implementation, update:
- `docs/frontend-implementation-status.md` - Mark pages as complete
- `docs/24-frontend-uygulama-plani.md` - Update F5 status
- `docs/06-karar-kaydi.md` - Add ADR if new decisions made

---

## Next Steps

1. Implement **Tenant Provisioning** page first
2. Test with real step-up auth
3. Implement **Membership Management**
4. Implement **Abuse Decision UI**
5. Implement **Appeal Review UI**
6. Implement **Closure Case Management**
7. Update F5 status in frontend plan
8. Mark frontend implementation status as complete for platform admin