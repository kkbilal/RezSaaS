# Phase 4a - Depozito ve No-Show Ücreti

> Bu dosya, eski tek parça `phase-4-odeme-gelir-optimizasyonu.md`'nin
> yeniden parçalanmasıyla oluştu (bkz. ADR-068). Orijinal dosya arşivlenmedi;
> bu dosya onun yerine geçer ve daha keskin kabul kriterleri taşır.

## Amaç

Booking çekirdeği ve pilot kullanımı doğrulandıktan sonra ödeme yüzeyini
güvenli bir şekilde, **en küçük geri döndürülebilir adımla** açmak: depozito
tahsilatı, no-show ücreti ve bunlara bağlı refund *okuma* akışı.

Bu faz **MVP lansman kapısı değildir**; `mvp-lansman-kapisi.md` bağımsızdır.
Phase 4a yalnızca `Payments` modülünün readiness temeli (ADR-065) üzerine
ilk canlı ödeme akışını koyar.

## Kapsam

- Tek bir ödeme sağlayıcısı seçimi ve hosted/redirect checkout adapter'ı
- `PaymentPolicy` üzerinde depozito ve no-show ücreti konfigürasyonu
- Müşteri ödeme başlatma (`PaymentIntent`) ve sağlayıcıya yönlendirme
- Ödeme webhook imza doğrulaması + idempotent kayıt
- İşletme/operasyon için refund/chargeback *salt okunur* durum görünürlüğü
- İlk gerçek ödeme hata ve refund senaryoları için operasyon runbook'u

## Backend teslimatları

- Sağlayıcı kararı ve ADR güncellemesi (raw secret repo/config dışında).
- `Payments` modülünde hosted checkout adapter; kart verisi, CVV, PAN veya raw
  sağlayıcı payload'u DB/log/audit/response içinde tutulmaz (ADR-065).
- İşletme ödeme ayarı mutation endpoint'i: tenant header + `BusinessOwner`
  tenant-wide yetki + tenant-scope step-up kararı (ADR-068 öncesi kural).
- Müşteri payment intent başlatma endpoint'i: auth zorunlu, tenant context
  doğrulanmış business slug üzerinden, `AppointmentRequest` snapshot'ına bağlı.
- Webhook imza doğrulaması: provider event id + payload hash ile idempotent
  (ADR-065 zaten hash kuralını getirdi; bu faz gerçek signature verify ekler).
- Refund/chargeback webhook kaydı ve salt-okunur business/admin sorgu yüzeyi.
- `Payments` schema/migration: yeni alanlar geriye uyumlu ve tenant-scoped.

## Frontend teslimatları

- Depozito/no-show ücreti bilgilendirme ve yönlendirme (hosted checkout) ekranı;
  kart formu asla frontend'de tutulmaz, kullanıcı sağlayıcıya yönlendirilir.
- İşletme ödeme ayarı formu yalnız `BusinessOwner` capability'siyle açılır.
- Ödeme durumunun müşteri talep ve işletme appointment detail'inde doğru gösterimi.
- `402`/`409`/`422`/`429` ödeme hatası için anlaşılır Türkçe recovery akışı.

## Bağımlılıklar

- **ADRs:** ADR-065 (Payments default kapalı foundation), ADR-068 (yol haritası refactor).
- **Açık sorular (blokaj):** `docs/12-acik-sorular.md` — "Production e-posta sağlayıcısı"
  ödeme sağlayıcı onboarding akışını etkileyebilir; "SMS sağlayıcısı" bu fazı bloklamaz.
- **Diğer fazlar:** Phase 4a, 4b'nin (tam ön-ödeme) ön koşuludur. Phase 3 tamamlanmış
  olmalı (booking snapshot, tenant header, audit, idempotency).

## Kabul kriterleri

- Kart verisi sistemde tutulmaz; tüm tahsilat hosted/redirect checkout ile olur.
- Ödeme webhook'ları imza doğrulamasıyla, idempotent ve auditli işlenir.
- Aynı `Idempotency-Key` ile tekrar gönderim çift tahsilat/çift refund üretmez.
- Refund ve hata senaryoları operasyon runbook'unda tanımlıdır.
- `Payments` modülü explicit konfigürasyon olmadan ödeme tahsilatı açamaz.
- Tenant dışı/branch dışı erişim `404`, yetersiz rol `403` döner; PII loglanmaz.

## Güvenlik / tenant minimumları

- Ödeme ayar mutation'ları `BusinessOwner` tenant-wide yetki + tenant-scope step-up
  veya `PlatformAdminWithStepUp` kararı olmadan yayınlanmaz.
- Read-only readiness yüzeyi yalnız `PlatformAdminWithStepUp` olabilir.
- Webhook raw payload saklanmaz; payload hash, correlation id, event type, teslimat
  durumu tutulur. Provider secret'ları repo/config dosyasına gömülmez.
- Ödeme audit kayıtları append-only; geçmiş kayıtlar uygulama üzerinden değiştirilemez.

## Mevcut durum

- Tamamlandı (ADR-065): `Payments` modülü ayrı schema/DbContext/migration ile eklendi,
  online tahsilat default kapalı, `PaymentPolicy`/`PaymentIntent`/`PaymentWebhookEvent`/
  `PaymentAuditLogEntry` domain/persistence temeli, webhook idempotency unique index +
  SHA-256 payload hash kuralı, `GET /api/admin/payments/readiness` (`PlatformAdminWithStepUp`).
- Bekliyor: sağlayıcı seçimi, hosted checkout adapter'ı, business/customer payment
  endpoint'leri, gerçek webhook imza doğrulaması, refund/chargeback runbook'u, frontend
  depozito/no-show yönlendirme ekranı.