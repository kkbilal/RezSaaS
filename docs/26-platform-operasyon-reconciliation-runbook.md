# Platform Operasyon Reconciliation Runbook

Son güncelleme: 2026-06-04

## Amaç ve Güvenlik Sınırı

Bu runbook platform-global bildirim outbox'ı ve hesap kapatma saga'sındaki yarım kalmış
operasyonları görünür kılar. Reconciliation **salt-okunurdur**; otomatik hesap kapatma,
terminal mesajı yeniden açma veya doğrudan tablo düzeltme yapmaz.

Log, health ve admin response'ları yalnızca incident sayıları ile operasyonel kayıt
GUID'lerini taşıyabilir. E-posta, `UserAccountId`, mesaj konu/gövdesi, customer notice,
internal reason, token veya secret bu yüzeylere eklenmez.

## İzlenen Sinyaller

| Sinyal | Varsayılan eşik | Seviye | Anlam |
|---|---:|---|---|
| Terminal `Failed` platform notification | Her kayıt | Critical | Sınırlı retry tüketildi; otomatik teslimat durdu |
| Stale `Processing` notification | Lease bitişi 10 dakikadan eski | Degraded | Worker lease'i zamanında tamamlanmadı |
| Callback/finalization pending notification | `SentAtUtc` 15 dakikadan eski | Degraded | Sağlayıcı kabulü var; callback veya terminal tamamlama bekliyor |
| Notification overdue closure case | Proposal 30 dakikadan eski | Degraded | Aktif closure case için teslim kanıtı oluşmadı |
| Stalled `Executing` closure case | Execution başlangıcı 15 dakikadan eski | Critical | Cross-module closure saga tamamlanamadı |

Eşikler `Operations:Reconciliation` bölümünden değiştirilir. Eşik değişikliği gerçek
operasyon gözlemine dayanmalı; incident'i görünmez kılmak için yükseltilmemelidir.

## İzleme Yüzeyleri

- `GET /health`: operasyon reconciliation kontrolünü dışarıda bırakan temel health yüzeyidir.
- `GET /health/operations`: critical incident varsa `503`, yalnız degraded incident varsa
  degraded health sonucu döndürür ve operasyon rate limit'i ile korunur.
- `GET /api/admin/operations/reconciliation`: yalnızca `PlatformAdminWithStepUp` ve admin
  operations rate limit'i ile erişilen safe count + örnek kayıt GUID snapshot'ıdır.
- `PlatformOperationsReconciliationHostedService`: periyodik kontrol yapar ve PII-minimum
  yapılandırılmış log üretir.

Default health'in sağlıklı olması operasyon health'inin de sağlıklı olduğu anlamına gelmez.
Deploy/liveness ile operasyon alarmı ayrı değerlendirilir.

## İlk Müdahale

1. `/health/operations` ve step-up admin reconciliation snapshot'ı ile incident türünü doğrula.
2. İlgili kayıt GUID'lerini, worker/dependency loglarını ve son deploy/config değişikliklerini
   korele et; PII'yi log veya ticket'a kopyalama.
3. Gerçek SMTP/Identity/Admin/Tenant Management bağımlılıklarının erişilebilirliğini doğrula.
4. Recovery öncesi incident sahibini ve uygulanan adımı operasyon kaydına yaz.
5. Aşağıdaki güvenli recovery yolunu kullan; SQL ile state değiştirme.

## Güvenli Recovery

### Terminal Failed Notification

- Sağlayıcı veya bağımlılık hatasını gider ve mesajın purpose/correlation GUID'ini doğrula.
- Terminal `Failed` kayıt mevcut worker tarafından yeniden claim edilmez.
- Kontrollü requeue yüzeyi henüz yoktur; doğrudan DB ile `Pending` durumuna döndürme yasaktır.
- Closure proposal bildirimi başarısızsa closure execution kapısını kapalı tut ve incident'i
  platform güvenlik sahibine yükselt.
- Auditli, step-up ve idempotent terminal requeue yüzeyi ayrı ADR/özellik olarak
  tamamlanmadan manuel mutasyon yapılmaz.

### Stale Processing Notification

- Worker instance'ları, DB bağlantısı ve lease süresini kontrol et.
- Süresi dolmuş lease normal worker claim akışı tarafından güvenli biçimde geri alınır.
- Manuel lock temizleme veya status değiştirme yapma; tekrar eden lease düşüşünü incident
  olarak incele.

### Callback/Finalization Pending Notification

- Admin callback bağımlılığını ve closure case durumunu doğrula.
- Worker aynı mesajı tekrar claim ettiğinde `SentAtUtc` bulunduğu için e-postayı yeniden
  göndermez; yalnız callback/finalization adımını tekrarlar.
- Mesaj terminal `Failed` durumuna geçtiyse terminal failed prosedürünü uygula.

### Notification Overdue Closure Case

- Aynı closure case correlation GUID'i için outbox kaydı olup olmadığını admin snapshot ve
  kontrollü operasyon incelemesiyle doğrula.
- Outbox kaydı yoksa aynı proposer ve aynı içerikle mevcut idempotent proposal endpoint'ini
  tekrar çalıştır; bu eksik enqueue adımını tamamlar.
- Outbox kaydı pending/processing ise worker ve bağımlılıkları düzelt; failed ise terminal
  failed prosedürüne geç.
- Teslim kanıtı oluşmadan closure execute etme.

### Stalled Executing Closure Case

- Identity hesap durumunu, aktif tenant membership uygunluğunu, açık appeal durumunu ve
  güncel risk kanıtını doğrula.
- `PlatformAdminWithStepUp` ile
  `POST /api/admin/abuse/closure-cases/{closureCaseId}/execute` isteğini tekrar çalıştır.
- Execute akışı idempotent saga completion olarak tasarlanmıştır; doğrudan Admin/Identity
  tablo düzeltmesi yapma.
- Retry tekrar başarısızsa genel incident sürecine yükselt ve closure execution safety gate'ini
  kapalı tut.

## Escalation ve Kapanış

- Her critical incident derhal platform güvenlik/operasyon sahibine yükseltilir.
- Degraded incident bir reconciliation interval'ından uzun sürerse sahip atanır; eşik
  süresince kendiliğinden kaybolması kapanış kanıtı sayılmaz.
- Incident; root cause, kullanılan idempotent recovery yolu, etkilenen kayıt GUID'leri,
  doğrulanan son durum ve takip işi kaydedilmeden kapatılmaz.
- Gerçek SMTP alarm routing'i, backup/restore tatbikatı ve genel incident runbook
  doğrulanmadan `Admin:AbuseRisk:AccountClosureExecutionEnabled=true` yapılmaz.
