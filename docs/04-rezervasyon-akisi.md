# Rezervasyon Akışı (İşletme Onaylı) — Taslak

## Durumlar (state machine)

- `Draft`: istemci tarafında seçim yapılıyor; veritabanında rezervasyon kaydı olmak zorunda değil
- `PendingApproval`: randevu isteği işletmeye iletildi
- `Confirmed`: işletme onayladı, randevu kesinleşti
- `Declined`: işletme reddetti
- `Expired`: istek zaman aşımı ile kapandı
- `CancelledByCustomer`
- `CancelledByBusiness`
- `Completed`
- `NoShow`

## Temel kurallar

- Çakışma engeli: kesinleşmiş randevularda hem aynı `staff + time range` hem aynı `resource + time range` ayrı ayrı çakışamaz.
- MVP kuralı: her rezervasyon **1 staff + 1 resource** ile planlanır (ikisi de zorunlu).
- `PendingApproval` süresince slot **bloklanmaz**: aynı slot için birden fazla istek oluşabilir; işletme birini seçer.
- `PendingApproval` için üst zaman aşımı (TTL): **1 gün (24 saat)**.
- Gerçek zaman aşımı `min(createdAt + 24 saat, appointmentStart - responseBuffer)` olarak hesaplanır. Geçmiş veya cevaplanamayacak kadar yakın randevu isteği oluşturulmaz.
- İşletme, TTL dolmadan onay/ret verebilir; TTL dolduktan sonra istek kapanmış sayılır (onaylanamaz).
- İşletme onayı transaction içinde çalışır; DB constraint çakışması varsa onay reddedilir.
- İşletme bir isteği onayladığında, artık karşılanamayacak çakışan `PendingApproval` istekleri `Superseded` gerekçesiyle kapatılır.
- `Confirmed` olmayan randevularda ödeme yok; bildirim sadece transactional seviyede başlar.

## Durum geçişleri

| Başlangıç | Aksiyon | Sonuç |
| --- | --- | --- |
| `PendingApproval` | İşletme onaylar, çakışma yok | `Confirmed` |
| `PendingApproval` | İşletme reddeder | `Declined` |
| `PendingApproval` | TTL dolar | `Expired` |
| `Confirmed` | Müşteri iptal eder | `CancelledByCustomer` |
| `Confirmed` | İşletme iptal eder | `CancelledByBusiness` |
| `Confirmed` | Hizmet tamamlanır | `Completed` |
| `Confirmed` | Müşteri gelmez | `NoShow` |

Her komut idempotent olmalı ve actor, zaman, gerekçe ile auditlenmelidir.

## Multi-service randevu

Bir randevu “çoklu hizmet” içerebilir. Her hizmetin süresi farklı olabileceği için iki yaklaşım değerlendirilecek:

1. `AppointmentRequestLine` ve `AppointmentLine` altında `serviceVariant`, süre ve fiyat snapshot tutulur.
2. Toplam süre satırların toplamı ve tanımlı buffer sürelerinden hesaplanır.
3. Daha ileri aşamada segment modeli değerlendirilebilir; MVP'de her satır aynı staff ve resource kullanır.

MVP hedefi: çoklu hizmeti desteklerken, zamanlama karmaşıklığını kontrollü tutmak.
