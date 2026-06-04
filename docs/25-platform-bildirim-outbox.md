# Platform Bildirim Outbox'ı

Son güncelleme: 2026-06-04

## Amaç

Bu belge tenant'a bağlı olmayan abuse itirazı ve hesap kapatma bildirimlerinin güvenli,
PII-minimum ve retry edilebilir teslimat akışını tanımlar. Rezervasyon bildirimlerinin
tenant-scoped `TransactionalMessage` modeli değişmez; platform operasyonları ayrı
`PlatformTransactionalMessage` outbox'ını kullanır.

## Mimari Sınırlar

- Messaging outbox yalnızca `UserAccountId`, purpose, correlation id, unique delivery
  key, müşteri-güvenli konu/gövde ve teslimat metadata'sı taşır.
- Raw e-posta adresi Messaging tablosuna, response'a veya log'a yazılmaz.
- Alıcı adresini Identity içindeki `UserTransactionalEmailService` çözer ve sağlayıcıya
  gönderir; Identity bu adresi composition root'a geri döndürmez.
- Admin, Identity ve Messaging birbirlerinin tablolarına erişmez. Orchestration yalnızca
  API composition root içindeki `PlatformNotificationDispatchService` ile yapılır.
- Worker platform-global çalışır; tenant context veya tenant query-filter bypass'ı
  kullanmaz.

## Desteklenen Amaçlar

- `AccountClosureProposed`
- `AbuseAppealAccepted`
- `AbuseAppealRejected`

Yeni purpose eklenirken müşteri-güvenli içerik, delivery key, callback ihtiyacı, saklama
politikası ve PII etkisi birlikte değerlendirilir.

## Teslimat Durum Makinesi

```text
Pending -> Processing -> Sent
Pending -> Processing -> Pending
Pending -> Processing -> Failed
Pending/Processing -> Cancelled
```

- `DeliveryKey` unique index ve advisory transaction lock ile enqueue retry'ı tekilleşir.
- Aynı delivery key farklı immutable içerikle tekrar kullanılırsa sessizce mevcut mesaj
  döndürülmez; collision hata olarak reddedilir.
- Due kayıtlar `FOR UPDATE SKIP LOCKED` ile lease edilir.
- Lease süresi dolan `Processing` kayıtları başka worker tarafından tekrar alınabilir.
- Deneme sayısı, lock/retry zamanları ve terminal durum şekli DB check constraint'leriyle
  korunur.
- Sağlayıcı kabulü `SentAtUtc` alanına yazıldıktan sonra callback hatası oluşursa sonraki
  deneme e-postayı yeniden göndermez; yalnızca eksik callback ve finalization çalışır.
- Sağlayıcı kabulü ile `SentAtUtc` persistence'ı arasındaki dar hata aralığında SMTP
  idempotency desteği olmadığı için duplicate teslimat tamamen garantiyle önlenemez.

## Hesap Kapatma Güvenlik Akışı

1. Closure proposal Admin içinde oluşturulur ve API composition root delivery key'i
   tekil platform mesajını enqueue eder.
2. Worker, vaka hâlâ `PendingApproval` veya `Approved` ise e-postayı Identity üzerinden
   gönderir; terminal/reddedilmiş/itirazla iptal edilmiş vaka henüz gönderilmediyse mesaj
   `Cancelled` olur.
3. Sağlayıcı çağrısı başarıyla döndüğünde kabul zamanı `SentAtUtc` olarak kalıcılaştırılır.
4. Admin callback aynı zamanı `CustomerNoticeDeliveredAtUtc` olarak kaydeder ve
   `EligibleForExecutionAtUtc = deliveredAt + ClosureAppealWindowDays` hesaplar.
5. Callback başarısız olursa retry e-postayı tekrar göndermeden Admin kaydını tamamlar.
6. `CustomerNoticeDeliveredAtUtc` ve `EligibleForExecutionAtUtc` yoksa closure execution
   `ACCOUNT_CLOSURE_NOTICE_NOT_DELIVERED` ile bloklanır.

Buradaki “teslim” SMTP/sağlayıcı kabulünü ifade eder; müşterinin inbox'ına kesin ulaşım
garantisi değildir. Uzun süre başarısız kayıtlar reconciliation tarafından görünür
kılınır; bounce/webhook semantiği ayrı bir sağlayıcı entegrasyonu kararıdır.

## Konfigürasyon

`Messaging:PlatformNotificationWorker`:

- `Enabled`: worker çalışma kapısı
- `InitialDelay`, `Interval`: tarama zamanlaması
- `BatchSize`: tek turda lease edilen kayıt sınırı
- `LockDuration`: worker lease süresi
- `RetryDelay`, `MaxAttempts`: sınırlı retry politikası

Production closure execution ayrıca
`Admin:AbuseRisk:AccountClosureExecutionEnabled=true` olmadan çalışmaz. Bu kapı gerçek
SMTP teslimatı, alarm/reconciliation ve operasyon runbook'u doğrulanmadan açılmaz.

## Operasyon ve İzleme

İzlenmesi gereken minimum sinyaller:

- `Failed` platform mesajı sayısı ve purpose dağılımı
- uzun süre `Processing` kalan veya lease'i sürekli düşen mesajlar
- `SentAtUtc` dolu fakat terminal `Sent` olmayan callback retry kayıtları
- notification bekleyen closure case yaşı
- `Executing` durumunda uzun süre kalan closure case'ler

Manuel müdahale doğrudan tablo düzenleyerek yapılmaz. Retry/reconciliation servisleri
idempotent state geçişlerini kullanmalıdır.

Uygulanan reconciliation yüzeyleri:

- `PlatformOperationsReconciliationHostedService`: eşikleri periyodik kontrol eder ve
  yalnızca sayı + kayıt GUID'i içeren yapılandırılmış warning/error/critical log üretir.
- `GET /health`: operasyon kontrolünü bilinçli olarak dışarıda bırakan temel health yüzeyi.
- `GET /health/operations`: failed notification veya stalled execution için unhealthy,
  stale processing/callback pending/notification overdue için degraded döner ve operasyon
  rate limit'i ile korunur.
- `GET /api/admin/operations/reconciliation`: `PlatformAdminWithStepUp` ve admin operations
  rate limit'i altında safe count ve örnek kayıt GUID'leri döndürür.

Reconciliation hiçbir state'i otomatik değiştirmez. Eşikler
`Operations:Reconciliation` konfigürasyonu ile yönetilir; kurtarma akışı
`26-platform-operasyon-reconciliation-runbook.md` içinde tanımlıdır.

Migration sırasında eski `PendingApproval`/`Approved` vakaların önceki eligibility
zamanı temizlenir; bu vakalar yeni bildirim kanıtı oluşmadan execute edilemez. Daha önce
`Executing`/`Executed` olmuş kayıtlar tarihsel durumlarını korumak için legacy kabul
kanıtıyla taşınır.

## Doğrulanan Senaryolar

- Aynı delivery key ile enqueue retry tek kayıt üretir.
- Provider kabulü kalıcılaştıktan sonra callback retry e-postayı yeniden göndermez.
- Closure proposal bildirimi kabul edilmeden execution ilerlemez.
- Kabul zamanı kaydedildiğinde itiraz penceresi bu zamandan başlar.
- Platform outbox migration seed'i mesaj, kullanıcı veya operasyon verisi üretmez.
