# Phase 1 Uygulama Planı

> ⚠️ **SUPERSEDED (ADR-068, 2026-06-20):** Bu doküman artık güncel uygulama
> planı değildir. Phase 1 içeriği `roadmap/phase-1-cekirdek-saas.md` ve ilgili
> ADR'ler (özellikle ADR-016/018/019/020/023/024) tarafından kapsanır. Bu dosya
> yalnızca geçmiş referans için korunur; yeni çalışma `docs/roadmap/` altında
> yürütülür.

Bu plan Phase 1'i küçük, doğrulanabilir dikey dilimlere böler. Her dilim build, test, doküman ve güvenlik kontrolüyle kapanır.

## Dilim 0 - Platform İskeleti

Durum: tamamlandı.

- `.NET 10.0.300` SDK sabitleme
- API composition root
- `BuildingBlocks` modül kontratı
- Domain başına ayrı modül assembly'si
- Merkezi analyzer ve NuGet sürüm yönetimi
- Modüller arası doğrudan assembly bağımlılığını engelleyen mimari test
- PostgreSQL 18.4 local compose

## Dilim 1A - Identity/Auth Kapısı

Durum: backend güvenlik kapısı tamamlandı; gerçek sağlayıcı ve UX/onboarding kararları sonraki teslimat kapısında doğrulanacak.

- Tamamlandı: ASP.NET Core Identity + PostgreSQL store
- Tamamlandı: register, cookie/bearer login, refresh ve manage endpoint yüzeyi
- Tamamlandı: platform-global `UserAccount` ve hesap durumu (`Active`, `Suspended`, `Closed`)
- Tamamlandı: global `PlatformAdmin`, `PlatformSupport` policy kontratları; migration rol seed'i kullanılmaz
- Tamamlandı: IP bazlı auth rate limit (`10/dakika`, `429`) ve Identity lockout
- Tamamlandı: production confirmed e-posta fail-fast; local token loglamayan sink
- Tamamlandı: production için `Smtp` delivery mode konfigürasyonu
- Tamamlandı: `PlatformAdminWithStepUp` policy; admin aksiyonları MFA claim'i ister
- Tamamlandı: ilk `PlatformAdmin` için token-hash kontrollü, auditli bootstrap servisi; rol/user migration seed'i yok
- Tamamlandı: identity audit log migration'ı
- Tamamlandı: migration ve gerçek PostgreSQL entegrasyon testleri
- Açık: production SMTP sağlayıcısı seçimi, secret yükleme ve uçtan uca confirmation/password reset testi; SMS sağlayıcı seçimi maliyet nedeniyle sonraki faza bırakıldı
- Açık: ayrıcalıklı hesap MFA enrollment ekranı ve güvenilir cihaz/oturum UX'i

Admin/işletme yönetim endpoint'leri bu step-up policy'yi kullanmadan yayınlanmaz.

## Dilim 1B - Tenant ve Organization Temeli

Durum: backend domain/persistence temeli tamamlandı; yönetim endpoint'leri yayınlanmadı.

- Tamamlandı: `Tenant`, `TenantMembership`, `TenantStatus`, `TenantAuditLogEntry` domain modeli
- Tamamlandı: ayrı `tenant_management` PostgreSQL schema'sı ve EF Core migration
- Tamamlandı: tenant slug benzersizliği, membership benzersizliği ve `BusinessOwner` branch scope engeli
- Tamamlandı: audit log omurgası için append-only veri modeli başlangıcı
- Tamamlandı: migration'ın tenant/üyelik/audit seed verisi üretmediğini doğrulayan entegrasyon testi
- Tamamlandı: request-scope tenant context accessor ve `X-RezSaaS-Tenant` parse middleware'i
- Tamamlandı: `Business`, `Branch`, `StaffMember`, `Skill`, `StaffSkill` organization modeli
- Tamamlandı: branch timezone alanı ve tenant-scoped global query filter kalıbı
- Tamamlandı: tenant izolasyon entegrasyon testi
- Tamamlandı: `TenantManagementDbContext` için platform registry istisnası dokümante edildi; erişimler explicit tenant scope ile açılacak
- Açık: public/admin endpoint tasarımı; endpoint açılırken authz, rate limit, audit ve idempotency eklenecek

