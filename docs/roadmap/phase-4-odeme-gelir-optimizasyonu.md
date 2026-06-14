# Phase 4 - Ödeme ve Gelir Optimizasyonu

## Amaç

Booking çekirdeği ve pilot kullanımı doğrulandıktan sonra ödeme akışlarını kademeli açmak ve birim ekonomiyi iyileştirmek. Bu faz MVP lansman kapısı değildir.

## Sıralama

1) Depozito  
2) Full prepayment  
3) Refund/chargeback süreçleri + webhook tabanlı ödeme durum senkronizasyonu

## Kapsam genişletmeleri (opsiyonel)

- Paket/membership, gift card
- İptal politikası, ücretli no-show kuralları
- Branch-level ödeme yöntemi farklılaşması
- Analitik: occupancy, no-show rate, dönüşüm, top services

## Kabul kriterleri (örnek)

- Kart verisi sistemde tutulmaz (hosted/redirect checkout).
- Ödeme webhooks idempotent işlenir ve auditlenir.
- Refund ve hata senaryoları operasyon runbook'unda tanımlıdır.

## Mevcut durum

- Tamamlandı: `Payments` modülü ayrı schema/DbContext/migration ile eklendi.
- Tamamlandı: ödeme tahsilatı default kapalı tutuldu; provider key olmadan online collection açılamaz.
- Tamamlandı: `PaymentPolicy`, `PaymentIntent`, `PaymentWebhookEvent` ve `PaymentAuditLogEntry` domain/persistence temeli oluşturuldu.
- Tamamlandı: webhook idempotency için provider event id unique index ve raw payload yerine SHA-256 payload hash saklama kuralı eklendi.
- Tamamlandı: `GET /api/admin/payments/readiness` yalnız `PlatformAdminWithStepUp` ile read-only olarak açıldı.
- Bekliyor: provider seçimi, hosted checkout adapter'ı, business/customer payment endpoint'leri, gerçek webhook signature doğrulaması, refund/chargeback runbook'u.

