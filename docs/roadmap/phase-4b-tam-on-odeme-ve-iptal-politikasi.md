# Phase 4b - Tam Ön-Ödeme ve İptal Politikası

> Bu dosya, eski tek parça `phase-4-odeme-gelir-optimizasyonu.md`'nin
> parçalanmasıyla oluştu (bkz. ADR-068). Phase 4a'nın (depozito + no-show)
> tamamlanmasını zorunlu ön koşul alır.

## Amaç

Phase 4a'daki hosted checkout ve webhook altyapısı doğrulandıktan sonra,
**tam ön-ödeme** akışını ve buna bağlı **iptal politikası / chargeback**
süreçlerini açmak. Amaç, birim ekonomiyi iyileştirirken refund/chargeback
riskini denetimli tutmaktır.

## Kapsam

- Tam ön-ödeme (full prepayment) seçeneği `PaymentPolicy` üzerinde
- İptal politikası motoru: zaman bazlı refund oranı, ücretsiz iptal penceresi
- Chargeback/webhook senkronizasyonu ve operasyonel müdahale akışı
- İşletme bazlı iptal politikası farklılaşması (branch-level)

## Backend teslimatları

- `PaymentPolicy` üzerinde full-prepayment mode + cancellation policy alanları.
- İptal politikası hesaplama domain servisi: randevu başlangıcına kalan süreye
  göre refund oranı; tüm kurallar tenant-scoped ve branch override destekli.
- Refund işlemi: idempotent, auditli, sağlayıcı refund API'sine bağlanır; raw
  sağlayıcı payload'u saklanmaz (ADR-065).
- Chargeback webhook işleme: open/lost/won durumları, dispute evidence alanları.
- İşletme iptal politikası ayar endpoint'i: `BusinessOwner` tenant-wide + step-up.
- Booking cancel/complete/no-show komutları ödeme durumunu snapshot'ta günceller;
  çift işlem `Idempotency-Key` ve DB row lock ile engellenir.

## Frontend teslimatları

- Checkout ekranında tam ön-ödeme vs depozito seçeneği (varsa) net gösterimi.
- İptal penceresi ve refund oranı müşteriye randevu öncesi/sırasında anlaşılır
  biçimde gösterilir; yanlış söz (ör. "tam iade") verilmez.
- İşletme iptal politikası yönetim formu (`BusinessOwner` only).
- Chargeback/odak durumunun işletme appointment detail'inde *maskelenmiş/özet*
  görünümü; raw sağlayıcı detayı sızdırılmaz.

## Bağımlılıklar

- **Ön koşul faz:** Phase 4a (depozito + no-show + hosted checkout + webhook
  imza doğrulaması) tamamlanmış olmalı.
- **ADRs:** ADR-065, ADR-062 (Phase 3 booking operasyon state geçişleri),
  ADR-068 (yol haritası refactor).
- **Diğer fazlar:** Phase 4c (gelir genişleme: paket/membership/gift card) yalnızca
  4b tamamlandıktan sonra başlayabilir.

## Kabul kriterleri

- Tam ön-ödeme tahsilatı hosted/redirect checkout ile; kart verisi saklanmaz.
- İptal politikası kuralları müşteriye *tahsilattan önce* gösterilir.
- Refund/chargeback işlemleri idempotent, auditli ve operasyon runbook'una bağlı.
- Chargeback `lost` sonucu booking durumuna ve gerekirse abuse/şikayet akışına
  işlenir; otomatik sanction üretmez (ADR-048/049 ile uyumlu).
- Aynı iptal niyetinin retry'ı çift refund üretmez.
- Explicit konfigürasyon olmadan tam ön-ödeme açılamaz.

## Güvenlik / tenant minimumları

- Ödeme ayar mutation'ları `BusinessOwner` tenant-wide yetki + tenant-scope step-up
  olmadan yayınlanmaz.
- Refund/chargeback audit kayıtları append-only.
- Webhook raw payload saklanmaz; yalnızca payload hash + event id + idempotent
  delivery durumu tutulur.
- PII (kart, müşteri iletişim) log/audit/response'a eklenmez.

## Mevcut durum

- Başlamadı. Phase 4a tamamlanmadan başlatılamaz. `Payments` modülü readiness
  temeli (ADR-065) hazır; tam ön-ödeme alanları ve cancellation policy domain
  servisi henüz eklenmedi.