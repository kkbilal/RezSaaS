# RezSaaS — Frontend Page Inventory & UI Specification

> **Purpose:** Complete, authoritative list of every page/screen in RezSaaS, organized by product surface and user role. Each page entry includes its route, access level, current status (exists / needs build / placeholder), and a detailed function description suitable for handing to an AI to build the UI.
>
> **Conventions:**
> - URLs are Turkish (matches existing codebase, e.g. `/giris`, `/panel`).
> - Routes use Next.js App Router; `page.tsx` files.
> - Roles: `Customer` (platform account), `Staff`, `BranchManager`, `BusinessOwner` (tenant membership), `PlatformSupport`, `PlatformAdmin` (global Identity).
> - Status legend: ✅ exists/implemented · 🟡 placeholder/mock · 🔴 needs build.

---

## Product Surfaces (Route Groups)

| Surface | Base route | Auth | Indexable | Layout |
|---|---|---|---|---|
| Public / Marketing | `/` , `/kesfet`, `/isletme/*` | None | Yes (SSR) | Public layout |
| Auth | `(auth)/` → `/giris`, `/kayit`, ... | None (gate) | No (`noindex`) | Auth layout |
| Role Dispatch | `/gelis`, `/admin` | Logged-in | No | Bare |
| Customer | `/hesabim/*` | Customer | No (`no-store`) | Customer app shell |
| Business Panel | `/panel/*` | Tenant membership | No | Business app shell (sidebar) |
| Platform Admin | `/platform/*` | `PlatformAdminWithStepUp` | No | Platform app shell (sidebar) |

---

# A. PUBLIC SURFACE (Anonymous)

## A1. Landing / Home
- **Route:** `/`
- **Status:** ✅ exists
- **Audience:** Anonymous visitors
- **Functions:**
  - Marketing hero: value proposition ("Book any service category — barber, spa, nail, tattoo").
  - Primary CTAs: "Discover businesses" → `/kesfet`, "Register" → `/kayit`, "Business login" → `/giris`.
  - Category showcase chips (hair, nail, spa, makeup, tattoo, etc.).
  - "How it works" steps for customers and for businesses.
  - Footer with legal links, login, register.
  - SEO metadata, OpenGraph, server-rendered.

## A2. Discover / Browse Businesses
- **Route:** `/kesfet`
- **Status:** ✅ exists
- **Audience:** Anonymous + logged-in
- **Functions:**
  - Business directory cards (logo, name, category, rating, address, "Book").
  - Filters: service category, city/region, search query, rating, open-now.
  - Sort: relevance, rating, distance.
  - Pagination / infinite scroll.
  - Each card links to `/isletme/[businessSlug]`.
  - Empty state ("no businesses match"), loading skeletons.

## A3. Business Public Profile
- **Route:** `/isletme/[businessSlug]`
- **Status:** ✅ exists
- **Audience:** Anonymous + logged-in
- **Functions:**
  - Business header: name, category badge, aggregated rating, address, contact CTA.
  - Image gallery (`BusinessGalleryImage`) — lightbox.
  - Branch selector (if multiple branches) — affects services/staff shown.
  - Services list with prices/durations → "Book" triggers booking flow.
  - Staff display (respecting `PublicStaffDisplayPolicy`): name, photo, skills.
  - Published reviews list + rating summary (avg, count, distribution).
  - "Book now" CTA → enters public booking flow (A4).
  - Business hours summary.

