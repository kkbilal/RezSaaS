# Phase 4c - Gelir Genişleme (Opsiyonel)

> Bu dosya, eski tek parça `phase-4-odeme-gelir-optimizasyonu.md`'nin
> parçalanmasıyla oluştu (bkz. ADR-068). Bu faz **opsiyonel** bir genişleme
> fazıdır; ürün doğrulanmadan başlatılmaz.

## Amaç

Temel ödeme akışları (Phase 4a/4b) doğrulandıktan sonra, birim ekonomiyi
iyileştiren ek gelir modlarını **isteğe bağlı** olarak eklemek: paket,
membership, gift card ve branch-level ödeme yöntemi farklılaşması.

## Kapsam

- Paket/membership (çoklu seans veya süre bazlı)
- Gift card (alım + kullanım + bakiye sorgusu)
- Branch-level ödeme yöntemi farklılaşması
- Analitik kesişimi: bu faz, Phase 5b (Analytics modülü) ile birlikte ölçülebilir

## Backend teslimatları

- `Catalog`/`Payments` sınırlarında paket/membership modeli; modüller arası
  doğrudan tablo erişimi yok, açık application service kontratı (AGENTS.md §3.2).
- Gift card: satın alma (hosted checkout), bakiye sorgusu, kullanım uygulama.
  Kod/raw bakiye token'ı log/audit'e eklenmez.
- Branch-level ödeme yöntemi farklılaştırması `PaymentPolicy` branch override.
- Tüm yeni mutation endpoint'leri: tenant header + authz + idempotency + audit.
- Yeni DB tabloları: tenant-scoped kararı, index, migration, saklama politikası
  AGENTS.md §12 kontrol listesine göre işlenir.

## Frontend teslimatları

- Paket/membership satın alma ve kullanım UI'ı (hosted checkout yönlendirmesi).
- Gift card satın alma ve rezervasyonda kullanım akışı.
- Branch ödeme yöntemi farklılaşmasının işletme ayar formuna eklenmesi
  (`BusinessOwner` only).

## Bağımlılıklar

- **Ön koşul faz:** Phase 4a ve 4b tamamlanmış olmalı.
- **Önerilen eş faz:** Phase 5b (Analytics) — gelir metrikleri için.
- **ADRs:** ADR-065, ADR-068, ve yeni paket/membership modeli için ADR eklenir.
- **Ürün kapısı:** Bu faz yalnızca pilot kullanım ve gelir doğrulamasından sonra
  açılır; MVP lansman kapısı değildir.

## Kabul kriterleri

- Paket/membership/gift card kullanımı booking snapshot'ına yansır; çift kullanım
  DB constraint + idempotency ile engellenir.
- Gift card bakiyesi/kodu log/audit/response dışında raw tutulmaz.
- Branch-level fark yalnızca `BusinessOwner` tenant-wide + step-up ile değiştirilir.
- Tüm akışlar hosted/redirect checkout ile; kart verisi saklanmaz.
- Yeni modül sınırı bağımlılıkları mimari testiyle (AGENTS.md §11.2) denetlenir.

## Güvenlik / tenant minimumları

- Yeni mutation yüzeyleri authz, idempotency, audit ve rate limit değerlendirmesi
  olmadan yayınlanmaz (AGENTS.md §12).
- Gift card kodu brute-force için IP + hesap bazlı rate limit; NAT/CGNAT nedeniyle
  yalnızca IP bazlı ban uygulanmaz (AGENTS.md §7.2).
- Ödeme audit kayıtları append-only; tenant izolasyonu merkezi filtreyle.

## Mevcut durum

- Başlamadı. Opsiyonel faz; Phase 4a/4b ve ürün doğrulaması bekler.