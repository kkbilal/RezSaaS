# Gelir Modeli ve Ödeme Stratejisi (Taslak)

## İlke

İlk günden **SaaS-first**: ana gelir motoru abonelik. Pazaryeri komisyonu/lead-fee yalnızca discovery hacmi oluşursa ikincil katman.

## Plan taslağı (hipotez)

- **Launch**: ücretsiz/çok düşük giriş, 1 şube, 2 aktif personel, sınırlı aylık rezervasyon
- **Starter**: yıllık ~990–1.290 TL/ay (1 şube, 5 personel, sınırsız rezervasyon, temel rol, review isteği, SMS kotası)
- **Growth**: ~1.790–2.390 TL/ay (depozito, paket/membership, kaynak analitiği, gelişmiş yetki/filtreler)
- **Scale**: ~3.290–4.490 TL/ay (çoklu şube, API/webhook, onboarding, premium destek)

## Maliyet şeffaflığı

- OTP/hatırlatma SMS maliyeti planlarda “görünmez” yapılmaz: kota + overage yaklaşımı.

## Ödeme sağlayıcısı (Türkiye-first)

- Başlangıçta public pricing şeffaflığı nedeniyle `iyzico` değerlendirilir; GMV artınca alternatif sağlayıcılarla oran pazarlığı yapılır.

## Ödeme feature sıralaması

MVP: **ödeme yok** (operasyon + rezervasyon çekirdeği önce).

Ödeme açılacaksa önerilen sıra:

1) `pay-at-store`  
2) depozito  
3) full prepayment  
4) refund/webhook olgunluğu  