## A4. Public Booking Flow (slot picker + request creation)
- **Route:** `/isletme/[businessSlug]/rezervasyon` (suggested) — multi-step
- **Status:** 🔴 needs build (core MVP feature)
- **Audience:** **Must be authenticated** (booking request requires account). Anonymous users see auth gate → `/giris?returnTo=...`.
- **Functions / Steps:**
  1. **Service & variant selection** — multi-service allowed; total duration shown.
  2. **Staff preference** (optional) — filter by `ServiceRequiredSkill` + `StaffSkill`; "any available" option.
  3. **Date & slot picker** — calendar grid of bookable slots (`BookableSlot`); excludes confirmed appointments, staff unavailability, resource blocks; **`PendingApproval` does NOT block slots**. Branch timezone shown, times in local.
  4. **Review & submit** — summary (services, staff preference, time, price snapshot), consent, notes field; submit → creates `AppointmentRequest` (state `PendingApproval`) with exactly **1 staff + 1 resource** assigned internally.
  5. **Confirmation** — request id, status "Awaiting business approval", link to `/hesabim/talepler`.
  - **Notes:** No resource GUID/name shown to customer (resource assignment is internal). Abuse sanctions active on the user block submission.

## A5. Business Public Reviews (optional dedicated list)
- **Route:** `/isletme/[businessSlug]/degerlendirmeler`
- **Status:** 🔴 needs build (can also be a profile section)
- **Functions:** Full paginated published reviews with filter (rating, date, with photo).

---

# B. AUTH SURFACE (`(auth)/`, noindex)

## B1. Login
- **Route:** `/giris`
- **Status:** ✅ exists
- **Audience:** All (single login gate)
- **Functions:**
  - Email + password form.
  - "Forgot password" link → `/sifremi-unuttum`.
  - "Register" link → `/kayit`.
  - Return-to redirect handling (`returnTo` query param, sanitized).
  - Rate-limited / lockout-aware error messages.
  - On success → `/gelis` (role dispatch) or return-to.
  - Platform admin accounts: triggers MFA/step-up.

## B2. Register (Customer)
- **Route:** `/kayit`
- **Status:** ✅ exists
- **Audience:** Anonymous
- **Functions:**
  - Registration form: name, email, phone, password, consent.
  - E-mail verification notice (production requires confirmed email).
  - Creates `Customer` context `UserAccount`.
  - Redirect to `/giris` or auto-login → `/hesabim`.
  - Business owner onboarding is separate (tenant provisioning, see E5).

## B3. Forgot Password
- **Route:** `/sifremi-unuttum`
- **Status:** ✅ exists
- **Functions:** Email input → sends reset link (rate-limited); generic success message regardless of account existence.

## B4. Reset Password
- **Route:** `/sifre-sifirla`
- **Status:** ✅ exists
- **Functions:** Token-based new-password form; validation; redirect to login.

## B5. Email Confirmation
- **Route:** `/eposta-dogrula` (suggested)
- **Status:** 🔴 needs build
- **Functions:** Confirmation landing from email link; success/already-confirmed/expired states.

## B6. MFA / Step-up (Platform Admin)
- **Route:** `/platform/adim` or `/adim` (suggested)
- **Status:** 🔴 needs build
- **Functions:**
  - TOTP code entry for privileged sessions (`PlatformAdminWithStepUp`).
  - Recovery code fallback.
  - Step-up session establishment; expiry display.
  - Trusted-device policy (future).

## B7. Role Dispatch
- **Route:** `/gelis`
- **Status:** ✅ exists
- **Functions:** Server-side redirect based on roles:
  - `PlatformAdmin`/`PlatformSupport` → `/platform`
  - Tenant membership (`BusinessOwner`/`BranchManager`/`Staff`) → `/panel`
  - `Customer` only → `/hesabim`
  - Anonymous → `/giris`

---

# C. CUSTOMER SURFACE (`/hesabim/*`)

## C1. Customer Dashboard / Overview
- **Route:** `/hesabim` (suggested)
- **Status:** 🔴 needs build
- **Functions:**
  - Upcoming confirmed appointments (next 3).
  - Pending requests awaiting approval (count + quick list).
  - Quick actions: discover, view requests, view appointments.
  - Active sanction banner (if any) with appeal link.

