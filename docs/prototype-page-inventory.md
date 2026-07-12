# RezSaaS — UI Prototype Page Inventory & Implementation Map

> **Source:** Vite/React prototype at `C:\Users\Bilal\Downloads\rezsaas` (5 surface components + `SimulatorShell` + `SaaSContext`).
> **Target:** Next.js App Router web app at `src/Apps/RezSaaS.Web/src/app/`.
> **Purpose:** The exact page list, routes, and functions the prototype defines — ready to hand to an AI to rebuild each page in the real Next.js app.

---

## How the Prototype Is Organized

- **Routing:** A single `activeRoute` string (no React Router). `App.tsx` dispatches by prefix to one of 5 surface components: `PublicSurface`, `AuthSurface`, `CustomerSurface`, `BusinessSurface`, `PlatformSurface`.
- **State:** `SaaSContext` holds mock data (businesses, branches, staff, skills, services, resourceTypes, resources, requests, reviews, strikes, sanctions, appeals, tenants, members, closureCases, notifications, auditLog) and mutating actions (`approveRequest`, `declineRequest`, `completeAppointment`, `markNoShow`, `reportAbuse`, `replyToReview`, `saveBranch/Staff/Skill/Service/ResourceType/Resource/BusinessSettings`, etc.).
- **Data model:** `src/types.ts` (UserAccount, Business, Branch, WeeklySchedule, Skill, Staff, ServiceVariant, Service, ResourceType, Resource, AppointmentStatus, AppointmentRequest, Review, Strike, Sanction, Appeal, Tenant, TenantMember, ClosureCase, PlatformNotification, AuditLogEntry).

---

# A. PUBLIC SURFACE — `PublicSurface.tsx`

Persistent header navbar on all public pages: Logo → `/`; "İşletmeleri Keşfet" → `/kesfet`; "Giriş Yap" → `/giris`; "Kayıt Ol" → `/kayit`.

| # | Prototype Route | Page | Target Web Route | Key Functions |
|---|---|---|---|---|
| A1 | `/` | Landing / Home | `/` | Hero (badge "Modern Rezervasyon Altyapısı", gradient headline, 2 CTAs); "Popüler Kategoriler" chip grid (Spa & Masaj, Berber, Nail Art, Dövme); 3-step "Nasıl Çalışır" explainer. Click category → `/kesfet?category=...`. |
| A2 | `/kesfet` | Discover / Browse | `/kesfet` | Business directory cards (logo, name, category, rating, city, address). Category filter chips, search input, sort. Card click → `/isletme/[slug]`. Empty + loading states. |
| A3 | `/isletme/[slug]` | Business Public Profile | `/isletme/[slug]` | Business header (name, category badge, rating, address, phone); image gallery; services list with variants (price/duration) + "Rezervasyon Yap" → `/isletme/[slug]/rezervasyon`; staff display; published reviews list + reply display; business hours. |
| A4 | `/isletme/[slug]/rezervasyon` | Booking Flow (multi-step) | `/isletme/[slug]/rezervasyon` | Step 1: select service(s) + variant(s), total duration/price. Step 2: optional staff preference (skill-matched) or "any available". Step 3: date picker + bookable slot grid (branch timezone). Step 4: review summary + notes + consent → submit creates `AppointmentRequest` (PendingApproval). Step 5: confirmation screen with request id → link to `/hesabim/talepler`. **Auth-gated** if not logged in. No resource shown to customer. |
| A5 | `/isletme/[slug]/degerlendirmeler` | Public Reviews List | `/isletme/[slug]/degerlendirmeler` | Full paginated published reviews (rating, text, customer name, date, business reply). Filter by rating. |

---

# B. AUTH SURFACE — `AuthSurface.tsx`

| # | Prototype Route | Page | Target Web Route | Key Functions |
|---|---|---|---|---|
| B1 | `/giris` | Login | `/giris` | Email + password form; "Şifremi unuttum" → `/sifremi-unuttum`; "Hesabın yok mu? Kayıt ol" → `/kayit`; role-switcher (simulator: pick role to impersonate); on submit → `/gelis`. |
| B2 | `/kayit` | Register (Customer) | `/kayit` | Name, email, phone, password, consent; creates `UserAccount` (Customer); → `/eposta-dogrula` or `/gelis`. |
| B3 | `/sifremi-unuttum` | Forgot Password | `/sifremi-unuttum` | Email input → "sıfırlama bağlantısı gönderildi" generic message; → `/sifre-sifirla`. |
| B4 | `/sifre-sifirla` | Reset Password | `/sifre-sifirla` | New password + confirm; token-based; → `/giris`. |
| B5 | `/eposta-dogrula` | Email Confirmation | `/eposta-dogrula` | Landing from email link: success / already-confirmed / expired states; → `/giris`. |
| B6 | `/platform/adim` | MFA / Step-up | `/adim` or `/platform/adim` | TOTP 6-digit code entry; recovery code fallback; verifies `mfaStepUpVerified` in context; → `/platform`. |
| B7 | `/gelis` | Role Dispatch | `/gelis` | Auto-redirect by role: PlatformAdmin/Support → `/platform`; BusinessOwner/Manager/Staff → `/panel`; Customer → `/hesabim`. |