## Dilim 2 - Catalog, Resources ve Availability

Durum: backend domain/persistence ve tenant-scoped read model temeli tamamlandı; endpoint yüzeyi sonraki dikey dilimde açılacak.

- Tamamlandı: `Service`, `ServiceVariant`, `ServiceRequiredSkill`
- Tamamlandı: `ResourceType`, `Resource`, `ResourceBlock`
- Tamamlandı: `BranchWorkingHours`, `StaffUnavailableTime`
- Tamamlandı: tenant-scoped EF query filter ve seed'siz migration
- Tamamlandı: `AvailabilityQueryService` ile branch çalışma saatleri ve staff unavailable snapshot read model'i
- Açık: public/admin availability endpoint yüzeyi, authz ve rate limit bağlanması

## Dilim 3 - Booking Request ve Approval

Durum: booking çekirdeği ve application use-case omurgası backend seviyesinde tamamlandı; endpoint/scheduler yüzeyi sonraki dikey dilimde açılacak.

- Tamamlandı: `AppointmentRequest`, `AppointmentRequestLine`, `Appointment`, `AppointmentLine`
- Tamamlandı: `PendingApproval`, `Approved`, `Declined`, `Expired`, `Superseded`, `CancelledByCustomer`
- Tamamlandı: `responseBuffer` ve 24 saat TTL hesabı domain seviyesinde
- Tamamlandı: snapshot hizmet adı, süre, fiyat ve para birimi alanları
- Tamamlandı: staff ve resource için ayrı PostgreSQL exclusion constraint
- Tamamlandı: pending request'in slot bloklamadığını ve confirmed overlap'in DB'de engellendiğini doğrulayan entegrasyon testleri
- Tamamlandı: transactional onay application service'i, seçilen request'i `Approved` kapatma, confirmed appointment üretme ve çakışan pending request'leri `Superseded` yapma
- Tamamlandı: ret ve TTL expiry application service'leri; approval/decline audit kaydı ve onay transactional e-posta kuyruğu kontratı
- Açık: endpoint yüzeyi, business-owner/branch-manager authz ve per-tenant background scheduler bağlanması

## Dilim 4 - MVP Güvenlik Minimumları

Durum: güvenlik/operasyon omurgası tamamlandı; endpoint yayınlanırken mevcut policy ve use-case kontrolleri zorunlu bağlanacak.

- Tamamlandı: auth rate limit ve Identity lockout
- Tamamlandı: transactional messaging queue modeli (`TransactionalMessage`)
- Tamamlandı: admin abuse/audit/sanction başlangıç modeli
- Tamamlandı: PII masking helper ve test
- Tamamlandı: healthcheck ve correlation id middleware
- Tamamlandı: booking request endpoint'i için hazır rate-limit policy kontratı (`booking-appointment-requests`)
- Tamamlandı: kullanıcı başına eşzamanlı pending ve günlük request limitleri; limit aşımında Admin abuse event üretimi
- Tamamlandı: cross-module abuse, audit ve transactional messaging outbox kontratları `BuildingBlocks` altında teknik arayüz olarak tanımlandı
- Açık: abuse cooldown/risk score ve işletme spam işaretleme endpoint'i

## Her Dilimde Kapanış

- `dotnet build RezSaaS.sln --no-restore`
- `dotnet test RezSaaS.sln --no-build`
- İlgili ADR/domain/yetki/veri envanteri güncellemeleri
- Migration ve rollback/forward-fix notu
- Tenant isolation, authz, audit, idempotency ve abuse etkisi incelemesi
