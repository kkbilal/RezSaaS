# Backend Finalization Roadmap — RezSaaS MVP Launch

> Bu doküman, MVP lansmanı (ilk 10-15 müşteri) için backend modüllerinin mevcut durumunu analiz eder ve kalan kritik işleri önceliklendirir.

## 📊 Mevcut Durum Analizi (21 Haziran 2026)

| Modül | Durum | MVP için Gerekli | Not |
|-------|-------|------------------|-----|
| Identity | ✅ Tam | ✅ | UserAccount, MFA, lockout, cookie auth |
| TenantManagement | ✅ Tam | ✅ | Tenant lifecycle, membership, suspend/close |
| Organization | ✅ Tam | ✅ | Business, Branch, StaffMember, Skills, Profile metadata, Gallery |
| Catalog | ✅ Tam | ✅ | Service, ServiceVariant, RequiredSkill |
| Resources | ✅ Tam | ✅ | Resource (chair/room/bed), ResourceType |
| Availability | ✅ Tam | ✅ | Slot hesaplama, working hours, blocks |
| Booking | ✅ Tam | ✅ | AppointmentRequest, Appointment, approve/decline/cancel/complete/no-show/rebook |
| Admin | ✅ Tam | ✅ | Abuse control plane, sanctions, appeals, closure |
| Messaging | ⚠️ %80 | ✅ | Platform outbox mature; tenant TransactionalMessage basit |
| Analytics | ⚠️ %70 | ⚠️ | Read models mevcut; dashboard endpoint + otomatik toplayıcı eksik |
| **Reviews** | ❌ **STUB** | ⚠️ | **Sadece boş ReviewsModule.cs — hiçbir implementation yok** |
| Payments | ⚠️ %30 | ❌ | Phase 4 — MVP için kapalı kalabilir (hosted checkout) |
| Integrations | ⚠️ %20 | ❌ | Phase 5 — MVP için kapalı kalabilir |

## 🎯 MVP Backend Finalization Öncelikleri

### Öncelik 1: KRİTİK — Reviews Modülü (Backend Core)

Reviews modülü tamamen boş. Frontend'de `BusinessReviews` component hazır. Backend implementation gerekli.

**Gereksinimler:**
- `Review` aggregate (tenant-scoped)
- `ReviewStatus` enum (Pending, Published, Rejected)
- Sadece tamamlanmış (`Completed`) appointment sonrası review yazılabilir
- Rating: 1-5 yıldız
- Public read endpoint (business slug ile)
- Business moderation endpoint (onayla/reddet)
- `Business.UpdateRatingSummary()` entegrasyonu (Organization modülünde zaten hazır)
- Tenant izolasyonu, query filter, audit

**Etki:** `docs/05-domain-sozlugu.md`, `docs/07-yetki-matrisi.md`, `docs/11-veri-envanteri-taslagi.md` güncellemesi.

---

### Öncelik 2: YÜKSEK — Analytics Dashboard Endpoint

Analytics read models mevcut (`DailyBusinessMetrics`, `TopServiceMetrics`, `ResourceCapacityMetrics`) ama:
- Business dashboard verisi çeken endpoint eksik olabilir
- Metriklerin otomatik toplanması (background projection) doğrulanmalı

**Gereksinimler:**
- `GET /api/business/analytics/summary` — tarih aralığı, branch opsiyonel
- Confirmed appointment, no-show, cancellation, revenue projection
- Top services, top staff, resource utilization
- Read-only, tenant-scoped, `BusinessOwner` veya `BranchManager` authz

---

### Öncelik 3: ORTA — Booking Notification Wiring Doğrulaması

Booking modülü complete ama Messaging entegrasyonu doğrulanmalı:
- `AppointmentRequest` create → müşteriye + işletmeye bildirim
- `Approved` → müşteriye onay bildirimi
- `Declined` → müşteriye ret bildirimi
- `Completed` → müşteriye review davet bildirimi (Reviews modülü完成后)
- `NoShow` / cancellation → ilgili taraflara bildirim

**Gereksinimler:**
- Transactional outbox'a mesaj yazıldığını doğrula
- Email template'lerinin mevcut olduğunu doğrula
- Development sink'in çalıştığını doğrula

---

### Öncelik 4: DÜŞÜK — Production Hardening

Lansman öncesi production gate'ler:

