# İşletme Abuse Raporu, Strike ve Risk

## Amaç

İşletme abuse raporu akışı; yetkili işletme kullanıcısının belirli bir `AppointmentRequest` için kötüye kullanım şüphesini platform incelemesine taşımasını sağlar. İşletme bildirimi yalnızca bir sinyaldir; müşteriye doğrudan strike, sanction veya hesap kapatma uygulanmaz.

## İşletme Yüzeyi

- `POST /api/business/appointment-requests/{appointmentRequestId}/abuse-reports`
- Tenant header, authenticated kullanıcı ve aktif tenant zorunludur.
- `BusinessOwner` tenant kapsamındaki talepleri, `BranchManager` yalnızca yetkili olduğu branch taleplerini raporlayabilir.
- `Staff` varsayılan olarak rapor oluşturamaz.
- Aynı tenant ve appointment request için yalnızca bir rapor tutulur. Retry mevcut raporu döndürür ve yeni kayıt üretmez.
- Raporlayan kullanıcı için tenant başına kayan 24 saatlik rapor limiti uygulanır.
- Report create; appointment request ve raporlayan actor kapsamlı PostgreSQL advisory transaction lock ile yarış koşuluna karşı korunur.

## Reason Code Taksonomisi

- `SlotSpam`
- `RepeatedCancellation`
- `NoShowPattern`
- `SuspectedAutomation`
- `AbusiveBehavior`
- `Other`

Reason code operasyonel sınıflandırmadır. Serbest metin `Note` alanı opsiyonel ve en fazla 300 karakterdir; PII, secret, token veya erişim bilgisi içermemelidir.

## Platform İnceleme Yüzeyi

Tüm endpoint'ler `PlatformAdminWithStepUp` ve admin operasyon rate limit'i ister:

- `GET /api/admin/abuse/reports`
- `POST /api/admin/abuse/reports/{reportId}/confirm`
- `POST /api/admin/abuse/reports/{reportId}/dismiss`
- `POST /api/admin/abuse/users/{userAccountId}/strikes/{strikeId}/revoke`

Rapor durumları:

- `PendingReview`: işletme sinyali alınmış, platform kararı verilmemiştir.
- `Confirmed`: platform incelemesi sinyali doğrulamış ve tek bir süreli strike üretmiştir.
- `Dismissed`: sinyal doğrulanmamış, strike üretilmemiştir.

Review kararı PostgreSQL row lock ile korunur. Aynı karar retry edildiğinde idempotent cevap döner; terminal kararı farklı bir karara çevirmeye çalışan istek conflict olur.

## Strike ve Risk Kuralları

- Her confirmed rapor en fazla bir `UserStrike` üretir.
- Strike tenant kaynağını taşır ancak risk hesabı platform-global kullanıcı hesabı üzerinde yapılır.
- Strike süreli, auditli ve geri alınabilirdir.
- Strike revoke edildiğinde tarihsel kayıt silinmez.
- Aktif strike; revoke edilmemiş ve expiry zamanı geçmemiş strike'tır.
- Risk seviyeleri aktif strike sayısından hesaplanır: `Normal`, `Monitor`, `Elevated`, `High`.
- Risk seviyesi yalnızca operasyon önerisidir; otomatik warning, cooldown, temporary ban veya permanent closure uygulamaz.
- Yaptırım gerekiyorsa platform admin mevcut sanction control-plane üzerinden ayrı, gerekçeli ve step-up korumalı karar verir.

Varsayılan operasyon ayarları `Admin:AbuseRisk` altında yapılandırılır:

- strike yaşam süresi: 90 gün
- elevated eşik: 2 aktif strike
- high eşik: 3 aktif strike
- raporlayan actor için tenant başına günlük limit: 20

## Veri, Audit ve İzolasyon

- `BusinessAbuseReport` tenant-scoped sinyaldir; business yüzeyinde yalnızca doğrulanmış tenant ve appointment request bağlamıyla oluşturulur.
- Admin modülü global inceleme ihtiyacı nedeniyle query filter kullanmaz; business komutunda explicit tenant zorunludur, platform listeleme yalnızca step-up admin yüzeyindedir.
- Rapor oluşturma, confirm/dismiss ve strike revoke aksiyonları audit kaydı üretir.
- İşletme rapor cevabı serbest metin note alanını döndürmez.
- Note yalnızca step-up platform admin inceleme yüzeyinde görünür.
- Abuse event detaylarına note veya başka serbest metin eklenmez.
- Migration seed'iyle rapor, strike, kullanıcı veya operasyon verisi üretilmez.

## Açık İşler

- Appeal inceleme SLA'sı ve operasyon dashboard'u
- Reason code taksonomisi için operasyon runbook'u
- No-show sinyalinin appointment operasyon akışıyla otomatik fakat insan incelemeli bağlanması
- İşletme raporlama davranışının yanlış/kötüye kullanım risk skoru