## C2. My Requests (Pending Approval)
- **Route:** `/hesabim/talepler`
- **Status:** ✅ exists
- **Functions:**
  - List of `AppointmentRequest` for the logged-in customer (tenant context via `businessSlug`).
  - Status badges: `PendingApproval`, `Confirmed`/`Approved`, `Declined`, `Expired`, `Superseded`, `CancelledByCustomer`.
  - Detail drawer/page: services, staff preference, time, branch, status.
  - **Cancel** own `PendingApproval` request (idempotent).
  - 24h TTL countdown for pending.
  - Cannot see other customers' requests (404).

## C3. My Appointments
- **Route:** `/hesabim/randevular`
- **Status:** ✅ exists
- **Functions:**
  - Confirmed `Appointment` list (upcoming / past / completed / cancelled / no-show).
  - Branch timezone display (no silent tz conversion).
  - Filter tabs by status and date.
  - Cancel upcoming confirmed appointment (`CancelledByCustomer`, time policy).
  - "Leave a review" CTA for completed appointments.

## C4. Appointment Detail
- **Route:** `/hesabim/randevular/[appointmentId]` (suggested)
- **Status:** 🔴 needs build
- **Functions:** Full details (services snapshot, price, staff, branch, time), cancellation action, review action, status history.

## C5. My Reviews
- **Route:** `/hesabim/degerlendirmeler` (suggested)
- **Status:** 🔴 needs build
- **Functions:**
  - Reviews written by the customer.
  - Write/edit review for a completed appointment (rating, text, optional photos; edit window policy).
  - Status of each review (published / pending moderation / hidden).

## C6. My Appeals
- **Route:** `/hesabim/itirazlar`
- **Status:** ✅ exists
- **Functions:**
  - List of customer appeals (strikes, active blocking sanctions, eligible closure cases).
  - Status: pending review, approved, rejected.
  - Create new appeal (reason + evidence) for own strike/sanction/closure case.
  - Cannot target other users (404).

## C7. Profile / Account Settings
- **Route:** `/hesabim/profil`
- **Status:** 🟡 placeholder UI (save returns "endpoint not available")
- **Functions:**
  - Edit name, email, phone.
  - Change password.
  - Email/phone verification status & resend.
  - Manage MFA (if applicable).
  - Account status display (Active / banned).
  - Account closure request (if eligible / not under closure case).

---

# D. BUSINESS PANEL (`/panel/*`)

## D1. Business Dashboard
- **Route:** `/panel`
- **Status:** ✅ exists
- **Audience:** `BusinessOwner` (tenant-wide), `BranchManager`/`Staff` (branch-scoped)
- **Functions:**
  - Today's confirmed appointments (per selected branch scope).
  - Count of `PendingApproval` requests awaiting decision.
  - Quick KPIs: today's revenue, occupancy %, no-show rate, upcoming.
  - Branch scope switcher (for multi-branch owners/managers).
  - Recent reviews snippet.

## D2. Appointment Requests (Approve / Decline)
- **Route:** `/panel/talepler` (suggested) — may be tab of Calendar
- **Status:** 🔴 needs build (core)
- **Functions:**
  - Queue of `PendingApproval` requests for the selected branch.
  - Customer info **masked PII** (email/phone masked).
  - Service(s), duration, requested time, staff preference.
  - **Approve** → `Confirmed` appointment (transactional; conflict recheck; idempotent).
  - **Decline** (with reason).
  - On approve: conflicting other requests auto-closed as `Superseded`.
  - 24h TTL countdown; mark expired.
  - "Report abuse" action per request (idempotent, daily limit).

## D3. Calendar
- **Route:** `/panel/takvim` (suggested)
- **Status:** 🔴 needs build (core)
- **Functions:**
  - Day / week / list views of confirmed appointments.
  - Resource lanes (by `Resource`) and staff rows.
  - Click appointment → detail/operations (D4).
  - Drag-to-reschedule / rebook (with conflict recheck).
  - Filter by staff, resource, branch.
  - Show working hours overlay, blocked resources.

