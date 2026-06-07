# Tenant Management Temeli

## Amaç

Tenant Management modülü; RezSaaS içindeki tenant yaşam döngüsü, tenant üyeliği ve tenant yönetim audit omurgasının başlangıç noktasıdır.

Bu modül tenant yaşam döngüsü için domain/persistence temelini ve Phase 3 control-plane başlangıcını sağlar. Platform operasyon endpoint'leri `PlatformAdminWithStepUp` ile korunur; tenant self-service işletme yönetimi ise tenant membership authz tamamlanmadan yayınlanmaz.

## Uygulanan Temel

- `Tenant`: tenant izolasyon sınırı, slug, görünen ad, durum ve yaşam zamanı alanları
- `TenantMembership`: platform-global `UserAccount` ile tenant içi rol/scope ilişkisi
- `TenantMembershipRole`: `BusinessOwner`, `BranchManager`, `Staff`
- `TenantMembershipStatus`: `Active`, `Suspended`, `Revoked`
- `TenantAuditLogEntry`: tenant yönetim aksiyonları için append-only audit başlangıcı
- EF Core `TenantManagementDbContext`
- PostgreSQL `tenant_management` schema'sı
- `CreateTenantWithOwnerService`: `Tenant` + ilk `BusinessOwner` membership + audit kaydını tek komutta üretir
- `TenantControlPlaneQueryService`: platform admin için tenant liste/detay ve membership read modeli
- `AddTenantMembershipService`: tenant membership ekleme ve audit kaydı
- `ChangeTenantMembershipStatusService`: membership suspend/revoke, terminal `Revoked` durumu ve son aktif owner koruması
- `ChangeTenantLifecycleService`: auditli, neden zorunlu ve row-lock korumalı tenant suspend/reactivate/close state machine'i
- `POST /api/admin/tenants`: API composition root altında `PlatformAdminWithStepUp` korumalı tenant provisioning yüzeyi
- `GET /api/admin/tenants`, `GET /api/admin/tenants/{tenantId}` ve `GET /api/admin/tenants/{tenantId}/memberships`: platform control-plane okuma yüzeyleri
- `POST /api/admin/tenants/{tenantId}/suspend`, `/reactivate`, `/close`: platform control-plane tenant lifecycle yüzeyleri
- `POST /api/admin/tenants/{tenantId}/memberships`, `/suspend`, `/revoke`: platform control-plane membership yönetim yüzeyleri

## DB Kuralları

- `Tenants.NormalizedSlug` benzersizdir.
- `TenantMemberships(TenantId, UserAccountId)` benzersizdir.
- `BusinessOwner` branch scope alamaz.
- Audit detayları `jsonb` olarak saklanır.
- Migration tenant, üyelik, kullanıcı, rol veya operasyon verisi seed etmez.
- `TenantManagementDbContext` platform tenant registry'sidir; `Tenants` tablosu globaldir ve request-scope query filter kullanmaz.
- `TenantMembership` ve `TenantAuditLogEntry` erişimleri endpoint/application service açıldığında explicit `TenantId` parametresi, authz ve audit kontrolüyle yapılır; gizli tenant-context varsayımı kullanılmaz.
- Tenant lifecycle state machine PostgreSQL `FOR UPDATE` row lock kullanır; yarışan suspend/reactivate/close komutları son-yazan-kazan ile `Closed` durumunu geri alamaz.

## Yetki Sınırı

