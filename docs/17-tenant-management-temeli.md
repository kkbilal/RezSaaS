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
- `POST /api/admin/tenants`: API composition root altında `PlatformAdminWithStepUp` korumalı tenant provisioning yüzeyi
- `GET /api/admin/tenants`, `GET /api/admin/tenants/{tenantId}` ve `GET /api/admin/tenants/{tenantId}/memberships`: platform control-plane okuma yüzeyleri
- `POST /api/admin/tenants/{tenantId}/memberships`, `/suspend`, `/revoke`: platform control-plane membership yönetim yüzeyleri

## DB Kuralları

- `Tenants.NormalizedSlug` benzersizdir.
- `TenantMemberships(TenantId, UserAccountId)` benzersizdir.
- `BusinessOwner` branch scope alamaz.
- Audit detayları `jsonb` olarak saklanır.
- Migration tenant, üyelik, kullanıcı, rol veya operasyon verisi seed etmez.
- `TenantManagementDbContext` platform tenant registry'sidir; `Tenants` tablosu globaldir ve request-scope query filter kullanmaz.
- `TenantMembership` ve `TenantAuditLogEntry` erişimleri endpoint/application service açıldığında explicit `TenantId` parametresi, authz ve audit kontrolüyle yapılır; gizli tenant-context varsayımı kullanılmaz.

## Yetki Sınırı

- `BusinessOwner` tenant kapsamlıdır.
- `BranchManager` ve `Staff` ileride branch scope ile sınırlandırılabilir.
- Tenant membership rolleri global Identity rolleri değildir ve `AspNetRoles` içine eklenmez.
- Tenant membership ekleme/suspend/revoke endpoint'leri `PlatformAdminWithStepUp` ister, hedef kullanıcıyı aktif `UserAccount` olarak doğrular ve audit kaydı üretir.
- `Revoked` terminal durumdur; revoked membership tekrar `Suspended` duruma çekilmez.
- Son aktif `BusinessOwner` membership'i suspend/revoke edilemez.
- Booking approval yüzeyi için `BusinessOwner` tenant-wide, `BranchManager` branch-scoped authz kullanılır; `Staff` onay/ret yetkisi almaz.
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

## Açık İşler

- Explicit tenant scope taşıyan background job kontratı Phase 2 booking expiry worker ile ilk kez uygulandı; merkezi job dashboard/retry politikası sonraki fazda detaylandırılacak.
- Tenant lifecycle suspend/close komutları ve operasyon runbook'u
- Tenant self-service işletme yönetimi için `BusinessOwner`/`BranchManager` authz yüzeyi
- `BranchManager`/`Staff` branch scope doğrulamasının Organization branch lifecycle kaynağına bağlanması
- Bootstrap sonrası ilk tenant oluşturma UI/runbook akışı
