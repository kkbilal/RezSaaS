# Abuse İtirazı ve Kalıcı Hesap Kapatma

## Amaç

Bu akış, yanlış pozitif strike/yaptırımlar için müşteriye kendi hesabı kapsamında güvenli bir itiraz yolu sağlar ve kalıcı hesap kapatmanın tek admin kararıyla veya otomatik risk skoru sonucuyla uygulanmasını engeller.

Kalıcı hesap kapatma; Admin, Identity ve Tenant Management modüllerinin API composition root içinde açıkça orkestre edildiği, auditli ve retry edilebilir bir operasyondur. Modüller birbirlerinin tablolarına erişmez.

## Domain Sınırları

- `AbuseAppeal`: müşterinin kendi `UserStrike`, aktif bloklayıcı `UserSanction` veya uygun `AccountClosureCase` kaydına yaptığı tekil itirazdır.
- `AccountClosureCase`: kalıcı hesap kapatma teklifini, bağımsız ikinci admin kararını, itiraz penceresini ve execution durumunu taşır.
- `UserAccount.Status=Closed`: Identity hesabının terminal durumudur.
- `UserSanctionType.PermanentClosure`: kapatma tamamlandıktan sonra Admin geçmişinde üretilen tarihsel yaptırım kaydıdır; normal sanction endpoint'i bunu üretemez.

## İtiraz Akışı

Müşteri yüzeyi authenticated aktif hesap ve kullanıcı+IP kapsamlı rate limit ister:

- `GET /api/customer/abuse/overview`
- `GET /api/customer/abuse/appeals/{appealId}`
- `POST /api/customer/abuse/appeals`

Kurallar:

- Müşteri yalnızca kendi hedef kaydına itiraz edebilir; başka kullanıcının hedefi `404` kabul edilir.
- Aynı kullanıcı+hedef için tek itiraz tutulur; retry mevcut itirazı döndürür.
- Kullanıcı başına açık itiraz limiti `Admin:AbuseRisk:MaxOpenAppealsPerUser` ile yönetilir.
- `Executing` veya `Executed` hesap kapatma vakası varken yeni itiraz açılamaz.
- Kabul edilen strike itirazı strike'ı revoke eder.
- Kabul edilen aktif bloklayıcı sanction itirazı sanction'ı revoke eder.
- Kabul edilen hesap kapatma itirazı `PendingApproval` veya `Approved` vakayı `CancelledByAppeal` durumuna taşır.
- Reddedilen itiraz hedef kaydı değiştirmez.
- İtiraz sahibi kendi itirazını admin yetkisi taşısa bile review edemez.

Müşteri response'larında internal sanction nedeni, closure internal reason, review reason ve admin actor kimlikleri bulunmaz. Müşteri yalnızca güvenli `CustomerNotice`, durum ve kendi hedef referanslarını görür.

## Kalıcı Hesap Kapatma Ön Koşulları

Kapatma teklifi ve karar endpoint'lerinin tamamı `PlatformAdminWithStepUp` ve admin operasyon rate limit'i ister.

Bir teklif ancak aşağıdaki şartlarla oluşturulur:

- hedef aktif bir `UserAccount` olmalıdır;
- hedef herhangi bir platform rolü taşımamalıdır;
- hedef aktif tenant membership taşımamalıdır;
- hedefin aktif strike sayısı `High` risk eşiğine ulaşmalıdır;
- hedef için başka aktif closure case bulunmamalıdır;
- teklif eden admin hedef hesabın kendisi olmamalıdır.

Aynı teklif eden adminin aktif vakaya yönelik proposal retry'ı mevcut vakayı döndürür; farklı adminin paralel yeni teklif denemesi conflict olur.

Teklif eden admin kendi vakasını onaylayamaz. `Approved` kararı ikinci ve farklı bir `PlatformAdminWithStepUp` tarafından verilmelidir.

Execution için:

- vaka `Approved` olmalıdır;
- `Admin:AbuseRisk:ClosureAppealWindowDays` ile belirlenen pencere dolmuş olmalıdır; varsayılan 7 gündür;
- kullanıcıya ait açık `PendingReview` itiraz bulunmamalıdır;
- aktif strike sayısı execution anında hâlâ `High` risk eşiğini karşılamalıdır;
- platform rolü ve aktif tenant membership uygunluğu yeniden doğrulanmalıdır.

## Durum Makinesi

```text
PendingApproval -> Approved -> Executing -> Executed
PendingApproval -> Rejected
PendingApproval/Approved -> CancelledByAppeal
```

