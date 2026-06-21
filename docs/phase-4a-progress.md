# Phase 4a Implementation Progress

## Status: PARTIALLY IMPLEMENTED

This document tracks the implementation progress of Phase 4a (Deposit and No-show payments).

## Completed ✓

### 1. Domain Model Enhancements
- ✅ **PaymentPolicy** extended with no-show fee support:
  - Added `NoShowFixedAmount` property
  - Added `NoShowPercentage` property
  - Updated `Configure()` method to accept no-show parameters
  - Validation: no-show fee can be fixed amount OR percentage, not both
  - Validation: no-show amount must be non-negative
  - Validation: no-show percentage must be between 0-100%

- ✅ **PaymentIntentPurpose** extended:
  - Added `NoShowFee` enum value
  - Supports `AppointmentDeposit`, `AppointmentPrepayment`, `NoShowFee`, `SubscriptionInvoice`

### 2. Application Layer (Interface Design)
- ✅ **IPaymentProviderAdapter** created:
  - `CreateCheckoutSessionAsync()` - Creates hosted checkout session
  - `ProcessWebhookEventAsync()` - Handles webhook events with signature verification
  - `GetPaymentStatusAsync()` - Queries payment status
  - Supporting types: `CreateCheckoutCommand`, `HostedCheckoutResult`, `ProcessWebhookCommand`, `WebhookProcessingResult`

### 3. Existing Foundation (From ADR-065)
- ✅ **PaymentIntent** domain entity with proper lifecycle:
  - `CreateHostedCheckout()` - Creates pending intent
  - `AttachCheckout()` - Attaches provider checkout URL
  - `MarkPaid()` - Transitions to paid state
  - `MarkFailed()` - Transitions to failed state
  - Status states: `PendingCheckout`, `CheckoutCreated`, `Paid`, `Failed`, `Expired`, `Cancelled`, `Refunded`

- ✅ **PaymentPolicy** domain entity:
  - Supports `Disabled`, `PayAtStore`, `Deposit`, `FullPrepayment` modes
  - Configurable fixed amount or percentage for deposits
  - Hosted checkout capability flag
  - Provider key storage (not raw secrets)

- ✅ **PaymentCollectionMode** enum with all required modes

- ✅ **PaymentsDbContext** with separate schema for payments module

- ✅ **PaymentReadinessOptions** configuration:
  - `OnlineCollectionEnabled` - Feature flag
  - `ProviderKey` - Provider identifier (not raw secret)
  - `WebhookMaxPayloadBytes` - Security limit

- ✅ **PaymentWebhookEvent** domain with:
  - Idempotent delivery via unique index
  - SHA-256 payload hash
  - Event type tracking
  - Delivery status

- ✅ **PaymentAuditLogEntry** for audit trail

## Pending Implementation ⏳

### 1. Payment Provider Adapter (Stripe Implementation)
- ❌ **StripePaymentProviderAdapter** implementation:
  - Stripe SDK integration
  - Checkout session creation
  - Webhook signature verification using Stripe webhook signing
  - Payment status query

### 2. Application Services
- ❌ **PaymentIntentCreationService**:
  - Calculate deposit amount based on policy
  - Calculate no-show fee amount based on policy
  - Create `PaymentIntent` entity
  - Call provider adapter to create checkout
  - Update intent with checkout URL

- ❌ **PaymentWebhookProcessingService**:
  - Validate webhook signature
  - Parse event type
  - Update `PaymentIntent` status based on event
  - Create `PaymentWebhookEvent` record
  - Idempotency handling via unique event ID

- ❌ **BusinessPaymentSettingsService**:
  - Read payment policy for tenant/branch
  - Update payment policy (with step-up authz requirement)
  - Tenant isolation enforcement
  - Audit logging

### 3. API Endpoints
- ❌ **Business Payment Settings Endpoints**:
  - `GET /api/business/payment-settings` - Read current policy
  - `POST /api/business/payment-settings` - Configure policy (requires `BusinessOwner` + step-up)
  - `GET /api/business/payment-settings/readiness` - Feature readiness check