## D4. Appointment Detail & Operations
- **Route:** `/panel/randevular/[appointmentId]` (suggested)
- **Status:** 🔴 needs build (core)
- **Functions:**
  - Full appointment details (services, price snapshot, customer masked PII, staff, resource, time).
  - Operations (idempotent, time-gated):
    - **Complete** (only after end time).
    - **No-show** (only after start time).
    - **Cancel** (with reason).
    - **Rebook** → old = `Rebooked`, new `Confirmed` (conflict recheck).
    - **Add note**.
  - Status history / audit.

## D5. Appointments List
- **Route:** `/panel/randevular` (suggested)
- **Status:** 🔴 needs build
- **Functions:** Searchable/filterable table of all appointments by status/date/staff; export candidate.

## D6. Branches
- **Route:** `/panel/subeler`
- **Status:** ✅ exists
- **Functions:**
  - Branch CRUD (name, address, timezone, phone).
  - Per-branch public slot settings (`SlotIntervalMinutes`, `MaxPublicSlots`) — positive values.
  - Branch working hours summary.
  - Activate/deactivate branch.

## D7. Staff Members
- **Route:** `/panel/personel`
- **Status:** ✅ exists
- **Functions:**
  - Staff CRUD (name, photo, bio, display policy).
  - Assign staff to branches.
  - Assign skills (link to D8).
  - Link staff to a user account (optional).
  - Working hours per staff (link to D12).

## D8. Skills
- **Route:** `/panel/yetenekler`
- **Status:** ✅ exists
- **Functions:** Skill catalog CRUD (e.g. "Men's cut", "Gel nail"); used for service-staff matching.

## D9. Services / Catalog
- **Route:** `/panel/hizmetler`
- **Status:** ✅ exists
- **Functions:**
  - Service CRUD (name, category, description, image).
  - **Service variants** (duration, price) — multi-variant per service.
  - Required skill (`ServiceRequiredSkill`) and required resource type.
  - Active/inactive toggle.
  - Multi-service booking support indicators.

## D10. Resource Types
- **Route:** `/panel/kaynak-turleri`
- **Status:** ✅ exists
- **Functions:** Resource type CRUD (chair, room, bed, station, device) — generic capacity model.

## D11. Resources
- **Route:** `/panel/kaynaklar`
- **Status:** ✅ exists
- **Functions:**
  - Resource CRUD per branch (name, type, capacity).
  - **Block / out-of-service** scheduling (resource→branch validation, tenant authz) — affects slot finder.
  - Resource calendar/availability view.

## D12. Working Hours / Availability
- **Route:** `/panel/calisma-saatleri`
- **Status:** ✅ exists
- **Functions:**
  - Branch working hours (weekly schedule, breaks, holidays).
  - Staff availability rules / unavailability.
  - Resource block management (may overlap with D11).

## D13. Reviews Management
- **Route:** `/panel/degerlendirmeler` (suggested)
- **Status:** 🔴 needs build
- **Functions:**
  - Incoming reviews list (rating, text, customer, appointment).
  - **Reply** to reviews (within policy window).
  - Report inappropriate review.
  - Rating summary analytics.

## D14. Messaging / Notifications Settings
- **Route:** `/panel/mesajlar` (suggested)
- **Status:** 🔴 needs build (MVP: email + limited SMS)
- **Functions:**
  - Notification templates (confirmation, reminder, no-show, etc.).
  - Channel toggles (email / SMS / WhatsApp pilot).
  - Reminder timing configuration.
  - Outbox/delivery status (read-only).

## D15. Business Settings
- **Route:** `/panel/ayarlar`
- **Status:** ✅ exists
- **Functions:**
  - Business profile (name, slug, logo, description, category).
  - Public profile preview.
  - Gallery management.
  - Contact info, address.
  - Tenant membership / team management (owners view members).
  - Payment settings readiness (Phase 4, BusinessOwner + step-up).

## D16. Abuse Reports (Business)
- **Route:** `/panel/abuse-raporlari` (suggested)
- **Status:** 🔴 needs build
- **Functions:**
  - List of abuse reports submitted by this business (per appointment request).
  - Idempotency + daily reporter limit indicators.
  - Status (submitted → admin reviewed → strike/no strike).
  - Note: reporting alone does not create a strike; only `PlatformAdminWithStepUp` confirms.