---

# C. CUSTOMER SURFACE — `CustomerSurface.tsx`

**Shell:** Header (avatar, name, email) + tab bar: Özet · Taleplerim (badge) · Randevularım · Yorumlarım · Cezalar & İtirazlar · Profil. Conditional active-sanction banner with "İtiraz Et" → `/hesabim/itirazlar`.

| # | Prototype Route | Page | Target Web Route | Key Functions |
|---|---|---|---|---|
| C1 | `/hesabim` | Dashboard / Overview | `/hesabim` | "Yaklaşan Randevularınız" (up to 3 confirmed, "Detay" → `/hesabim/randevular/detay`); action cards ("Hemen Randevu Alın" → `/kesfet`, "Onay Bekleyen Talepler" → `/hesabim/talepler`); "Hesap Özeti" sidebar (strike points, confirmed count, reviews count). |
| C2 | `/hesabim/talepler` | My Requests | `/hesabim/talepler` | List of customer's `AppointmentRequest` with status badges (PendingApproval, Confirmed, Declined, Expired, Superseded, CancelledByCustomer). Detail view with services, time, branch, status. Cancel own pending request. 24h TTL countdown. |
| C3 | `/hesabim/randevular` | My Appointments | `/hesabim/randevular` | Confirmed appointments list (upcoming/past/completed/cancelled/no-show). Tabs by status. "Detay" → `/hesabim/randevular/detay`. Cancel upcoming. "Yorum Yap" for completed → `/hesabim/yorumlar`. |
| C4 | `/hesabim/randevular/detay` | Appointment Detail | `/hesabim/randevular/[id]` | Full details: services snapshot, price, staff, branch, time; cancellation action; review action; status history. |
| C5 | `/hesabim/yorumlar` | My Reviews | `/hesabim/degerlendirmeler` | Reviews written by customer. Write/edit review (rating, text) for completed appointment. Status badges (Published/PendingModeration/Hidden). |
| C6 | `/hesabim/itirazlar` | Appeals & Sanctions | `/hesabim/itirazlar` | List of strikes, active sanctions, closure cases. Status (Pending/Approved/Rejected). Create appeal (reason + evidence) for own strike/sanction. |
| C7 | `/hesabim/profil` | Profile / Settings | `/hesabim/profil` | Edit name, email, phone; change password; verification status; account status (Active/Banned); account closure request. |

---

# D. BUSINESS PANEL — `BusinessSurface.tsx`

**Shell:** Sidebar (amber theme, dark slate) with branch scope switcher dropdown. Groups: **Ana** (Özet, Onay Bekleyenler [badge], Rezervasyon Takvimi, Tüm Randevular) · **Sistem Ayarları** (Şubeler, Personel, Yetenekler, Hizmetler, Kaynak Türleri, Kaynaklar, Çalışma Saatleri) · **Müşteri İlişkileri** (Yorumlar, Bildirim Şablonları, Profil Ayarları, Abuse Raporları).

