# Phase 5a - İşletme Yönetim CRUD ve Gelişmiş Yetki Ağacı

> Bu dosya, eski tek parça `phase-5-platformlastirma-genisleme.md`'nin
> parçalanmasıyla oluştu (bkz. ADR-068). Bu faz, **Frontend F6** settings CRUD
> dilimini bloke eden tek backend ön koşuldur ve öncelikli başlangıç dilimidir.

## Amaç

İşletme panelini "request inbox" seviyesinden gerçek bir **salon operasyon
OS**'ine taşımak için gereken backend CRUD endpoint'lerini açmak. Aynı zamanda
çoklu şube yönetimini ve gelişmiş yetki ağacını (rol + branch scope + kritik
aksiyon step-up) keskinleştirmek.

Bu faz, `docs/24-frontend-uygulama-plani.md` F6 "İşletme Operasyon Derinliği"
diliminin kapanmasını sağlayan ana backend dayanağıdır.

## Kapsam

- Branch, staff, skill ve membership scope yönetimi (CRUD)
- Service, service variant ve required skill/resource type yönetimi (CRUD)
- Resource (chair/room/bed/station/device) CRUD + block/out-of-service
- Working hours ve staff unavailable yönetimi
- Public profil metadata/galeri/slot ayarı genişletilmiş yönetimi
- Çoklu şube karşılaştırma ve gelişmiş raporlama read model'leri
- Gelişmiş yetki ağacı: rol + branch scope + kritik aksiyon step-up

## Backend teslimatları

### Organization modülü
- Branch CRUD endpoint'leri: `BusinessOwner` tenant-wide; `BranchManager`
  branch-scoped; tenant header + membership authz + audit + idempotency.
- Staff/membership branch-scope atama ve `BranchManager`/`Staff` tenant membership
  lifecycle'ına bağlama (ADR-046 kurallarıyla uyumlu, membership mutation'ları
  `PlatformAdminWithStepUp` gerektiren tenant-wide mutation'dan ayrı tutulur).
- Çoklu aktif `Business` henüz desteklenmiyorsa, settings mutation `409` döner
  (ADR-064); multi-business contract ayrı ADR ister (`docs/12` açık sorusu).

### Catalog modülü
- Service/service-variant/required-skill CRUD endpoint'leri.
- Required skill `ServiceRequiredSkill` + `StaffSkill` eşleşmesi korunur (ADR-036).

### Resources modülü
- Resource CRUD (chair/room/bed/station/device vb.); `Resource` fiziksel kapasite
  olarak geneldir (ADR-002, AGENTS.md §2.2).
- Resource block/out-of-service zaten Phase 3'te açıldı; bu faz CRUD derinliğini
  kapatır. Public slot hesaplama resource block sinyalini kullanmaya devam eder.

### Availability modülü
- Working hours ve staff unavailable CRUD endpoint'leri; slot hesaplama motoru
  bunları existing sinyallerle birlikte değerlendirir (ADR-033).
- Branch public slot ayarları (`SlotIntervalMinutes`, `MaxPublicSlots`) config
  default'larını override eder; ayarlar pozitif değer olmak zorundadır.

### Gelişmiş yetki ağacı
- Branch-scoped `BranchManager` policy'si tüm yeni mutation'larda uygulanır.
- Kritik aksiyonlar (rol/scope değişimi, ödeme ayarı) için tenant-scope step-up
  veya `PlatformAdminWithStepUp` zorunluluğu netleşir.
- Tüm yeni endpoint'ler `AGENTS.md §4.3` yetki kurallarına uyar: tenant dışı `404`,
  yetersiz rol `403`.

## Frontend teslimatları (F6.2)

- `/panel/ayarlar/subeler` — branch yönetimi (`BusinessOwner`/`BranchManager`).
- `/panel/ayarlar/personel` — staff, skill ve membership scope yönetimi.
- `/panel/ayarlar/hizmetler` — service/variant/required-skill yönetimi.
- `/panel/ayarlar/kaynaklar` — resource CRUD; müşteriye GUID gösterilmez.
- `/panel/ayarlar/calisma-saatleri` — working hours + staff unavailable.
- `/panel/ayarlar/profil` — zaten var (ADR-064); galeri/metadata genişletme.
- Çoklu şube karşılaştırma read-only görünümü.
- Tüm formlar backend contract'ı olmadan sahte teslim edilmez; OpenAPI artifact
  üzerinden generated tiplerle çalışır (ADR-055).

## Bağımlılıklar

- **Ön koşul faz:** Phase 3 (booking operasyon, abuse, tenant control-plane).
- **ADRs:** ADR-040 (public profil metadata Organization'da), ADR-060 (staff
  tercihi opsiyonel, resource internal), ADR-061 (read model'ler composition
  root), ADR-062 (Phase 3 appointment operasyon), ADR-068 (yol haritası refactor).
- **Açık sorular (blokaj):** `docs/12` — "`BranchManager`/`Staff` tenant
  membership branch scope doğrulaması Organization branch lifecycle kaynağına
  hangi contract ile bağlanacak?" ve "Bir tenant içinde birden fazla `Business`
  desteklenecekse ... hangi contract ile açılacak?" soruları bu fazın contract
  tasarımını etkiler; multi-business 5a'da yapılmaz.
- **Diğer fazlar:** Phase 5b (Analytics) ve 5c (API/webhook) 5a'dan bağımsız
  paralel başlayabilir; 5a settings verisi olmadan 5b metrikleri sığ kalır.

## Kabul kriterleri

- `BusinessOwner` tenant-wide, `BranchManager` branch-scoped davranış tüm yeni
  CRUD endpoint'lerinde integration testiyle doğrulanır.
- Branch dışı bir kaynağa erişim `404`, yetersiz rol `403` döner.
- Staff/resource/service CRUD sonrası slot hesaplama ve booking create doğrulaması
  yeni veriyi tutarlı kullanır (çakışma kuralı bozulmaz, ADR-010/025).
- Frontend hiçbir yerde resource GUID veya tenant GUID'i kullanıcıya operasyon
  etiketi olarak göstermez (ADR-060).
- Tüm mutation'lar `Idempotency-Key` destekler; raw key saklanmaz (ADR-037).
- Public slot/create akışı, yeni working hours/resource block/staff unavailable
  verisini mevcut sinyallerle birleştirir (ADR-033/034/036).

## Güvenlik / tenant minimumları

- Yeni endpoint'ler: authn, authz, tenant isolation, idempotency, audit, rate limit
  değerlendirmesi olmadan yayınlanmaz (AGENTS.md §12).
- Rol/scope değişimi kritik aksiyonu step-up gerektirir (AGENTS.md §4.3/§6.6).
- Resource block komutları resource→branch doğrulaması ve tenant membership authz
  olmadan yayınlanmaz; public slot hesaplama resource block sinyalini kullanır.
- PII (staff iletişim, müşteri) log/audit'e eklenmez; telefon/e-posta maskelenir.

## Mevcut durum

- Başlangıç fazı. Backend modülleri (Organization, Catalog, Resources,
  Availability) mevcut; domain/persistence temeli hazır. Yalnızca business panel
  mutation CRUD endpoint'leri ve çoklu şube karşılaştırma read model'i eksik.
- Frontend F6 appointment operasyonları (calendar/cancel/complete/no-show/
  rebook/note) ve profil ayar formu (ADR-064) tamamlandı; settings CRUD formları
  bu fazın backend contract'larını bekliyor.