# Rezervasyon Akışı (İşletme Onaylı) — Taslak

## Durumlar (state machine)

- `Draft`: istemci tarafında seçim yapılıyor; veritabanında rezervasyon kaydı olmak zorunda değil
- `PendingApproval`: randevu isteği işletmeye iletildi
- `Approved`: işletme isteği seçti; bu request kapanır ve ayrı bir `Appointment` `Confirmed` olarak oluşur
- `Confirmed`: işletme onayladı, randevu kesinleşti
- `Declined`: işletme reddetti
- `Expired`: istek zaman aşımı ile kapandı
- `Superseded`: aynı staff veya resource zaman aralığı artık confirmed appointment ile karşılanamadığı için kapandı
- `CancelledByCustomer`
- `Cancelled`: işletme confirmed appointment'ı iptal etti
- `Completed`
- `NoShow`
- `Rebooked`: eski appointment yeni bir `Confirmed` appointment'a taşındı

## Temel kurallar

- Çakışma engeli: kesinleşmiş randevularda hem aynı `staff + time range` hem aynı `resource + time range` ayrı ayrı çakışamaz.
- MVP kuralı: her rezervasyon **1 staff + 1 resource** ile planlanır (ikisi de zorunlu).
- `PendingApproval` süresince slot **bloklanmaz**: aynı slot için birden fazla istek oluşabilir; işletme birini seçer.
- `PendingApproval` için üst zaman aşımı (TTL): **1 gün (24 saat)**.
- Gerçek zaman aşımı `min(createdAt + 24 saat, appointmentStart - responseBuffer)` olarak hesaplanır. Geçmiş veya cevaplanamayacak kadar yakın randevu isteği oluşturulmaz.
- İşletme, TTL dolmadan onay/ret verebilir; TTL dolduktan sonra istek kapanmış sayılır (onaylanamaz).
- İşletme onayı transaction içinde çalışır; DB constraint çakışması varsa onay reddedilir.
- İşletme bir isteği onayladığında, artık karşılanamayacak çakışan `PendingApproval` istekleri `Superseded` gerekçesiyle kapatılır.
- Business appointment operasyonları tenant header + authenticated user + membership authz ister; komutlar `Idempotency-Key` destekler.
- `Complete` yalnız appointment end zamanından sonra, `NoShow` yalnız appointment start zamanından sonra uygulanır.
- Rebook eski appointment'ı `Rebooked` yapar, yeni `Confirmed` appointment üretir ve staff/resource conflict kontrolünü tekrar çalıştırır.
- `Confirmed` olmayan randevularda ödeme yok; bildirim sadece transactional seviyede başlar.

## Durum geçişleri

| Başlangıç | Aksiyon | Sonuç |
| --- | --- | --- |
| `PendingApproval` | İşletme onaylar, çakışma yok | Request `Approved`, Appointment `Confirmed` |
| `PendingApproval` | İşletme reddeder | `Declined` |
| `PendingApproval` | TTL dolar | `Expired` |
| `Confirmed` | Müşteri iptal eder | `CancelledByCustomer` |
| `Confirmed` | İşletme iptal eder | `Cancelled` |
| `Confirmed` | Hizmet tamamlanır | `Completed` |
| `Confirmed` | Müşteri gelmez | `NoShow` |
| `Confirmed` | İşletme yeni zamana taşır | Eski appointment `Rebooked`, yeni appointment `Confirmed` |

Her komut idempotent olmalı ve actor, zaman, gerekçe ile auditlenmelidir.

## Multi-service randevu

Bir randevu “çoklu hizmet” içerebilir. Her hizmetin süresi farklı olabileceği için iki yaklaşım değerlendirilecek:

1. `AppointmentRequestLine` ve `AppointmentLine` altında `serviceVariant`, süre ve fiyat snapshot tutulur.
2. Toplam süre satırların toplamı ve tanımlı buffer sürelerinden hesaplanır.
3. Daha ileri aşamada segment modeli değerlendirilebilir; MVP'de her satır aynı staff ve resource kullanır.

MVP hedefi: çoklu hizmeti desteklerken, zamanlama karmaşıklığını kontrollü tutmak.
