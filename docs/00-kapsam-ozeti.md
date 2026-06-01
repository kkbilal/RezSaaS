# Kapsam Özeti (RezSaaS)

## Ürün tanımı

RezSaaS; kuaför/berber, nail, spa, kaş-kirpik, makyaj, tattoo/piercing, sağlıklı yaşam vb. kategorilere genişleyebilecek şekilde tasarlanmış **salon operasyon ve rezervasyon SaaS**’idir.

Ürün iki ana katmandan oluşur:

1) **Müşteri tarafı keşif + rezervasyon**: anonim keşif, işletme sayfası, uygun slot bulma, rezervasyon oluşturma  
2) **İşletme tarafı operasyon paneli**: takvim, personel, hizmet kataloğu, kaynak/koltuk yönetimi, iptal/no-show, mesajlaşma ve raporlama

## Stratejik farklılaşma (çekirdek hipotez)

Rakiplerin pazarlama ve paketlerinde genelde “koltuk/oda/yatak/istasyon” gibi **fiziksel kaynağı** birinci sınıf bir planlama kavramı olarak öne çıkarmadığı görülüyor. RezSaaS’in çekirdeği “berber koltuğu” değil; spa odası, nail desk, cihaz istasyonu gibi genişleyebilen **generic resource scheduling** modeli olmalı.

## MVP’de hedeflenen değer

- İşletmeler: hızlı kurulum + tek yerden operasyon (çoklu şube/personel/kaynak)
- Müşteriler: güvenilir keşif + net fiyat/hizmet + uygun zaman bulma + kolay rezervasyon

## MVP dışı (ilk fazlarda özellikle kaçınma)

- Pazaryeri komisyonu/lead-fee’yi ana gelir motoru yapmak
- Mikroservis mimarisi ile erken parçalanma
- “Her şeyi yapan” dev ilk sürüm (fazlandırılmış teslimat yerine)

## Çekirdek kavramlar (omurga)

Veri ve domain omurgası, personel ve koltuk etrafında daraltılmaz:

- `Tenant → Business → Branch`
- `Branch → ResourceType → Resource`
- `Branch → StaffMember ↔ Skill`
- `Service → ServiceVariant → Skill/ResourceType gereksinimi`
- `AvailabilityRule → StaffMember/Resource/Branch`
- `AppointmentRequest → Appointment`

## Varsayılan teknoloji yönü (taslak)

- Backend: **ASP.NET Core Web API (.NET 10 LTS)**
- DB: **PostgreSQL** (çakışma önleme için range type + exclusion constraint yaklaşımına uygun)
- Mimari: **Modüler monolith** (Identity, Booking, Catalog, Resources, Messaging, Payments, Analytics…)
- Frontend: React tabanlı modern yaklaşım; public keşif sayfalarında SEO için SSR/SSG zorunluluğu mimari tasarımda korunacak

## Doğrulanmış kararlar

- Ürün konumlandırması: **tek domain altında keşif** + işletme sayfaları (işletmeler sayfalarını müşterileriyle paylaşır).
- İlk hedef: **multi-category** (dil, şablonlar ve model buna göre).
- Ödeme: ilk sürümde **ödeme yok** (depozito/prepayment daha sonraki faz).
- Rezervasyon: müşteri isteği **işletme onayına** gider; onaylanınca randevu işlenir.
- İşletme onayı: randevu istekleri **24 saat** içinde yanıtlanmazsa zaman aşımına düşer.
- Müşteri auth: rezervasyon için **hesap şart**.
- Kaynak planlama: her randevu **tam olarak 1 staff + 1 resource** kullanır.
- Çoklu hizmet: MVP'de tüm hizmetler aynı staff ve resource üzerinde toplam süreli tek bloktur.
- Bildirim: MVP'de e-posta zorunlu, SMS sınırlı transactional kanal; WhatsApp sonraki faz pilotudur.

## MVP kapsamı

- Tek domain üzerinde anonim keşif ve işletme sayfası
- Müşteri hesabı, işletme hesabı ve işletme üyelikleri
- Şube, staff, resource, hizmet ve hizmet varyantı yönetimi
- Uygunluk hesaplama ve işletme onaylı rezervasyon isteği
- E-posta bildirimleri ve sınırlı SMS bildirimleri
- Temel spam önleme, audit log ve tenant izolasyonu testleri

## MVP dışı

- Online ödeme, depozito, iade ve chargeback
- WhatsApp üzerinden production bildirim akışı
- Paket/membership, stok, prim ve gelişmiş analitik
- Marketplace komisyonu, sponsorlu sıralama ve uluslararası açılım