1. **Email Delivery**: `Identity:DeliveryMode=Smtp` + provider secret olmadan production başlamaz
2. **Rate Limiting**: login, register, password reset, booking request create — hepsi protected
3. **Background Jobs**: 
   - `PendingApproval` expiry worker
   - Platform notification delivery worker
   - Analytics projection worker (varsa)
4. **Backup/Restore**: `scripts/Backup-Postgres.ps1` test edildi
5. **Health Checks**: DB, SMTP readiness
6. **Swagger**: Production'da kapalı
7. **CSRF/Origin**: State-changing endpoint'ler için strateji

---

## 🏗️ Reviews Modülü Implementation Planı

### Domain Layer

```
Domain/
├── Review.cs                    # Aggregate root
├── ReviewStatus.cs              # enum: Pending, Published, Rejected
└── ReviewModerationReason.cs    # value object (optional)
```

**Review Aggregate Kuralları:**
- Tenant-scoped (`TenantId` zorunlu)
- Bir müşteri bir appointment için sadece 1 review yazabilir (unique constraint)
- Rating: 1-5 (invariant)
- Sadece `Appointment.Status == Completed` ise review yazılabilir
- `ReviewStatus`: müşteri yazınca `Pending`, business onaylayınca `Published`
- Published review `Business.RatingAverage` ve `ReviewCount` günceller

### Application Layer

```
Application/
├── CreateReviewCommand.cs
├── CreateReviewService.cs
├── ModerateReviewService.cs        # publish/reject
├── PublicReviewQueryService.cs     # public read (business slug)
├── BusinessReviewQueryService.cs   # business panel list
├── ReviewView.cs
└── PublicReviewSummaryView.cs      # rating + count + reviews
```

### Infrastructure Layer

```
Infrastructure/
└── Persistence/
    ├── ReviewsDbContext.cs
    └── Migrations/
```

### API Endpoints (host)

```
Customer:
  POST /api/customer/reviews              # create review (auth required)
  GET  /api/customer/reviews/mine         # my reviews

Public:
  GET /api/public/businesses/{slug}/reviews  # public published reviews

Business:
  GET    /api/business/reviews               # list (Pending + Published)
  POST   /api/business/reviews/{id}/publish  # approve
  POST   /api/business/reviews/{id}/reject   # reject
```

### Cross-Module Contract

Reviews modülü Organization modülüne yazamaz. Sadece:
- `Business.UpdateRatingSummary()` için bir **domain event** veya **integration contract** gerekir
- Veya BuildingBlocks'ta `IBusinessRatingUpdater` interface tanımlanıp Organization implementation verir

**Önerilen yaklaşım:** `IBusinessRatingSummarySink` contract BuildingBlocks'ta, implementation Organization'da. Reviews application service recompute sonrası sink'e yazar.

---

## ✅ Lansman Hazırlık Checklist

### Backend
- [ ] Reviews modülü implement edildi
- [ ] Reviews → Organization rating summary sync çalışıyor
- [ ] Analytics dashboard endpoint test edildi
- [ ] Booking → Messaging notification wiring doğrulandı
- [ ] Tüm background job'lar çalışıyor (expiry, notification delivery)
- [ ] Production email provider config hazır
- [ ] Rate limiting tüm kritik endpoint'lerde aktif
- [ ] DB backup prosedürü test edildi
- [ ] Health check endpoint'leri doğru

### Integration Test
- [ ] Full booking flow: request → approve → confirmed → complete → review
- [ ] Tenant izolasyon testleri
- [ ] Double-booking prevention testleri
- [ ] TTL expiry testi
- [ ] Abuse flow testi

### Frontend ↔ Backend
- [ ] Reviews frontend component API'ye bağlı
- [ ] Analytics dashboard API'ye bağlı
- [ ] Tüm formlar validation + hata durumu gösteriyor
- [ ] Mobile responsive kontrol

---

## 📅 Önerilen Sıra

1. **Reviews modülü** (2-3 gün) — frontend hazır, backend critical
2. **Analytics dashboard** doğrulama (1 gün)
3. **Notification wiring** doğrulama (1 gün)
4. **Integration testler** (1-2 gün)
5. **Production hardening** (1 gün)
6. **Soft launch** (dahili test)
7. **Public launch** (10-15 müşteri)