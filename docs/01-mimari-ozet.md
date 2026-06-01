# Mimari Özet (Taslak)

## Mimari yaklaşım

- **Modüler monolith**: tek deployment; domain sınırları net modüller.
- Başlangıç tenancy: **shared DB + tenant_id** + sıkı yetkilendirme/sorgu disiplini.
- İleride “büyük müşteri ayrı deployment” opsiyonunu açık bırakma.

## Önerilen modüller

- Identity
- Tenant Management
- Organization (Business, Branches, Staff)
- Catalog
- Resources
- Availability
- Booking
- Messaging (e-posta, SMS; WhatsApp sonraki faz)
- Reviews
- Admin
- Analytics (MVP sonrası)
- Payments (MVP sonrası)

## Veri modeli omurgası

- `Tenant → Business → Branch`
- `Branch → ResourceType → Resource`
- `Branch → StaffMember ↔ Skill`
- `Service → ServiceVariant → Skill/ResourceType gereksinimi`
- `AvailabilityRule → StaffMember/Resource/Branch`
- `AppointmentRequest → Appointment`

Notlar:

- `Title` (çırak/kalfa) unvan; bookability’yi `Skill/Capability` belirler.
- `Resource` generic olmalı (chair/room/bed/station).
- Bir hizmet varyantı seçilen staff'ın yetenekleri ve resource tipinin uygunluğu ile doğrulanır.

## DB düzeyi bütünlük

Rezervasyon çakışmaları yalnızca uygulama kodu ile değil, DB constraint'leri ile de engellenir:

- Aynı `staff_id` için çakışan kesinleşmiş zaman aralıkları engellenir.
- Aynı `resource_id` için çakışan kesinleşmiş zaman aralıkları engellenir.
- Bu iki kontrol birbirinden bağımsızdır; yalnızca `staff + resource` birleşimini kontrol etmek yeterli değildir.

## Rezervasyon akışı notu

MVP’de rezervasyon **işletme onaylı** çalışır: müşteri `AppointmentRequest` oluşturur (`PendingApproval`), işletme onaylayınca `Confirmed` olur (veya `Declined/Expired`).

## MVP rezervasyon kuralı

Her randevu **1 staff + 1 resource** ile planlanır (ikisi de zorunlu). Bu kural, özellikle kaynak planlama farklılaşması için çekirdek kabul edilir.

## Veri sahipliği

- Identity: kullanıcı, kimlik doğrulama, MFA ve hesap durumu
- Tenant Management: tenant, üyelik ve tenant kapsamı
- Organization: business, branch, staff ve çalışma bağlamı
- Catalog: service, variant, süre, fiyat ve yetenek gereksinimi
- Resources: resource type, resource ve kullanım dışı zamanlar
- Availability: çalışma saatleri, izinler ve uygunluk hesaplama
- Booking: request, appointment, durum geçişleri ve çakışma garantileri
- Messaging: kanal tercihi, şablon, gönderim ve teslimat kaydı
- Admin: abuse vakası, strike, yaptırım ve audit inceleme

## Zaman ve veri kuralları

- Veritabanında zaman değerleri UTC tutulur; işletme/şube timezone bilgisi ayrıca saklanır.
- Hizmet adı, süre ve fiyat gibi müşteri tarafından görülen değerler rezervasyon satırına snapshot olarak yazılır.
- Background job'lar tenant kapsamını açıkça taşır; implicit tenant context ile çalışmaz.
- PostgreSQL RLS, tenant izolasyonu için defense-in-depth olarak ayrıca değerlendirilebilir; uygulama katmanı yetkilendirmesinin yerine geçmez.
