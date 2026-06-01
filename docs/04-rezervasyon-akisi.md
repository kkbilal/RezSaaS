# Rezervasyon Akışı (İşletme Onaylı) — Taslak

## Durumlar (state machine)

- `Draft`: müşteri seçim yapıyor (hizmet/variant/personel vs.)
- `Held`: slot geçici tutuldu (hold/TTL ile)
- `PendingApproval`: randevu isteği işletmeye iletildi
- `Confirmed`: işletme onayladı, randevu kesinleşti
- `Declined`: işletme reddetti
- `Expired`: hold/istek zaman aşımı ile düştü
- `CancelledByCustomer`
- `CancelledByBusiness`
- `Completed`
- `NoShow`

## Temel kurallar

- Çakışma engeli: aynı `staff + resource + time range` (veya seçilen uygunluk modeli) çakışamaz.
- MVP kuralı: her rezervasyon **1 staff + 1 resource** ile planlanır (ikisi de zorunlu).
- `PendingApproval` süresince slot **bloklanmaz**: aynı slot için birden fazla istek oluşabilir; işletme birini seçer.
- `PendingApproval` için zaman aşımı (TTL): **1 gün (24 saat)**. Süre dolunca istek `Expired` olur.
- İşletme, TTL dolmadan onay/ret verebilir; TTL dolduktan sonra istek kapanmış sayılır (onaylanamaz).
- İşletme onayı sırasında “çakışma” DB/transaction seviyesinde tekrar doğrulanır:
  - Onaylanan istek `Confirmed` olmaya çalışır.
  - Bu sırada aynı slot başka bir randevu ile dolmuşsa onay denemesi başarısız olur ve işletme yeniden seçim yapar.
- İşletme bir isteği onayladığında, aynı slot için kalan `PendingApproval` istekleri otomatik `Declined` (veya `Expired`) yapılır.
- `Confirmed` olmayan randevularda ödeme yok; bildirim sadece transactional seviyede başlar.

## Multi-service randevu

Bir randevu “çoklu hizmet” içerebilir. Her hizmetin süresi farklı olabileceği için iki yaklaşım değerlendirilecek:

1) `Appointment` altında `AppointmentLine` (her line: serviceVariant + duration) ve toplam süreyi line’ların toplamından üretmek  
2) Daha ileri aşamada “segment” modeli (her segment: staff/resource değişebilir) — MVP’de zorunlu değil

MVP hedefi: çoklu hizmeti desteklerken, zamanlama karmaşıklığını kontrollü tutmak.

Pratik MVP kuralı (öneri): aynı randevu içindeki hizmetlerin toplam süresi “tek blok” olarak ele alınır ve **aynı staff + aynı resource** için tek bir zaman aralığı rezerve edilir.