| # | Prototype Route | Page | Target Web Route | Key Functions |
|---|---|---|---|---|
| D1 | `/panel` | Dashboard (Özet Panel) | `/panel` | 4 KPI cards (Bugünkü Ciro, Doluluk Oranı, Bekleyen Talepler, No-Show Oranı); "Bugünkü Randevular" list (up to 5, "Yönet" → `/panel/randevular/islem`); link to calendar. |
| D2 | `/panel/talepler` | Requests Approve/Decline | `/panel/talepler` | Queue of `PendingApproval` requests (masked phone). Each: customer name, services, time, total price, notes. **Onayla** (approveRequest → Confirmed), **Reddet** (declineRequest with reason), **Abuse report** (modal with reason textarea → reportAbuse). 24h TTL notice. |
| D3 | `/panel/takvim` | Reservation Calendar | `/panel/takvim` | Day view: hourly grid (09:00–18:00) with confirmed appointments placed by start hour. Timezone label. Click appointment "İşlem Yap" → `/panel/randevular/islem`. Resource/staff lanes. |
| D4 | `/panel/randevular/islem` | Appointment Operations | `/panel/randevular/[id]` | Selected appointment detail (customer, services, time, total). Operations (status-gated): **Tamamlandı** (completeAppointment), **No-Show** (markNoShow). Only when Confirmed. Back to list. |
| D5 | `/panel/randevular` | All Appointments List | `/panel/randevular` | History of all non-pending appointments with status badges. |
| D6 | `/panel/subeler` | Branches | `/panel/subeler` | Branch cards (name, slotInterval, address, timezone, phone, workingHours summary). "Yeni Şube Ekle" modal (name, address, phone, slotInterval, maxPublicSlots). |
| D7 | `/panel/personel` | Staff Members | `/panel/personel` | Staff cards (photo, name, bio, skill IDs, branch IDs). "Yeni Çalışan Ekle" modal (name, bio, default skill, branch). |
| D8 | `/panel/yetenekler` | Skills Catalog | `/panel/yetenekler` | Skill list (name, id, description). "Yeni Yetenek Ekle" modal (name, description). |
| D9 | `/panel/hizmetler` | Services Catalog | `/panel/hizmetler` | Service cards (image, name, description, requiredResourceType, variants with price/duration). "Yeni Hizmet Ekle" modal (name, description, price, duration, variant). |
| D10 | `/panel/kaynak-turleri` | Resource Types | `/panel/kaynak-turleri` | Resource type list (name, description). Shared view with resources. |
| D11 | `/panel/kaynaklar` | Resources (Capacity) | `/panel/kaynaklar` | Resource cards per branch (name, capacity, blocked status). Toggle block/unblock (saveResource). "Yeni Kaynak Ekle" modal (name, capacity, type). |
| D12 | `/panel/calisma-saatleri` | Working Hours | `/panel/calisma-saatleri` | Weekly schedule for active branch (day, isOpen, start–end). Staff availability rules. |
| D13 | `/panel/degerlendirmeler` | Reviews Management | `/panel/degerlendirmeler` | Incoming reviews list (rating, text, customer). "Müşteriye Yanıt Yaz" modal → replyToReview (replyText). Reply status shown. |
| D14 | `/panel/mesajlar` | Notification Templates | `/panel/mesajlar` | Read-only templates (confirmation, rejection notifications). Channel toggles. |
| D15 | `/panel/ayarlar` | Business Profile Settings | `/panel/ayarlar` | Form: business name, description, phone. "Bilgileri Güncelle" → saveBusinessSettings. Gallery management. |
| D16 | `/panel/abuse-raporlari` | Abuse Reports | `/panel/abuse-raporlari` | List of abuse reports submitted by this business (per request). Idempotency + daily-limit note. Status (submitted → reviewed). |

---

# E. PLATFORM ADMIN — `PlatformSurface.tsx`

**Shell:** Dark slate sidebar. **Gate:** MFA step-up — if `!mfaStepUpVerified`, renders "Yüksek Güvenlik Protokolü" card → `/platform/adim`.

| # | Prototype Route | Page | Target Web Route | Key Functions |
|---|---|---|---|---|
| E1 | `/platform` | System Summary Dashboard | `/platform` | 4 stat cards (active tenants, total reservations, active sanctions, system health %). Last 5 audit log entries preview → "Tüm Günlüğü Gör" → `/platform/denetim-gunlugu`. |
| E2 | `/platform/kiracilar` | Tenant Management | `/platform/tenantlar` | List of businesses/tenants with status badge, id, slug, phone. **Platformu Kilitle / Kilidi Aç** (toggleBusinessLock) per tenant. |
| E3 | `/platform/abonelikler` | Subscriptions & Billing | `/platform/abonelikler` | Subscription/billing per tenant (plan, status, invoice). |
| E4 | `/platform/kimlikler` | Identities & Ban Panel | `/platform/kimlikler` | User account list (id, name, email, phone, role, status). Ban/unban actions. Masked PII. |
| E5 | `/platform/cezalar` | Sanctions & Strike Engine | `/platform/cezalar` | Strikes + sanctions management. Apply/revoke sanctions (Warning, Cooldown ≤24h, TempBan 24–72h). Risk level by active strike count. One active blocking sanction rule. |
| E6 | `/platform/itirazlar` | Appeals Pool | `/platform/itirazlar` | Appeal review queue (strikes, sanctions, closure cases). Approve/reject with notes. |
| E7 | `/platform/veritabani` | Database Monitor (JSON) | `/platform/veritabani` | Raw JSON dump of context state (tenants, members, closureCases, notifications, auditLog) — diagnostic view. |
| E8 | `/platform/denetim-gunlugu` | Audit Log | `/platform/denetim-gunlugu` | Append-only audit entries (actor, action, target, details, timestamp). Filter. |
| E9 | `/platform/destek` | Support & Infrastructure | `/platform/destek` | Health checks, reconciliation, support tools. |