---

# E. PLATFORM ADMIN SURFACE (`/platform/*`)

## E1. Platform Dashboard
- **Route:** `/platform`
- **Status:** ✅ exists
- **Audience:** `PlatformAdminWithStepUp`, `PlatformSupport` (read-only)
- **Functions:**
  - Platform KPIs: tenants, businesses, branches, users, appointments, revenue.
  - Abuse queue count, appeals pending, closure cases.
  - System health snapshot (read-only reconciliation; numbers + GUIDs only, no PII).
  - Recent platform activity.

## E2. Abuse Events
- **Route:** `/platform/abuse`
- **Status:** ✅ exists
- **Functions:**
  - Queue of abuse events (platform-global; query filter bypass via ADR).
  - Filter by status, severity, tenant, date.
  - Link to user detail (E3).
  - **Confirm strike** action (step-up, audited, time-limited, revocable) — produces strike only on admin confirm.

## E3. Abuse User Detail
- **Route:** `/platform/abuse/kullanici/[userAccountId]`
- **Status:** ✅ exists
- **Functions:**
  - User profile (account status, masked PII).
  - Strike history (reason, time, active count, risk level).
  - Sanctions applied (cooldown / temp ban) — apply/revoke (audited).
  - Sanction ladder: warning (no block), cooldown ≤24h, temp ban 24–72h; only one active blocking sanction.
  - Account closure case status (if any).
  - Transaction lock indicators.
  - **Apply sanction** / **Revoke sanction** (step-up, audited, row lock).
  - **Propose permanent closure** (workflow entry — see E8).

## E4. Appeals
- **Route:** `/platform/itirazlar`
- **Status:** ✅ exists
- **Functions:**
  - Appeal review queue (customer strikes, sanctions, closure cases).
  - Filter by status, type, risk.
  - **Review** approve/reject (step-up, audited, transaction lock).
  - Link to appeal detail.

## E5. Tenants List
- **Route:** `/platform/tenantlar`
- **Status:** ✅ exists
- **Functions:**
  - Tenant lifecycle management: `Active`, `Suspended`, `Closed`.
  - **Suspend / Reactivate / Close** (step-up, long reason, audit, row lock). `Closed` terminal.
  - Filter/search.
  - Link to members (E6) and new tenant (E7).

## E6. Tenant Members
- **Route:** `/platform/tenantlar/[tenantId]/uyeler`
- **Status:** 🟡 placeholder UI
- **Functions:**
  - Tenant membership list (`BusinessOwner`, `BranchManager`, `Staff`).
  - **Add / Suspend / Revoke** membership (step-up, audited; last active owner protected; `Revoked` terminal).
  - Verify target is active `UserAccount`; prevents global Identity role pollution.
  - Active tenant membership check before closure (link to E3/E8).

## E7. New Tenant (Provisioning)
- **Route:** `/platform/tenantlar/yeni`
- **Status:** 🟡 placeholder UI
- **Functions:**
  - Tenant + business + branch + owner user creation form.
  - Validates owner is active `UserAccount`; assigns tenant role (not global Identity role).
  - `PlatformAdminWithStepUp` gate; audited.

## E8. Account Closure Cases
- **Route:** `/platform/hesap-kapatma` (suggested)
- **Status:** 🔴 needs build
- **Functions:**
  - Closure case workflow (`Proposed` → `CustomerNoticeDeliveredAtUtc` → `EligibleForExecutionAtUtc` → `Executing` → `Closed`).
  - Requires: `High` risk proof, two distinct `PlatformAdminWithStepUp` approvals, ≥7-day appeal window, no open appeal, no platform role / active membership.
  - Proposal email delivery proof gate.
  - **Execute** retryable saga; re-validates active strike count & membership at execution.
  - Read-only reconciliation; no direct DB mutation.

