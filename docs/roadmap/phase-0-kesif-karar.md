# Phase 0 - Keşif ve Karar

## Amaç

Kod yazmadan önce ürünün doğru sınırlarla tanımlanması; domain’in netleştirilmesi; uyumluluk, ödeme ve mesajlaşma kararlarının çerçevelenmesi.

## Teslimatlar (çıktılar)

- Kapsam ve MVP sınırı: `../00-kapsam-ozeti.md`
- Domain glossary: `../05-domain-sozlugu.md`
- Karar günlüğü: `../06-karar-kaydi.md`
- Kullanıcı rolleri ve erişim matrisi: `../07-yetki-matrisi.md`
- Müşteri yolculukları (keşif → slot → doğrulama → rezervasyon → yorum)
- İşletme iş akışları (kurulum, çalışma saatleri, izin, kaynak kapatma, iptal/no-show)
- Rezervasyon durum makinesi: `../04-rezervasyon-akisi.md`
- Kaynak tipleri listesi (chair/room/bed/station/cihaz…)
- KVKK veri envanteri taslağı: `../11-veri-envanteri-taslagi.md`
- Bildirim kanalı kararı: `../08-bildirim-kanali-stratejisi.md`
- Ödeme kapsamı kararı: **MVP’de ödeme yok**; depozito/prepayment sonraki fazlarda
- Fiyatlandırma hipotezi: planlar + limitler + SMS kota/overage yaklaşımı

## Kritik karar noktaları

- “Service + Staff + Resource + Availability” omurgası doğrulanmadan Phase 1’e geçilmez.
- Ürün dili “berber sitesi” değil, çoklu kategoriye genişleyebilir “salon business OS” olmalı.

## Kabul kriterleri

- Glossary’de tüm temel kavramlar tek anlamlı: `Resource`, `ServiceVariant`, `AvailabilityRule`, `Appointment` vb.
- Permission matrix; tenant/branch ayrımı ve rol bazlı yetkileri net tanımlar.
- Rezervasyon state machine; iptal/no-show/complete akışlarını içerir.
- `../12-acik-sorular.md` içindeki Phase 1'i bloke eden sorular yanıtlanmıştır.
