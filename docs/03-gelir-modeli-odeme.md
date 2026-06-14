# Gelir Modeli ve Ödeme Stratejisi (Taslak)

## İlke

İlk günden **SaaS-first**: ana gelir motoru abonelik. Pazaryeri komisyonu/lead-fee yalnızca discovery hacmi oluşursa ikincil katman.

## Plan taslağı (hipotez)

Bu rakamlar ürün kararı değil, doğrulanacak fiyat hipotezidir. Lansman öncesi tarihli benchmark çalışmasıyla güncellenir.

- **Launch**: ücretsiz/çok düşük giriş, 1 şube, 2 aktif personel, sınırlı aylık rezervasyon
- **Starter**: yıllık ~990–1.290 TL/ay (1 şube, 5 personel, sınırsız rezervasyon, temel rol, review isteği, SMS kotası)
- **Growth**: ~1.790–2.390 TL/ay (depozito, paket/membership, kaynak analitiği, gelişmiş yetki/filtreler)
- **Scale**: ~3.290–4.490 TL/ay (çoklu şube, API/webhook, onboarding, premium destek)

## Maliyet şeffaflığı

- OTP/hatırlatma SMS maliyeti planlarda “görünmez” yapılmaz: kota + overage yaklaşımı.

## Ödeme sağlayıcısı (Türkiye-first)

- MVP'de online ödeme entegrasyonu yoktur.
- Ödeme fazına geçildiğinde hosted/redirect checkout sunan sağlayıcılar güncel komisyon, onboarding ve operasyon koşullarıyla yeniden değerlendirilir.

## Ödeme feature sıralaması

MVP: **ödeme yok** (operasyon + rezervasyon çekirdeği önce).

Ödeme açılacaksa önerilen sıra:

1) `pay-at-store`  
2) depozito  
3) full prepayment  
4) refund/webhook olgunluğu  

## Phase 4 backend başlangıcı

2026-06-14 itibarıyla Phase 4 backend hazırlığı provider seçmeden başlatıldı:

- `Payments` modülü ayrı PostgreSQL schema'sı ile eklenir.
- Online tahsilat default kapalıdır; provider seçimi ve onboarding kararı olmadan customer/business ödeme endpoint'i açılmaz.
- Kart verisi RezSaaS içinde tutulmaz; ileride yalnız hosted/redirect checkout desteklenir.
- Webhook idempotency provider event id + payload hash ile hazırlanır; raw sağlayıcı payload'u saklanmaz.
- İlk dış yüzey yalnız `PlatformAdminWithStepUp` korumalı read-only readiness endpoint'idir.
