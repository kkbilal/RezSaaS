# Phase 2 Uygulama Planı

Phase 2 hedefi, gerçek kullanıcı alabilecek ilk uçtan uca müşteri akışını üretmektir: keşif → işletme profili → uygun slot → hesaplı rezervasyon isteği → işletme onayı.

## Başlangıç Kararları

- Public işletme URL yapısı `/isletme/{businessSlug}` olarak başlar.
- `Business.NormalizedSlug` tek domain altında global benzersizdir; tenant içinde tekrar eden slug kabul edilmez.
- Anonymous public discovery tenant header istemez; yalnızca açık public işletme verisini read-only servis üzerinden döndürür.
- SMS sağlayıcı seçimi maliyet nedeniyle Phase 2 lansman kapısı değildir; aktif bildirim kanalı e-postadır, SMS/WhatsApp altyapısı sonraki fazlara hazır kalır.

## Dilim 2.0 - Public Directory Temeli

Durum: başladı.

- Tamamlandı: business public slug global benzersiz olacak şekilde Organization migration hazırlığı
- Tamamlandı: branch `city`, `district`, `addressLine` metadata alanları
- Tamamlandı: anonymous public business search/profile read service
- Tamamlandı: `/api/public/businesses` ve `/api/public/businesses/{slug}` endpoint yüzeyi
- Tamamlandı: public discovery IP bazlı rate limit policy
- Açık: business profil galeri, kurallar, puan ve SEO metadata alanları

## Dilim 2.1 - Public Profil Detayı

- Catalog read service: hizmet menüsü ve varyant fiyat/süreleri
- Staff görünürlüğü: aktif staff listesi ve gösterim politikası
- Branch çalışma saatleri: availability snapshot endpoint'i ile public profile birleşimi
- Response contract: public profile ekranının tek seferde çizilmesi için API composition kararı

## Dilim 2.2 - Slot Bulma

- Seçilen service variant toplam süre hesabı
- Staff tercihi: MVP'de optional; seçilmezse uygun staff adayları listelenir
- Resource uygunluğu: required resource type varsa sadece uyumlu resource adayları
- Confirmed appointment, staff unavailable ve resource block kontrolleri
- Slot response: UTC zaman + branch timezone gösterim bilgisi

## Dilim 2.3 - Rezervasyon İsteği Endpoint'i

- Auth zorunlu müşteri request create endpoint'i
- `booking-appointment-requests` rate limit policy zorunlu
- Tenant/branch/business eşlemesi public slug üzerinden doğrulanır
- Kullanıcı pending/günlük limitleri ve abuse event üretimi devrede kalır
- Request sonucu `PendingApproval`; confirmed appointment yalnızca işletme onayıyla oluşur

## Dilim 2.4 - İşletme Onay Paneli API'si

- BranchManager/BusinessOwner authz ve tenant membership scope kontrolü
- Pending request listesi
- Approve/decline endpoint'leri
- Approval audit, transactional email outbox ve `Superseded` kapanışları
- TTL expiry scheduler için explicit tenant scope

## Kapanış Kriterleri

- Public endpoint'ler tenant verisi sızdırmadan çalışır.
- State değiştiren endpoint'lerde authn/authz/rate limit/idempotency değerlendirmesi yapılır.
- Booking create/approve/decline/expire uçtan uca integration test ile doğrulanır.
- Dokümanlar: ADR, yetki matrisi, veri envanteri ve açık sorular güncel kalır.
