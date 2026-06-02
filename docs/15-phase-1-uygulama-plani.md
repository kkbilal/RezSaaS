# Phase 1 Uygulama Planı

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
- Açık: production SMTP sağlayıcısı seçimi, secret yükleme ve uçtan uca confirmation/password reset testi
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
- Açık: public/admin endpoint tasarımı; endpoint açılırken authz, rate limit, audit ve idempotency eklenecek

## Dilim 2 - Catalog, Resources ve Availability

Durum: backend domain/persistence temeli tamamlandı.

- Tamamlandı: `Service`, `ServiceVariant`, `ServiceRequiredSkill`
- Tamamlandı: `ResourceType`, `Resource`, `ResourceBlock`
- Tamamlandı: `BranchWorkingHours`, `StaffUnavailableTime`
- Tamamlandı: tenant-scoped EF query filter ve seed'siz migration
- Açık: uygunluk sorgusu read model'i ve endpoint yüzeyi

## Dilim 3 - Booking Request ve Approval

Durum: booking çekirdeği backend seviyesinde tamamlandı; approval endpoint/job yüzeyi sonraki dikey dilimde açılacak.

- Tamamlandı: `AppointmentRequest`, `AppointmentRequestLine`, `Appointment`, `AppointmentLine`
- Tamamlandı: `PendingApproval`, `Declined`, `Expired`, `Superseded`, `CancelledByCustomer`
- Tamamlandı: `responseBuffer` ve 24 saat TTL hesabı domain seviyesinde
- Tamamlandı: snapshot hizmet adı, süre, fiyat ve para birimi alanları
- Tamamlandı: staff ve resource için ayrı PostgreSQL exclusion constraint
- Tamamlandı: pending request'in slot bloklamadığını ve confirmed overlap'in DB'de engellendiğini doğrulayan entegrasyon testleri
- Açık: transactional onay application service'i, TTL expiry background job ve `Superseded` kapatma akışı

## Dilim 4 - MVP Güvenlik Minimumları

Durum: güvenlik/operasyon omurgası tamamlandı; endpoint bazlı limitler endpoint açıldıkça eklenecek.

- Tamamlandı: auth rate limit ve Identity lockout
- Tamamlandı: transactional messaging queue modeli (`TransactionalMessage`)
- Tamamlandı: admin abuse/audit/sanction başlangıç modeli
- Tamamlandı: PII masking helper ve test
- Tamamlandı: healthcheck ve correlation id middleware
- Açık: booking request endpoint'i açıldığında kullanıcı/tenant bazlı request limitleri ve abuse event üretimi

## Her Dilimde Kapanış

- `dotnet build RezSaaS.sln --no-restore`
- `dotnet test RezSaaS.sln --no-build`
- İlgili ADR/domain/yetki/veri envanteri güncellemeleri
- Migration ve rollback/forward-fix notu
- Tenant isolation, authz, audit, idempotency ve abuse etkisi incelemesi
