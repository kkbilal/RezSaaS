# Phase 5b - Analytics Modülü

> Bu dosya, eski tek parça `phase-5-platformlastirma-genisleme.md`'nin
> parçalanmasıyla oluştu (bkz. ADR-068). Bu faz, AGENTS.md modül listesinde
> tanımlı olan fakat henüz kodu olmayan **Analytics** modülünü kurar.

## Amaç

İşletme ve platform için **veriye dayalı karar** verebilmek için read-only
analitik read model'leri ve dashboard API'lerini üretmek: occupancy, no-show
rate, dönüşüm, top services ve resource capacity analytics.

`Analytics` modülü modüler monolith sınırlarına (AGENTS.md §3.2/§11.2) tam
uymalıdır: write-side modüllerin (Booking, Catalog, Resources) tablolarına
**doğrudan yazılamaz/okunamaz**; gerekiyorsa ayrı read model tasarlanır
(AGENTS.md §3.2 son kuralı).

## Kapsam

- İşletme dashboard: occupancy, no-show rate, dönüşüm, top services
- Resource capacity analytics (kaynak kullanım oranı)
- Çoklu şube karşılaştırma metrikleri (Phase 5a read model'leriyle kesişir)
- Platform-genel metrikler (yalnız `PlatformAdminWithStepUp`)
- Veri envanteri ve saklama politikası etkisi (`docs/11`)

## Backend teslimatları

- Yeni `RezSaaS.Modules.Analytics` modülü: ayrı schema/DbContext/migration,
  `IModule` implementasyonu, modülden modüle assembly referansı yok.
- Read model doldurma stratejisi: transactional outbox/event veya kontrollü
  projection. Write modüllerine doğrudan tablo erişimi **yasaktır**; cross-module
  raporlama için ayrı read model (AGENTS.md §3.2).
- İşletme analytics sorgu endpoint'leri: tenant header + membership authz
  (`BusinessOwner` tenant-wide, `BranchManager` branch-scoped); salt okunur.
- Platform analytics sorgu endpoint'leri: yalnız `PlatformAdminWithStepUp`.
- Tüm metrik sorguları tenant filtreli; global sorgu yalnız platform admin ve
  açık ADR ile. Query filter bypass PR review'da reddedilir (AGENTS.md §4.2).
- Zaman aralığı parametreleri UTC; şube timezone gösterimi response'da ayrı.
- Hesaplama deterministik ve test edilebilir; mock metrik kabul edilmez
  (Frontend `docs/24` "Bilinçli Olarak Ertelenenler": sahte dashboard reddi).

## Frontend teslimatları

- `/panel/analiz` — işletme dashboard'u; gerçek metriklerle occupancy/no-show/
  dönüşüm/top services görselleştirmesi. Sahte metrik yasak (ADR-054 gerekçesi).
- `/panel/analiz/kaynaklar` — resource capacity analytics.
- `/platform/analiz` — platform-genel metrikler (yalnız step-up admin).
- Tüm grafikler generated OpenAPI tipleriyle gerçek API'den okur (ADR-055).

## Bağımlılıklar

- **Ön koşul faz:** Phase 3 (booking operasyon tamamlanınca anlamlı veri oluşur).
- **Önerilen eş faz:** Phase 5a (settings verisi olmadan kaynak/şube kırılımı sığ
  kalır); opsiyonel olarak 4a/4b (ödeme metrikleri).
- **ADRs:** ADR-061 (read model'ler composition root), ADR-068; read model
  projection stratejisi için yeni ADR eklenir.
- **Açık sorular:** `docs/12` — saklama politikası ve KVKK veri envanteri
  etkisi (`docs/11`) bu fazda netleşmeli.

## Kabul kriterleri

- `Analytics` modülü mimari testlerine (module isolation, `IModule`) uyar.
- İşletme metrikleri yalnız aktif tenant membership'in scope'unda döner; tenant
  dışı/branch dışı veri sızdırmaz.
- Platform metrikleri yalnız `PlatformAdminWithStepUp` ile erişilebilir.
- Read model'ler write modüllerinin tablolarına doğrudan erişmez.
- Metrik hesaplamaları deterministik testlerle doğrulanır; sahte değer yok.
- Zaman aralığı ve timezone kuralları tutarlı (AGENTS.md §5.3).

## Güvenlik / tenant minimumları

- Tüm analytics sorguları tenant filtreli; query filter bypass yasak.
- Platform analytics raw `UserAccountId`/PII içermez; yalnızca özet/sayı.
- Yeni read model'ler PII içeriyorsa `docs/11` veri envanteri güncellenir.
- Saklama politikası: analytics projection verisi için retention netleşir.
- Rate limit: ağır aggregation sorguları için endpoint bazlı rate limit.

## Mevcut durum

- Başlamadı. `Analytics` modülü kod olarak mevcut değil; AGENTS.md modül
  listesinde tanımlıdır. Bu faz onu sıfırdan kurar. Frontend sahte dashboard
  metrikleri teslim etmez (`docs/24` Bilinçli Olarak Ertelenenler).