- `BusinessOwner` tenant kapsamlıdır.
- `BranchManager` ve `Staff` ileride branch scope ile sınırlandırılabilir.
- Tenant membership rolleri global Identity rolleri değildir ve `AspNetRoles` içine eklenmez.
- Tenant membership ekleme/suspend/revoke endpoint'leri `PlatformAdminWithStepUp` ister, hedef kullanıcıyı aktif `UserAccount` olarak doğrular ve audit kaydı üretir.
- `Revoked` terminal durumdur; revoked membership tekrar `Suspended` duruma çekilmez.
- Son aktif `BusinessOwner` membership'i suspend/revoke edilemez.
- `Suspended` tenant yalnızca auditli reactivation ile `Active` olabilir; `Closed` tenant tekrar suspend/reactivate edilemez.
- `Suspended` ve `Closed` tenant public discovery, slot arama, yeni booking request ve işletme booking operasyonlarına kapalıdır.
- Müşteri kendi mevcut booking request geçmişini görmeye ve izin verilen talebini iptal etmeye devam eder.
- Booking approval yüzeyi için `BusinessOwner` tenant-wide, `BranchManager` branch-scoped authz kullanılır; `Staff` onay/ret yetkisi almaz.
- Frontend işletme bağlamı `GET /api/business/context` üzerinden authenticated kullanıcının aktif tenant membership listesinden üretilir; kullanıcıya serbest tenant GUID seçtirilmez.
- Business context yalnızca `Active` tenant + `Active` membership döndürür; `BusinessOwner` tenant-wide, `BranchManager` branch-scoped capability alır, `Staff` varsayılan olarak yönetim capability'si almaz.
- Global müşteri appointment history tenant'ları Tenant Management üzerinden enumerate eder, her tenant için explicit `TenantId` set eder ve yalnızca müşterinin kendi request/appointment kayıtlarını döndürür.
- Business appointment request read model'i Organization/Resources label servisleri üzerinden branch/staff/resource görünen adlarını döndürür; bu label'lar endpoint authz yerine geçmez.
- Endpoint açıldığında her komut authn, authz, tenant isolation, audit, rate limit ve idempotency değerlendirmesinden geçmelidir.
- Platform admin tenant provisioning endpoint'i owner kullanıcısının aktif `UserAccount` olduğunu Identity üzerinden doğrular; Tenant Management modülü Identity assembly'sine doğrudan referans almaz.

## Test Kapsamı

Tenant Management entegrasyon testleri gerçek PostgreSQL üzerinde geçici test DB'si oluşturur ve migration'ı uygular.

Doğrulananlar:

- Migration seed verisi üretmez.
- Tenant + membership + audit kaydı persist edilebilir.
- Slug benzersizliği case-insensitive davranır.
- Bir kullanıcı aynı tenant içinde tek membership alabilir.
- `BusinessOwner` branch scope ile oluşturulamaz.
- Tenant provisioning service, ilk owner membership ve audit kaydını üretir.
- Tenant membership service, status geçişlerini auditler, revoked üyeliği terminal tutar ve son aktif `BusinessOwner` revoke/suspend denemesini reddeder.
- Admin control-plane API, tenant liste/detay ve membership add/suspend/revoke akışlarını `PlatformAdminWithStepUp` ile doğrular.
- Aktif `AccountClosureCase` taşıyan kullanıcı control-plane üzerinden yeni tenant owner veya aktif membership olarak atanamaz; bu cross-module kontrol API composition root içinde uygulanır.
- Tenant lifecycle servis/API testleri suspend/reactivate/close idempotency, zorunlu neden, row-lock yarışı ve terminal `Closed` davranışını doğrular.
- API testleri suspended tenant'ın public discovery/yeni booking/işletme operasyonlarına kapandığını ve müşteri geçmişinin erişilebilir kaldığını doğrular.

## Açık İşler

- Explicit tenant scope taşıyan background job kontratı Phase 2 booking expiry worker ile ilk kez uygulandı; merkezi job dashboard/retry politikası sonraki fazda detaylandırılacak.
- Tenant lifecycle operasyon runbook'u ve reason-code taksonomisi
- Tenant suspend/close sonrasında açık booking request temizleme/expiry entegrasyonu
- Tenant self-service işletme yönetimi için `BusinessOwner`/`BranchManager` authz yüzeyi
- `BranchManager`/`Staff` branch scope doğrulamasının Organization branch lifecycle kaynağına bağlanması
- Bootstrap sonrası ilk tenant oluşturma UI/runbook akışı