- `PendingApproval`: ikinci admin kararı beklenir.
- `Approved`: ikinci admin onayı alınmıştır; itiraz penceresi ve açık itiraz kontrolü devam eder.
- `Executing`: Admin tarafında execution kilidi alınmıştır; Identity kapatma veya tamamlayıcı Admin kaydı retry edilebilir.
- `Executed`: Identity hesabı kapatılmış ve `PermanentClosure` geçmiş kaydı üretilmiştir.
- `Rejected` ve `CancelledByAppeal`: terminaldir.

## Cross-module Orchestration

API composition root aşağıdaki sırayı uygular:

1. Admin closure case ve uygunluk bilgilerini okur.
2. Identity platform rolü ve hesap durumunu, Tenant Management aktif üyelik durumunu doğrular.
3. Admin transaction içinde vaka satırını ve kullanıcı kapsamlı advisory lock'ı alır; pencere/açık itiraz kontrolünden sonra vakayı `Executing` yapar.
4. Aktif tenant membership uygunluğu execution kilidinden sonra yeniden doğrulanır; aktif closure case taşıyan kullanıcıya control-plane üzerinden yeni tenant/üyelik verilmez.
5. Identity transaction içinde hesabı `Closed` yapar, security/concurrency stamp değerlerini döndürür ve Identity audit kaydı üretir.
6. Admin transaction içinde aktif bloklayıcı yaptırımları revoke eder, tek `PermanentClosure` geçmiş kaydı üretir ve vakayı `Executed` yapar.

`Executing` durumu bilinçli bir saga ara durumudur. Identity kapanıp Admin tamamlama başarısız olursa aynı execute isteği güvenli biçimde tekrar çalıştırılabilir. Operasyonel reconciliation/alert mekanizması sonraki sertleşme işidir.

## Production Açılış Kapıları

- Müşteriye closure proposal ve appeal review sonucu için zorunlu transactional e-posta henüz platform-global Messaging outbox modeliyle bağlanmamıştır.
- İtiraz penceresinin proposal, outbox kuyruğu veya doğrulanmış teslimat zamanından hangisiyle başlayacağı kararlaştırılmadan production closure execution açılmaz.
- `Admin:AbuseRisk:AccountClosureExecutionEnabled` güvenli varsayılan olarak `false` değerindedir; Development/test dışında ancak bu kapılar tamamlandıktan sonra bilinçli olarak açılır.
- Bildirim teslimat hatası, uzun süre `Executing` kalan vaka ve cross-module saga yarım kalması için reconciliation/alert runbook'u tamamlanmalıdır.

## Güvenlik, Audit ve Veri

- Kapatılmış veya suspended hesapların yeni login'i reddedilir.
- Her authenticated istekte aktif `UserAccount` kapısı çalışır; hesap kapatıldıktan sonra eski cookie/bearer token ile işlem yapılamaz.
- Proposal, review, execution start, execution complete, appeal create ve appeal review auditlenir.
- `InternalReason`, `CustomerNotice`, appeal statement ve review reason uzunluk sınırlıdır; appeal review reason revoke hedefleriyle uyumlu olarak en fazla 300 karakterdir. Metinler PII, secret, token veya erişim bilgisi içermemelidir.
- `InternalReason` yalnızca step-up admin yüzeyinde görünür.
- IP/device sinyali tek başına kalıcı hesap kapatma gerekçesi olamaz.
- Migration seed'i closure case, appeal, sanction, rol veya kullanıcı üretmez.

## Persistence ve Eşzamanlılık

- `AbuseAppeals` ve `AccountClosureCases` platform-global Admin tablolarıdır; tenant-scoped değildir.
- Aynı kullanıcı+hedef için itiraz unique index ile tekilleştirilir.
- Kullanıcı başına yalnızca bir aktif (`PendingApproval`, `Approved`, `Executing`) closure case bulunması partial unique index ile de korunur.
- Review ve execution state geçişleri PostgreSQL row lock ile korunur.
- Appeal create/review, strike revoke, sanction apply/revoke ve closure proposal/execution aynı kullanıcı kapsamlı advisory transaction lock ile sıralanır; risk kanıtı azaltılırken eski risk sayımıyla kapatma başlatılamaz ve closure tamamlanırken paralel ikinci bloklayıcı sanction üretilemez.
- DB check constraint'leri review/execution alanlarının durumla uyumunu ve self-proposal yasağını korur.

## Doğrulanan Senaryolar

- Başka kullanıcının yaptırımına itiraz `404` döner.
- Müşteri response'u internal nedenleri sızdırmaz.
- Kabul edilen itiraz hedef strike/sanction'ı revoke eder veya closure case'i iptal eder.
- Teklif eden admin kendi closure case'ini onaylayamaz.
- Aktif tenant üyeliği veya platform rolü closure teklifini engeller.
- İtiraz penceresi ve açık itiraz execution'ı engeller.
- Execution retry idempotent davranır, kalıcı yaptırım geçmişi üretir ve eski bearer token'ı geçersiz kılar.
