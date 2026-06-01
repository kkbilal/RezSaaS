# Mimari Özet (Taslak)

## Mimari yaklaşım

- **Modüler monolith**: tek deployment; domain sınırları net modüller.
- Başlangıç tenancy: **shared DB + tenant_id** + sıkı yetkilendirme/sorgu disiplini.
- İleride “büyük müşteri ayrı deployment” opsiyonunu açık bırakma.

## Önerilen modüller

- Identity
- Tenant Management
- Branches
- Catalog
- Resources
- Availability
- Booking
- Messaging (SMS/e-posta)
- Payments
- Reviews
- Analytics
- Admin

## Veri modeli omurgası

**Tenant → Business → Branch → ResourceType → Resource → StaffMember → Skill → Service → ServiceVariant → AvailabilityRule → Appointment**

Notlar:

- `Title` (çırak/kalfa) unvan; bookability’yi `Skill/Capability` belirler.
- `Resource` generic olmalı (chair/room/bed/station).

## DB düzeyi bütünlük

Rezervasyon çakışmalarının yalnızca uygulama kodu ile değil, DB constraint’leri ile de engellenmesi hedeflenir.

## Rezervasyon akışı notu

MVP’de rezervasyon **işletme onaylı** çalışır: müşteri `AppointmentRequest` oluşturur (`PendingApproval`), işletme onaylayınca `Confirmed` olur (veya `Declined/Expired`).

## MVP rezervasyon kuralı

Her randevu **1 staff + 1 resource** ile planlanır (ikisi de zorunlu). Bu kural, özellikle kaynak planlama farklılaşması için çekirdek kabul edilir.
