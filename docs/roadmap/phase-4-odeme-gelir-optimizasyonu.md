# Phase 4 — Ödeme ve Gelir Optimizasyonu

## Amaç

Ödeme akışlarını kademeli açmak ve birim ekonomiyi iyileştirmek.

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

