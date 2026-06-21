# Phase 5e - Platform Büyüme ve Uluslararasılaşma (i18n)

> Bu dosya, eski tek parça `phase-5-platformlastirma-genisleme.md`'nin
> parçalanmasıyla oluştu (bkz. ADR-068). Bu faz **en yüksek riskli** platform
> genişleme fazıdır ve tüm 5a–5d alt fazları tamamlandıktan, ürün/hacim
> doğrulandıktan sonra başlar.

## Amaç

RezSaaS'i gerçek bir "salon platformu"na taşımak: marketplace büyüme araçları,
uluslararası açılım (locale/currency), sağlayıcı soyutlaması ve tenant/deployment
taşıma politikası. Bu faz; altyapı, para birimi, sağlayıcı ve veri taşıma gibi
**geri dönüşü zor** kararlar içerir.

## Kapsam

- Marketplace growth araçları (sponsored placement vb.) — discovery hacmi oluştuğunda
- Resource capacity analytics (Phase 5b ile kesişen derinleşme)
- Uluslararası açılım: locale, currency, provider abstraction
- Tenant/deployment taşıma politikası (multi-region veya extractability hazırlığı)
- Fiyatlandırma planları ve limitlerin i18n/currency ile tutarlılığı

## Backend teslimatları

- Marketplace sponsored placement modeli: discovery ranking'e kontrollü sponsorship
  sinyali; en-boy (fairness) ve şeffaflık kuralları ADR ile.
- Locale/currency altyapısı: para birimi alanları `decimal` + currency code; tüm
  tutarlar tenant/bölge bazında; para birimi dönüşümü ayrı read service.
- Provider abstraction: Payments/Messaging/Integrations sağlayıcıları soyutlanır,
  bölgeye göre farklı sağlayıcı çalışabilir (AGENTS.md §3.1 provider-agnostic).
- Tenant/deployment taşıma: tenant verisini taşıma/export politikası; extractability
  için ayrı ADR ve operasyon runbook'u (`docs/27`/`28` runbook'larıyla ilişkili).
- Fiyatlandırma planları ve SMS kota/overage kuralları currency-aware.
- Tüm yeni yüzeyler: tenant izolasyonu, PII sınırı, audit, idempotency, rate limit.

## Frontend teslimatları

- Marketplace/sponsored placement işletme yönetim yüzeyi (`BusinessOwner` + step-up).
- İlk i18n locale (en az İngilizce pilotu); Türkçe varsayılan, region switching.
- Para birimi gösterimi; müşteri ve işletme için region-aware format.
- Sponsored placement'in discovery sonuçlarında şeffaf etiketlemesi ( reklam
  olduğunu gizlemez, ADR-054 ürün dili bütünlüğü).

## Bağımlılıklar

- **Ön koşul faz:** Phase 5a (settings CRUD), 5b (Analytics), 5c (Integrations),
  5d (Messaging) tamamlandıktan ve ürün/hacim doğrulandıktan sonra başlar.
- **ADRs:** ADR-068; i18n, currency, marketplace ve tenant taşıma için ayrı ADR'ler.
- **Açık sorular:** `docs/12` — "İlk pilot şehir ve işletme kategorileri hangileri?"
  ve "Platform support erişiminde kim ... tenant verisi görebilir?" soruları bu
  fazın operasyonel tasarımını etkiler.
- **Ürün kapısı:** Bu faz MVP lansman kapısı değildir; ölçek ve hacim doğrulaması
  gerektirir.

## Kabul kriterleri

- Marketplace sponsored placement şeffaf etiketli; en-boy ve kalite sinyallerini
  gizlemez.
- Para birimi dönüşümü ve locale gösterimi tutarlı; tutarlar her zaman currency
  code ile saklanır/gösterilir.
- Sağlayıcı abstraction bölgeye göre doğru sağlayıcı seçer; tenant izolasyonu
  bozulmaz.
- Tenant taşıma/export politikası ADR + runbook ile tanımlı; operasyon denetimli.
- Tüm yeni yüzeyler güvenlik minimumlarını (audit, idempotency, rate limit, PII)
  taşır; geriye dönük veri taşıma (data migration) sırasında tenant izolasyonu korunur.

## Güvenlik / tenant minimumları

- Marketplace mutation'ları `BusinessOwner` + step-up; platform mutation'ları
  `PlatformAdminWithStepUp`.
- Tenant taşıma sırasında `tenant_id` filtresi hiçbir adımda düşmez; raw SQL
  gerekiyorsa parametrik ve tenant filtreli (AGENTS.md §4.2).
- Para birimi/tutar PII değildir fakat işletme finansal verisi tenant-scoped ve
  auditli; platform analytics raw `UserAccountId` içermez.
- Sponsored placement kalite/şikayet sinyallerini gizlemez; reklam etiketi şeffaf.
- Provider secret'ları bölgesel olarak ayrı secret manager'da; repo/config gömülmez.

## Mevcut durum

- Başlamadı. En yüksek riskli faz; 5a–5d ve ürün/hacim doğrulaması bekler.
- Mevcut sistem tek locale (Türkçe) ve tek currency (örtük TRY) varsayımıyla
  tasarlandı; i18n ve currency altyapısı sıfırdan eklenir. Marketplace ve tenant
  taşıma henüz tasarlanmadı.
