# Tenant Management Temeli

## Amaç

Tenant Management modülü; RezSaaS içindeki tenant yaşam döngüsü, tenant üyeliği ve tenant yönetim audit omurgasının başlangıç noktasıdır.

Bu modül şu an yalnızca domain ve persistence temelini sağlar. Tenant/işletme yönetim endpoint'leri `PlatformAdminWithStepUp` ve tenant membership authz kontrolleri kullanılmadan yayınlanmaz.

## Uygulanan Temel

- `Tenant`: tenant izolasyon sınırı, slug, görünen ad, durum ve yaşam zamanı alanları
- `TenantMembership`: platform-global `UserAccount` ile tenant içi rol/scope ilişkisi
- `TenantMembershipRole`: `BusinessOwner`, `BranchManager`, `Staff`
- `TenantMembershipStatus`: `Active`, `Suspended`, `Revoked`
- `TenantAuditLogEntry`: tenant yönetim aksiyonları için append-only audit başlangıcı
- EF Core `TenantManagementDbContext`
- PostgreSQL `tenant_management` schema'sı

## DB Kuralları

- `Tenants.NormalizedSlug` benzersizdir.
- `TenantMemberships(TenantId, UserAccountId)` benzersizdir.
- `BusinessOwner` branch scope alamaz.
- Audit detayları `jsonb` olarak saklanır.
- Migration tenant, üyelik, kullanıcı, rol veya operasyon verisi seed etmez.

## Yetki Sınırı

- `BusinessOwner` tenant kapsamlıdır.
- `BranchManager` ve `Staff` ileride branch scope ile sınırlandırılabilir.
- Tenant membership rolleri global Identity rolleri değildir ve `AspNetRoles` içine eklenmez.
- Endpoint açıldığında her komut authn, authz, tenant isolation, audit, rate limit ve idempotency değerlendirmesinden geçmelidir.

## Test Kapsamı

Tenant Management entegrasyon testleri gerçek PostgreSQL üzerinde geçici test DB'si oluşturur ve migration'ı uygular.

Doğrulananlar:

- Migration seed verisi üretmez.
- Tenant + membership + audit kaydı persist edilebilir.
- Slug benzersizliği case-insensitive davranır.
- Bir kullanıcı aynı tenant içinde tek membership alabilir.
- `BusinessOwner` branch scope ile oluşturulamaz.

## Açık İşler

- Explicit tenant scope taşıyan background job kontratı
- Tenant/işletme yönetim application service ve endpoint yüzeyi
- Bootstrap sonrası ilk tenant oluşturma ürün akışı