> **Note:** `/platform/ayarlar` (`isSettings`) is declared but has no sidebar button or render block — likely a stub.

---

# F. ROUTE MAPPING: Prototype → Existing Web App

The existing Next.js app (`routes.ts`) already has some of these. **Differences:**

| Prototype Route | Existing Web Route | Status | Action |
|---|---|---|---|
| `/panel` | `/panel` | ✅ exists | rebuild UI |
| `/panel/talepler` | — (not in routes.ts) | 🔴 new | **add to routes.ts + create page** |
| `/panel/takvim` | — | 🔴 new | **add + create** |
| `/panel/randevular/islem` | — | 🔴 new | **add + create** |
| `/panel/randevular` | — | 🔴 new | **add + create** |
| `/panel/subeler` | `/panel/subeler` | ✅ exists | rebuild |
| `/panel/personel` | `/panel/personel` | ✅ exists | rebuild |
| `/panel/yetenekler` | `/panel/yetenekler` | ✅ exists | rebuild |
| `/panel/hizmetler` | `/panel/hizmetler` | ✅ exists | rebuild |
| `/panel/kaynak-turleri` | `/panel/kaynak-turleri` | ✅ exists | rebuild |
| `/panel/kaynaklar` | `/panel/kaynaklar` | ✅ exists | rebuild |
| `/panel/calisma-saatleri` | `/panel/calisma-saatleri` | ✅ exists | rebuild |
| `/panel/degerlendirmeler` | — | 🔴 new | **add + create** |
| `/panel/mesajlar` | — | 🔴 new | **add + create** |
| `/panel/ayarlar` | `/panel/ayarlar` | ✅ exists | rebuild |
| `/panel/abuse-raporlari` | — | 🔴 new | **add + create** |
| `/platform/kiracilar` | `/platform/tenantlar` | ✅ (different name) | **decide canonical name** |
| `/platform/kimlikler` | — | 🔴 new | identity/ban panel |
| `/platform/cezalar` | `/platform/abuse` (partial) | 🟡 partial | sanctions engine |
| `/platform/itirazlar` | `/platform/itirazlar` | ✅ exists | rebuild |
| `/platform/denetim-gunlugu` | — | 🔴 new | audit log |
| `/platform/abonelikler` | — | 🔴 new | subscriptions (Phase 4) |
| `/platform/veritabani` | — | 🔴 new | diagnostic |
| `/platform/destek` | — | 🔴 new | support/health |
| `/hesabim` | — | 🔴 new | customer dashboard |
| `/hesabim/randevular/detay` | — | 🔴 new | appointment detail |
| `/hesabim/yorumlar` | — | 🔴 new | customer reviews |
| `/isletme/[slug]/rezervasyon` | — | 🔴 new | booking flow |
| `/isletme/[slug]/degerlendirmeler` | — | 🔴 new | public reviews |
| `/eposta-dogrula` | — | 🔴 new | email confirm |
| `/adim` (MFA) | — | 🔴 new | step-up |

---

# G. SUMMARY

**Total prototype pages: 38** across 5 surfaces:
- Public: **5** (landing, discover, profile, booking flow, reviews)
- Auth: **7** (login, register, forgot, reset, email-verify, MFA, dispatch)
- Customer: **7** (dashboard, requests, appointments, detail, reviews, appeals, profile)
- Business: **16** (dashboard, requests, calendar, app-ops, app-list, branches, staff, skills, services, resource-types, resources, hours, reviews, templates, settings, abuse-reports)
- Platform: **9** (dashboard, tenants, subscriptions, identities/bans, sanctions, appeals, db-monitor, audit, support)

**New pages to create in Next.js app: ~15** · **Existing pages to rebuild: ~23**

**Next step:** Hand this doc + the prototype component for each page to the AI, one page at a time, to rebuild into Next.js App Router pages wired to the real API.