# Phase 5c - Açık API ve Webhook Teslimatı

> Bu dosya, eski tek parça `phase-5-platformlastirma-genisleme.md`'nin
> parçalanmasıyla oluştu (bkz. ADR-068). `Integrations` modülünün readiness
> temeli (ADR-066/067) üzerine ilk canlı dış entegrasyon yüzeyini koyar.

## Amaç

`Integrations` modülünün hazır olan application-layer lifecycle servisleri
üzerine: işletme self-service API client/webhook mutation'larını, public external
API authentication + scope enforcement'i ve webhook worker/delivery/retry/signature
gönderimini açmak.

## Kapsam

- Business self-service API client create/update/revoke mutation'ları
- Business webhook subscription create/update/revoke mutation'ları
- Public external API authentication (API key) ve scope enforcement
- Webhook delivery worker: retry, exponential backoff, signature gönderimi
- CRM/export sağlayıcı adapter'larının *ilk* kontrollü pilotları

## Backend teslimatları

- Business integration mutation endpoint'leri: tenant header + `BusinessOwner`
  tenant-wide yetki + tenant-scope step-up kararı olmadan yayınlanmaz (AGENTS.md
  §6.6 son blok). Plaintext secret yalnız create sonucunda tek seferlik döner;
  DB/audit/log içinde yalnız hash, prefix ve metadata kalır (ADR-067).
- Public external API auth: tenant-scoped API key doğrulaması + scope enforcement
  (read bookings, write appointments vb.); rate limit + idempotency.
- Webhook delivery worker: lease-based, unique delivery key, sınırlı retry,
  terminal durum koruması; raw payload saklanmaz, payload hash + correlation id +
  event type + idempotent delivery durumu tutulur (ADR-066).
- Webhook imza gönderimi: signing secret raw geri okunamaz, yalnız create'te tek
  seferlik; delivery HMAC imzalı.
- İlk CRM/export adapter'ları kontrollü pilot olarak; external delivery explicit
  config olmadan kapalı kalır.
- Webhook delivery raw payload saklamaz; payload hash, correlation id, event type
  ve idempotent delivery durumu tutulur (AGENTS.md §6.6).

## Frontend teslimatları

- `/panel/ayarlar/entegrasyonlar` — API client ve webhook subscription yönetimi;
  plaintext secret yalnız tek seferlik gösterim, sonrasında maskelenmiş.
- Webhook delivery durumu ve son denemelerin işletme tarafından *özet* görünümü;
  raw payload veya hedef URL secret kısımı maskelenmiş.
- CRM/export bağlantı kurulumu (`BusinessOwner` + step-up).

## Bağımlılıklar

- **Ön koşul faz:** Phase 3 (booking operasyon, tenant control-plane). ADR-066
  readiness temeli zaten mevcut.
- **ADRs:** ADR-066 (Integrations default kapalı foundation), ADR-067 (lifecycle
  servisleri), ADR-068 (yol haritası refactor).
- **Açık sorular:** `docs/12` — "Public external API authentication ve scope
  enforcement" contract'ı bu fazda netleşir.
- **Diğer fazlar:** Phase 5c, 5a/5b'den bağımsız paralel başlayabilir.

## Kabul kriterleri

- Integration API key ve webhook signing secret raw değeri DB/log/audit/response'ta
  yok; tek seferlik create sonucu dışında okunamaz.
- Webhook delivery raw payload saklanmaz; HMAC imzalı, idempotent ve retry'lı.
- Public external API her istekte scope enforcement uygular; tenant izolasyonu
  bozulmaz.
- Business integration mutation'ları `BusinessOwner` + tenant-scope step-up
  olmadan çalışmaz.
- External delivery explicit konfigürasyon olmadan kapalı kalır.
- Mimari testler: `Integrations` modülü başka modüle assembly referansı vermez.

## Güvenlik / tenant minimumları

- API key brute-force için rate limit; IP ban tek başına kalıcı uygulanmaz
  (AGENTS.md §7.2).
- Webhook hedef URL HTTPS zorunlu; query-string secret yasak (mevcut invariant).
- Secret yönetimi: raw değer repo/config dosyasına gömülmez; secret manager.
- Audit: API key create/revoke, webhook subscription create/revoke, delivery
  terminal durumları auditlenir (append-only).
- PII: webhook payload müşteri PII'si içeriyorsa `docs/11` güncellenir ve masking
  kuralı uygulanır.

## Mevcut durum

- `Integrations` modülü tenant-scoped persistence temeliyle mevcut (ADR-066).
- External API ve webhook delivery varsayılan olarak kapalı.
- API key ve webhook signing secret raw saklanmaz; yalnız güvenli prefix/hash
  alanları tutulur.
- API client ve webhook subscription lifecycle servisleri application katmanında
  hazır (ADR-067); config kapalıyken create çalışmaz, plaintext secret yalnız
  create sonucunda tek seferlik döner.
- Webhook delivery raw payload saklamaz; payload hash, correlation id, event type
  ve teslimat durumu izlenir.
- İlk API yüzeyi yalnız `PlatformAdminWithStepUp` korumalı read-only
  `/api/admin/integrations/readiness` endpoint'i.
- Bekliyor: business self-service mutation'ları, public API auth/scope, webhook
  worker/delivery/signature, CRM/export adapter pilotları, frontend entegrasyon
  yönetim ekranı.