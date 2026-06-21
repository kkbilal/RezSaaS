# Phase 2 Uygulama Planı

> ⚠️ **SUPERSEDED (ADR-068, 2026-06-20):** Bu doküman artık güncel uygulama
> planı değildir. Phase 2 içeriği `roadmap/phase-2-musteri-kesif-rezervasyon-mvp.md`
> ve ilgili ADR'ler (özellikle ADR-027/030/031/032/033/034/035/036/037/038/039)
> tarafından kapsanır. Bu dosya yalnızca geçmiş referans için korunur; yeni
> çalışma `docs/roadmap/` altında yürütülür.

Phase 2 hedefi, gerçek kullanıcı alabilecek ilk uçtan uca müşteri akışını üretmektir: keşif → işletme profili → uygun slot → hesaplı rezervasyon isteği → işletme onayı.

## Başlangıç Kararları

- Public işletme URL yapısı `/isletme/{businessSlug}` olarak başlar.
- `Business.NormalizedSlug` tek domain altında global benzersizdir; tenant içinde tekrar eden slug kabul edilmez.
- Anonymous public discovery tenant header istemez; yalnızca açık public işletme verisini read-only servis üzerinden döndürür.
- SMS sağlayıcı seçimi maliyet nedeniyle Phase 2 lansman kapısı değildir; aktif bildirim kanalı e-postadır, SMS/WhatsApp altyapısı sonraki fazlara hazır kalır.

## Dilim 2.0 - Public Directory Temeli

Durum: tamamlandı.

- Tamamlandı: business public slug global benzersiz olacak şekilde Organization migration hazırlığı
- Tamamlandı: branch `city`, `district`, `addressLine` metadata alanları
- Tamamlandı: anonymous public business search/profile read service
- Tamamlandı: `/api/public/businesses` ve `/api/public/businesses/{slug}` endpoint yüzeyi
- Tamamlandı: public discovery IP bazlı rate limit policy
- Tamamlandı: business profil galeri, kurallar, puan ve SEO metadata alanları

## Dilim 2.1 - Public Profil Detayı

Durum: tamamlandı.

- Tamamlandı: Catalog read service ile aktif hizmet menüsü ve varyant fiyat/süreleri
- Tamamlandı: Organization read-only context ile aktif staff listesi
- Tamamlandı: Availability read service ile branch çalışma saatleri
- Tamamlandı: `/api/public/businesses/{slug}/profile` endpoint'i
- Tamamlandı: API composition root içinde public profile response contract
- Tamamlandı: staff gösterim politikası işletme ayarına bağlandı
- Tamamlandı: business profil galeri, işletme kuralları, puan/yorum özeti ve SEO metadata

## Dilim 2.2 - Slot Bulma

Durum: tamamlandı.

- Tamamlandı: seçilen service variant toplam süre hesabı
- Tamamlandı: staff tercihi optional; seçilmezse uygun staff adayları listelenir
- Tamamlandı: required resource type varsa sadece uyumlu resource adayları
- Tamamlandı: confirmed appointment, staff unavailable ve resource block kontrolleri
- Tamamlandı: slot response UTC zaman + branch timezone + local gösterim bilgisi döndürür
- Tamamlandı: `PendingApproval` talepler slot bulmada bloklayıcı sayılmaz
- Tamamlandı: staff skill/service required skill eşlemesi slot motoruna ve create doğrulamasına eklendi
- Tamamlandı: slot interval ve max slot ayarları branch public slot ayarı olarak modele alındı

## Dilim 2.3 - Rezervasyon İsteği Endpoint'i

Durum: tamamlandı.

- Tamamlandı: auth zorunlu müşteri request create endpoint'i
- Tamamlandı: `booking-appointment-requests` rate limit policy public slug + user + IP partition ile uygulanır
- Tamamlandı: tenant/branch/business eşlemesi public slug üzerinden doğrulanır
- Tamamlandı: staff, resource, service variant ve slot uygunluğu create öncesi doğrulanır
- Tamamlandı: kullanıcı pending/günlük limitleri ve abuse event üretimi application service içinde devrede kalır
- Tamamlandı: request sonucu `PendingApproval`; confirmed appointment yalnızca işletme onayıyla oluşur
- Tamamlandı: idempotency key davranışı hash saklama ile eklendi
- Tamamlandı: müşteri kendi taleplerini listeleme, detay görme ve pending talebi iptal endpoint'i

## Dilim 2.4 - İşletme Onay Paneli API'si

Durum: tamamlandı.

- Tamamlandı: BranchManager/BusinessOwner authz ve tenant membership scope kontrolü
- Tamamlandı: pending request listesi
- Tamamlandı: approve/decline endpoint'leri
- Tamamlandı: approval audit, transactional email outbox ve `Superseded` kapanışları application service üzerinden korunur
- Tamamlandı: business decision endpoint'leri için tenant + user + IP rate limit policy
- Tamamlandı: TTL expiry scheduler explicit active tenant enumerasyonu ve tenant context ile çalışır
- Tamamlandı: işletme panelinde request detay, müşteri bilgisi maskeleme ve operasyonel filtreler
- Tamamlandı: idempotency key davranışı approve/decline için API kontratına taşındı

## Kapanış Kriterleri

- Public endpoint'ler tenant verisi sızdırmadan çalışır.
- State değiştiren endpoint'lerde authn/authz/rate limit/idempotency değerlendirmesi yapılır.
- Booking create/approve/decline/expire uçtan uca integration test ile doğrulanır.
- Dokümanlar: ADR, yetki matrisi, veri envanteri ve açık sorular güncel kalır.