## E9. Platform Notifications Outbox
- **Route:** `/platform/bildirimler` (suggested)
- **Status:** 🔴 needs build
- **Functions:**
  - Monitor transactional notification outbox (global, `UserAccountId` only; no raw email).
  - Delivery status, retry, terminal protection, unique delivery key, lease.
  - Read-only by default.

## E10. Operations / Reconciliation
- **Route:** `/platform/operasyon` (suggested)
- **Status:** 🔴 needs build
- **Functions:**
  - Health checks, alarms, reconciliation logs (numbers + operational record GUIDs; no PII/reason/subject).
  - Read-only recovery hints; recovery via idempotent app flows only; no direct DB fixes.

## E11. Audit Log
- **Route:** `/platform/audit` (suggested)
- **Status:** 🔴 needs build
- **Functions:**
  - Append-only audit viewer: role/auth changes, business settings, approve/decline/cancel, ban/strike, tenant lifecycle, membership changes.
  - Filter by actor, target, action, date.

## E12. Payments Readiness (Phase 4)
- **Route:** `/platform/odemeler` (suggested)
- **Status:** 🔴 needs build (Phase 4, default off)
- **Functions:**
  - Read-only payment provider readiness (`PlatformAdminWithStepUp`).
  - No card/PAN/CVV/raw payload; hosted checkout config; webhook idempotency status (event id + payload hash).

## E13. Integrations Readiness (Phase 5)
- **Route:** `/platform/entegrasyonlar` (suggested)
- **Status:** 🔴 needs build (Phase 5, default off)
- **Functions:**
  - API key / webhook signing secret management (prefix/hash proof only; one-time plaintext on create).
  - Read-only readiness (`PlatformAdminWithStepUp`).

---

# F. CROSS-CUTTING / SHARED COMPONENTS (not pages, but required)

- **Business app shell**: sidebar nav (Dashboard, Calendar, Requests, Appointments, Branches, Staff, Skills, Services, Resource Types, Resources, Working Hours, Reviews, Messaging, Settings, Abuse Reports), branch scope selector, user menu.
- **Platform app shell**: sidebar nav (Dashboard, Abuse, Appeals, Tenants, Closure Cases, Notifications, Operations, Audit, Payments, Integrations), step-up status indicator.
- **Customer app shell**: top nav (Overview, Requests, Appointments, Reviews, Appeals, Profile), active sanction banner.
- **Public header/footer**: nav links, login/register, language (TR).
- **404 / 403 / 500 error pages**: tenant out-of-scope → 404, insufficient role → 403.
- **Loading / empty / error states** per page.
- **Toast / notification system** (idempotency, rate-limit feedback).
- **Masked PII renderer** component.

---

# G. SUMMARY: Page Count by Status

| Surface | Total pages | ✅ Exists | 🟡 Placeholder | 🔴 Needs build |
|---|---|---|---|---|
| Public | ~5 | 3 | 0 | 2 |
| Auth | ~7 | 6 | 0 | 1 |
| Customer | ~7 | 4 | 1 | 2 |
| Business Panel | ~16 | 9 | 0 | 7 |
| Platform Admin | ~13 | 7 | 2 | 4 |
| **Total** | **~48** | **29** | **3** | **16** |

> **Priority for UI build-out (MVP-critical, build first):**
> 1. Public Booking Flow (A4) — core revenue path.
> 2. Business Appointment Requests approve/decline (D2) — business onay akışı.
> 3. Business Calendar (D3) + Appointment operations (D4) — daily ops.
> 4. Customer Dashboard (C1) + Appointment Detail (C4) — self-service.
> 5. Business Reviews (D13) + Customer Reviews (C5) — feedback loop.
> 6. MFA/Step-up (B6) — security gate for platform admin.
> 7. Platform closure/notifications/operations (E8–E10) — compliance.

---

*This document is the canonical page reference for UI generation. Update `docs/06-karar-kaydi.md` (ADR) if route structure changes materially.*