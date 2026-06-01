# Phase 0 — Keşif ve Karar

## Amaç

Kod yazmadan önce ürünün doğru sınırlarla tanımlanması; domain’in netleştirilmesi; uyumluluk, ödeme ve mesajlaşma kararlarının çerçevelenmesi.

## Teslimatlar (çıktılar)

- PRD (hedef kitle, problem, MVP kapsamı, non-goals)
- Domain glossary + ubiquitous language
- Kullanıcı rolleri ve erişim matrisi (permission matrix)
- Müşteri yolculukları (keşif → slot → doğrulama → rezervasyon → yorum)
- İşletme iş akışları (kurulum, çalışma saatleri, izin, kaynak kapatma, iptal/no-show)
- Rezervasyon durum makinesi (state machine) taslağı (**işletme onaylı akış** dahil)
- Kaynak tipleri listesi (chair/room/bed/station/cihaz…)
- KVKK veri envanteri taslağı + saklama süreleri hipotezi
- Ödeme kapsamı kararı: **MVP’de ödeme yok**; depozito/prepayment sonraki fazlarda
- Fiyatlandırma hipotezi: planlar + limitler + SMS kota/overage yaklaşımı

## Kritik karar noktaları

- “Service + Staff + Resource + Availability” omurgası doğrulanmadan Phase 1’e geçilmez.
- Ürün dili “berber sitesi” değil, çoklu kategoriye genişleyebilir “salon business OS” olmalı.

## Kabul kriterleri

- Glossary’de tüm temel kavramlar tek anlamlı: `Resource`, `ServiceVariant`, `AvailabilityRule`, `Appointment` vb.
- Permission matrix; tenant/branch ayrımı ve rol bazlı yetkileri net tanımlar.
- Rezervasyon state machine; iptal/no-show/complete akışlarını içerir.