- ❌ **Customer Payment Endpoints**:
  - `POST /api/customer/payment-intents` - Create deposit payment intent
  - `POST /api/customer/payment-intents/{id}/no-show` - Create no-show payment intent
  - `GET /api/customer/payment-intents/{id}` - Get payment intent status
  - Redirect to hosted checkout URL

- ❌ **Payment Webhook Endpoint**:
  - `POST /api/payments/webhook` - Handle provider webhook
  - Signature verification
  - Idempotent processing

### 4. Persistence Layer
- ❌ **Repositories**:
  - `IPaymentPolicyRepository` - CRUD for payment policies
  - `IPaymentIntentRepository` - CRUD for payment intents
  - `IPaymentWebhookEventRepository` - Write for webhook events
  - `IPaymentAuditLogRepository` - Append-only audit

- ❌ **EF Core DbContext Updates**:
  - Add `NoShowFixedAmount` and `NoShowPercentage` to PaymentPolicy entity configuration
  - Ensure proper indexes for tenant + branch queries

- ❌ **Migration**:
  - Add new columns to PaymentPolicy table
  - Update PaymentIntentPurpose enum in DB if needed

### 5. Frontend Implementation
- ❌ **Business Payment Settings UI**:
  - `/panel/ayarlar/odeme` - Payment settings page
  - Form to configure payment mode
  - Deposit amount (fixed or percentage)
  - No-show fee (fixed or percentage)
  - Currency selection
  - Hosted checkout toggle
  - Step-up authz integration

- ❌ **Customer Payment Flow UI**:
  - Deposit payment info on appointment request
  - No-show fee info after no-show marking
  - Redirect to hosted checkout
  - Payment success/failure display

### 6. Configuration & Deployment
- ❌ **appsettings.json**:
  - Payment provider configuration
  - Webhook signing secret (from secret manager, not repo)

- ❌ **Environment Setup**:
  - Stripe account setup
  - Webhook endpoint registration
  - Provider secret injection

### 7. Documentation
- ❌ **ADR Update**: Add ADR for Stripe provider selection and webhook signature verification
- ❌ **Operations Runbook**: Payment error handling, refund processing, webhook retry policy
- ❌ **Frontend Docs**: Payment flow guide for customers
- ❌ **Business Docs**: Payment configuration guide

## Security Requirements (To Verify)

- ❌ Payment policy mutation requires `BusinessOwner` + tenant-scope step-up OR `PlatformAdminWithStepUp`
- ❌ Payment settings read-only readiness requires `PlatformAdminWithStepUp`
- ❌ Webhook raw payload NOT stored (hash only)
- ❌ Provider secrets NOT in repo/config
- ❌ Payment audit logs are append-only
- ❌ Tenant isolation enforced on all payment queries
- ❌ PII not logged in payment operations

## Dependencies

- Phase 3 (booking operations, tenant lifecycle, abuse control) ✅
- PaymentReadinessOptions configuration ⏳
- Stripe SDK (needs NuGet package) ⏳

## Acceptance Criteria Status

| Criterion | Status |
|-----------|--------|
| Card data never stored in system | ⏳ (hosted checkout not implemented) |
| All payments via hosted checkout | ⏳ |
| Webhooks verified with signature | ⏳ (adapter interface defined, implementation pending) |
| Idempotent webhook processing | ⏳ |
| Idempotency-Key prevents double charges | ⏳ (not yet added to endpoints) |
| Refund/error scenarios documented | ❌ |
| Payments module requires explicit config to enable | ⏳ |
| Tenant/branch authz enforced | ⏳ |
| PII not logged | ⏳ |

## Next Steps

1. Implement Stripe adapter with webhook signature verification
2. Create application services for payment intent creation
3. Create repositories and update persistence layer
4. Implement API endpoints with proper authz
5. Build frontend UI components
6. Add integration tests
7. Document operations runbook
8. Update ADR for provider selection