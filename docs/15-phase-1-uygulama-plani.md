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

## Dilim 1 - Identity, Tenant ve Organization Temeli

- Platform-global `User`
- Tenant-scoped `Tenant`, `TenantMembership`, `Business`, `Branch`
- Tenant context çözümleme ve explicit background scope kontratı
- Rol/scope başlangıç modeli
- Audit log omurgası
- Tenant izolasyon entegrasyon testleri

## Dilim 2 - Catalog, Resources ve Availability

- `StaffMember`, `Skill`, `ResourceType`, `Resource`
- `Service`, `ServiceVariant`
- Çalışma saatleri, izin ve resource kullanım dışı zamanları
- UTC + branch timezone kuralları
- Uygunluk sorgusu için ilk read model

## Dilim 3 - Booking Request ve Approval

- `AppointmentRequest`, satırlar ve snapshot alanları
- `PendingApproval`, `Declined`, `Expired`, `Superseded`
- `responseBuffer` ve TTL expiry job
- `Appointment`, satırlar ve `Confirmed` akışı
- Staff ve resource için ayrı PostgreSQL exclusion constraint
- Transactional onay ve yarış koşulu entegrasyon testleri

## Dilim 4 - MVP Güvenlik Minimumları

- Booking request rate limit ve kullanıcı limitleri
- E-posta bildirim kuyruğu
- Abuse event, strike başlangıcı ve işletme spam işaretleme
- PII masking ve audit doğrulaması
- Healthcheck, structured logging ve correlation id

## Her Dilimde Kapanış

- `dotnet build RezSaaS.slnx --no-restore`
- `dotnet test RezSaaS.slnx --no-build`
- İlgili ADR/domain/yetki/veri envanteri güncellemeleri
- Migration ve rollback/forward-fix notu
- Tenant isolation, authz, audit, idempotency ve abuse etkisi incelemesi
