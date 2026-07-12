# RezSaaS — Agent & Mimari Kuralları

Bu dosya; bu repoda çalışan insan/agent (Codex, Claude, vb.) için **mimari sınırlar, güvenlik minimumları, çalışma disiplini ve “mimarinin ezilmemesi”** için kuralları tanımlar.

> RezSaaS; tek domain altında çoklu işletme/şube/personel/kaynak destekleyen, **multi-category** salon rezervasyon + operasyon SaaS’idir. Rezervasyonlar **işletme onaylı** ilerler ve `PendingApproval` için üst sınır **24 saat TTL** vardır. MVP’de her randevu **1 staff + 1 resource** ile planlanır.

## 0) Normatif Dokümanlar

Kod değiştirmeden önce ilgili dokümanları oku:

- `docs/00-kapsam-ozeti.md`: ürün ve MVP sınırı
- `docs/04-rezervasyon-akisi.md`: booking state machine
- `docs/05-domain-sozlugu.md`: terimlerin tek anlamlı tanımı
- `docs/06-karar-kaydi.md`: ürün ve mimari karar günlüğü
- `docs/07-yetki-matrisi.md`: roller ve kapsamlar
- `docs/09-abuse-yaptirim-politikasi.md`: abuse kuralları
- `docs/17-tenant-management-temeli.md`: tenant management domain/persistence sınırı
- `docs/22-abuse-itiraz-hesap-kapatma.md`: itiraz ve kalıcı hesap kapatma güvenlik akışı

Çekirdek karar değişirse `docs/06-karar-kaydi.md` içine yeni ADR eklenmeden kod değiştirilmez.

---

## 1) Birinci Sınıf Öncelikler

1. **Doğruluk ve izolasyon**: tenancy izolasyonu (tenant sınırı) her şeyin üstünde.
2. **Rezervasyon tutarlılığı**: double-booking ve yarış koşulları (race) DB + uygulama düzeyinde engellenir.
3. **Güvenlik ve abuse dayanımı**: slot spam, brute-force, OTP maliyeti, log/PII sızıntısı “sonradan eklenmez”.
4. **Modüler monolith disiplinini koruma**: hızlı ilerleme = sınırları gevşetmek değil, sınırları netleştirmek.

---

## 2) Ürün Domain İlkeleri (Değiştirilmemesi Tercih Edilen “Çekirdek”)

### 2.1 Rezervasyon modeli (MVP)

- Rezervasyon isteği: `AppointmentRequest` (state: `PendingApproval`)
- İşletme onayı: `Appointment` (state: `Confirmed`) veya request `Declined`
- TTL: `PendingApproval` gerçek sonlanma zamanı `min(createdAt + 24 saat, appointmentStart - responseBuffer)` ile hesaplanır
- Slot davranışı: `PendingApproval` **slot bloklamaz**; işletme aynı slot için gelen birden fazla isteği arasından birini seçebilir.
- Zorunlu eşleme: her randevu **tam olarak 1 Staff + 1 Resource** ile planlanır.
- Multi-service: tek randevu içinde çoklu hizmet olabilir; MVP’de toplam süre “tek blok” olarak değerlendirilir ve aynı staff+resource ile planlanır.
- MVP'de kalıcı bir `Held` durumu yoktur. Teknik idempotency/transaction mekanizmasını kullanıcıya açık slot hold ile karıştırma.

### 2.2 “Resource” kavramı

`Resource` fiziksel kapasiteyi temsil eder ve geneldir:

- chair (koltuk), room (oda), bed (yatak), station (istasyon), device (cihaz) vb.

“Berber koltuğu”na sıkışmak yok; model her kategoriye genişleyebilir olmalı.

---

## 3) Mimari Yaklaşım

### 3.1 Modüler Monolith (zorunlu yaklaşım)

Mikroservis yok. Tek deployable uygulama; ancak **domain sınırları** net modüller halinde korunur.

Önerilen modüller:

- Identity
- Tenant Management
- Organization
- Catalog
- Resources
- Availability
- Booking
- Messaging (MVP: email + sınırlı transactional SMS; WhatsApp sonraki faz pilotu)
- Reviews
- Integrations (Phase 5: API client, webhook, CRM/export temeli; default kapalı)
- Payments (Phase 4: provider-agnostic, hosted checkout only, default kapalı)
- Analytics
- Admin (operasyon, denetim, abuse)

### 3.2 Modül sınırı kuralları (“mimarinin ezilmemesi”)

- Bir modül, başka modülün **veritabanı tablolarına doğrudan** yazamaz/okuyamaz (kontrat üzerinden erişir).
- “Kolay olsun” diye tüm entity’leri tek bir dev “Core” paketine yığmak yasak.
- Modüller arası iletişim:
  - Tercihen uygulama katmanı üzerinden açık arayüzler (interfaces / application services)
- Gerekirse domain event / integration event (sonraki fazlarda)
- Kural: Cross-module çağrıların hepsi **açık bağımlılık** olmalı; gizli/yan etki ile data manipülasyonu yapılmaz.
- Cross-module raporlama ihtiyacı için doğrudan write erişimi verilmez; gerekirse ayrı read model tasarlanır.

### 3.3 Katmanlama (önerilen)

- Domain: iş kuralları, invariants
- Application: use-case’ler, komut/sorgu, validation, authorization policy
- Infrastructure: EF Core, dış servisler (email/SMS/WhatsApp), cache
- API/UI: HTTP endpoints, DTO’lar, auth, rate limit, input constraints

---

## 4) Tenancy ve Veri İzolasyonu (Kritik)

### 4.1 Başlangıç modeli

- **Shared DB** + her tabloda `tenant_id`
- Uygulama katmanında tenant context (ör. `TenantId`) her istekte zorunlu
- Platform-global tablolar (`User` gibi) açıkça belgelenir; yanlışlıkla tenant-scoped veya global tablo üretilmez.

### 4.2 Uygulama içi zorunluluklar

- Varsayılan: tüm sorgular tenant filtreli.
- EF Core kullanılıyorsa:
  - Global query filter veya repository katmanında merkezi tenant filtresi
  - Tenant context yoksa tenant-scoped query varsayılan olarak veri döndürmemeli; açık admin/read-model bypass gerekiyorsa ADR ve test şarttır
  - “Tenant filtresi yok” olan sorgu **PR review’de reddedilir**
- Raw SQL gerekiyorsa:
  - `tenant_id` filtresi olmadan merge edilmez
  - Parametrik sorgu zorunlu (SQL injection önlemi)
- Background job, export ve admin operasyonları explicit `TenantId` taşır; implicit HTTP tenant context'e güvenmez.

### 4.3 Yetkilendirme

- RBAC (rol bazlı) + scope (branch gibi kapsam) ileride genişleyebilir.
- Tenant dışı kaynak erişimi: kaynağın varlığını sızdırmamak için `404`.
- Tenant içindeki yetersiz rol: `403`.
- Kritik aksiyonlar (rol yönetimi, ban, ödeme ayarları gibi) için `PlatformAdminWithStepUp` veya tenant-scope eşdeğeri policy zorunludur.
- Tenant/işletme yönetim endpoint'leri, privileged MFA/step-up ve tenant membership authz uygulanmadan yayınlanmaz.
- Anonymous public discovery gibi tenant header istemeyen read-only yüzeyler yalnızca ADR ile belgelenmiş explicit servis üzerinden query filter bypass edebilir; operational/admin sorgularında bu bypass kullanılamaz.
- Public profile gibi çok modüllü anonymous read contract'lar yalnızca API composition root içinde birleştirilir; doğrulanmış `businessSlug` ile tenant context geçici set edilir, işlem sonunda eski context restore edilir ve modül servisleri read-only kalır.
- Admin modülündeki tenant referanslı abuse raporları platform-global inceleme nedeniyle query filter kullanmayabilir; business oluşturma yolu explicit tenant + appointment request + branch authz doğrulamalı, global okuma yolu yalnızca `PlatformAdminWithStepUp` olmalıdır.

---

## 5) Rezervasyon Tutarlılığı ve Eşzamanlılık

### 5.1 Double booking engeli

- Aynı staff için çakışan kesinleşmiş zaman aralığı oluşmamalı.
- Aynı resource için çakışan kesinleşmiş zaman aralığı oluşmamalı.
- Bu kontroller iki ayrı invariant'tır; yalnızca `staff + resource` birleşimini kontrol etmek hatalıdır.
- Bu kural sadece application ile değil, DB constraint ile de desteklenmelidir (PostgreSQL hedefi).

### 5.2 İşletme onayı yarış koşulu

Slot bloklanmadığı için onay ekranında yarış olur:

- Onay işlemi transaction içinde çalışmalı.
- Onay anında çakışma tekrar kontrol edilmeli (DB ile).
- Çakışma varsa onay başarısız olmalı; işletme başka isteği seçebilmeli.
- Başarılı onaydan sonra artık karşılanamayan çakışan talepler `Superseded` gerekçesiyle kapanmalı.

### 5.3 Zaman ve snapshot kuralları

- DB zaman değerleri UTC saklanır; şube timezone bilgisi ayrıca tutulur.
- Clock doğrudan çağrılmaz; test edilebilir bir clock abstraction kullanılır.
- Rezervasyon satırları hizmet adı, süre ve fiyat snapshot'ı taşır.

### 5.4 Slot bulma kuralları

- Public slot bulma `PendingApproval` talepleri bloklayıcı kabul etmez; bu ürün kararını tersine çeviren kod ADR olmadan yazılmaz.
- Slot hesaplama branch working hours, selected service variant toplam süresi, optional staff tercihi, required resource type, confirmed appointment, staff unavailable ve resource block sinyallerini birlikte değerlendirir.
- Slot response UTC başlangıç/bitiş zamanını ve branch timezone/local gösterim bilgisini birlikte taşır.
- Public rezervasyon isteği oluşturma auth zorunludur; tenant header beklemez, tenant context doğrulanmış `businessSlug` üzerinden geçici set edilir ve request `PendingApproval` olarak kalır.
- Create öncesi staff/resource/variant/branch eşleşmesi ve slot uygunluğu doğrulanmadan `AppointmentRequest` üretilmez.
- Public slot ve create doğrulaması `ServiceRequiredSkill` + `StaffSkill` eşleşmesini zorunlu uygular; unvan veya display name bookability yerine kullanılamaz.
- Public booking create kontratında staff tercihi opsiyoneldir; staff seçilmezse API uygun staff atar.
- Public/customer yüzeylerinde resource GUID veya resource display name gösterilmez; resource seçimi internal kapasite atamasıdır.
- Internal resource ataması kullanıcıya açık slot hold değildir; persisted `AppointmentRequest` yine tam olarak `1 Staff + 1 Resource` taşır.
- Branch public slot ayarları (`SlotIntervalMinutes`, `MaxPublicSlots`) varsa config default'larını override eder; ayarlar pozitif değer olmak zorundadır.
- İşletme onay/ret endpoint'leri tenant header + authenticated user + tenant membership authz ister; `BusinessOwner` tenant-wide, `BranchManager` branch-scoped, `Staff` varsayılan deny.
- Approve/decline API'leri mevcut Booking application servislerini kullanır; audit, transactional outbox ve `Superseded` davranışı bypass edilmez.
- Booking create/approve/decline/customer-cancel komutları `Idempotency-Key` destekler; raw key asla saklanmaz, yalnızca tenant+actor+operation kapsamlı hash tutulur.
- Müşteri kendi request listesi/detayı/cancel akışı public business slug ile tenant context set ederek çalışır; başka kullanıcıya ait request `404` kabul edilir.
- Business panel appointment request response'larında müşteri e-posta/telefon bilgisi yalnızca maskelenmiş döner; raw PII response veya log'a eklenmez.
- Business appointment calendar/cancel/complete/no-show/rebook/note endpoint'leri tenant header + authenticated user + tenant membership authz ister; `BusinessOwner` tenant-wide, `BranchManager` branch-scoped çalışır.
- Business appointment cancel/complete/no-show/rebook/note komutları `Idempotency-Key` destekler; raw key saklanmaz, yalnızca tenant+actor+operation kapsamlı hash tutulur.
- `Complete` yalnız appointment end zamanından sonra, `NoShow` yalnız appointment start zamanından sonra uygulanır; future slotu erken boşaltan operasyon yazılmaz.
- Rebook eski appointment'ı `Rebooked` yapar ve yeni `Confirmed` appointment üretir; onaylı staff/resource çakışması uygulama ve DB düzeyinde tekrar kontrol edilir.
- Resource out-of-service/block komutları resource->branch doğrulaması ve tenant membership authz olmadan yayınlanmaz; public slot hesaplama resource block sinyalini kullanmaya devam eder.
- `PendingApproval` expiry worker aktif tenant'ları Tenant Management üzerinden enumerate eder ve her tenant için explicit `TenantId` set eder; implicit HTTP context'e güvenmez.

---

## 6) Güvenlik Minimumları (MVP’den itibaren)

### 6.1 Rate limiting ve brute-force önleme

Şu endpoint’ler için global + endpoint bazlı rate limit zorunlu:

- login/register
- password reset
- email/phone code send/verify (varsa)
- booking request create (slot spam)

### 6.2 PII ve log hijyeni

- Telefon/e-posta gibi PII loglarda maskelenir.
- OTP/verification token/log linkleri loglanmaz.
- “Debug kolaylığı” için PII açmak yasak.

### 6.3 Secrets yönetimi

- Repo içine secret/API key koymak yasak.
- Local dev: `.env`/user-secrets/KeyVault benzeri yaklaşım; prod: secret manager.

### 6.4 Audit log

En azından şu aksiyonlar auditlenir:

- rol/yetki değişimi
- işletme/şube ayar değişimi
- rezervasyon onay/ret/iptal
- ban/strike işlemleri

### 6.5 Web ve uygulama güvenliği

- State değiştiren endpoint'ler için CSRF/origin stratejisi belirlenmeden auth yaklaşımı tamamlanmış sayılmaz.
- Dosya yüklemelerinde tür, boyut, isim ve storage kuralları uygulanır.
- Audit log append-only tutulur; geçmiş kayıtlar uygulama üzerinden değiştirilemez.
- CI içinde secret taraması, dependency taraması ve temel statik analiz planlanır.

### 6.6 Identity ve rol sınırları

- Platform-global kullanıcı hesabı `Identity` modülündeki `UserAccount`'tır.
- `PlatformAdmin` ve `PlatformSupport` global platform rolleridir; `Identity` modülünde yönetilir.
- Global rol kayıtları migration seed verisi olarak gömülmez. İlk rol ve ayrıcalıklı hesap üretimi token-hash kontrollü, auditli bootstrap akışıyla yapılır.
- `Customer` bir global Identity rolü değildir; aktif ve doğrulanmış platform hesabının temel kullanım bağlamıdır.
- `BusinessOwner`, `BranchManager` ve `Staff` tenant üyelik rolleridir; global Identity rolü olarak eklenmez.
- Browser tabanlı istemciler cookie auth tercih eder. Bearer token yalnızca kontrollü istemciler ve gerekli entegrasyonlar için kullanılır.
- Production ortamında confirmed e-posta zorunludur. `Identity:DeliveryMode=Smtp` ve sağlayıcı secret'ları olmadan production API başlamaz.
- `DevelopmentSinkEmailSender` yalnızca local development/test içindir; token veya doğrulama linki loglamaz.
- Login/register/password reset yüzeyi IP bazlı rate limit ve Identity lockout ile korunur.
- Normal müşteri login akışı her girişte tek kullanımlık kod istemez. Ayrıcalıklı hesaplar için MFA/step-up ve güvenilir cihaz/oturum policy tamamlanmadan platform admin veya işletme yönetim endpoint'i yayınlanmaz.
- İlk `PlatformAdmin` bootstrap endpoint'i tek istisna olarak anonymous olabilir; bunun için token-hash doğrulama, rate limit, origin guard, audit ve "zaten admin varsa reddet" davranışı zorunludur. Bootstrap token/parola hiçbir response veya log içinde dönmez.
- Tenant provisioning endpoint'leri `PlatformAdminWithStepUp` policy olmadan yayınlanmaz; owner kullanıcı Identity içinde aktif `UserAccount` olarak doğrulanır, tenant rolü global Identity rolüne dönüştürülmez.
- Tenant membership yönetim endpoint'leri (add/suspend/revoke) `PlatformAdminWithStepUp` ister; hedef kullanıcı aktif `UserAccount` olmalı, `Revoked` terminal durum olmalı, son aktif `BusinessOwner` suspend/revoke edilemez ve her state değişimi auditlenir.
- Tenant lifecycle suspend/reactivate/close endpoint'leri `PlatformAdminWithStepUp`, uzunluk sınırlı ve PII/secret içermeyen operasyon nedeni, audit ve DB row lock ister. `Closed` terminaldir.
- `Suspended`/`Closed` tenant public discovery, slot arama, yeni booking request ve işletme operasyonlarına kapalıdır; müşteri kendi mevcut taleplerini görme ve izin verilen talebini iptal etme hakkını korur.
- Abuse event inceleme ve user sanction apply/revoke endpoint'leri `PlatformAdminWithStepUp` ister. Warning booking'i bloklamaz; cooldown en fazla 24 saat, temporary ban 24–72 saat olmalı ve aynı kullanıcıda yalnızca bir aktif bloklayıcı sanction bulunmalıdır.
- Sanction geçmişi silinmez; revoke actor+neden ile auditlenir. Aktif bloklayıcı sanction yeni booking request'i engeller fakat müşterinin mevcut taleplerini görme/iptal hakkını kaldırmaz.
- `PermanentClosure` sanction endpoint'inden uygulanmaz; yalnızca `High` risk kanıtı, iki farklı `PlatformAdminWithStepUp`, en az 7 günlük appeal penceresi, açık appeal bulunmaması, platform rolü ve aktif tenant membership taşımama kontrolleriyle yürütülen hesap kapatma workflow'u tamamlayabilir.
- Müşteri yalnızca kendi strike, aktif bloklayıcı sanction veya uygun closure case kaydına appeal açabilir; başka kullanıcı hedefi `404` kabul edilir.
- Customer abuse response'larına sanction/closure internal reason, review reason veya admin actor kimliği eklenmez; güvenli müşteri metni `CustomerNotice` olarak ayrı tutulur.
- `AccountClosureCase.Executing` retry edilebilir saga ara durumudur. Identity kapatma başarıp Admin completion başarısız olursa execute retry tamamlamalı; doğrudan tablo düzeltmesi yapılmamalıdır.
- Aktif closure case taşıyan kullanıcıya tenant/aktif membership atanamaz; execution, Identity kapatmadan hemen önce aktif tenant membership uygunluğunu yeniden doğrular.
- Closure execution anında aktif strike sayısı yeniden hesaplanır; risk artık `High` değilse kalıcı kapatma ilerleyemez.
- Platform-global transactional bildirim outbox'ı yalnızca `UserAccountId` taşır; raw e-posta adresi Messaging tablosuna, response'a veya log'a yazılmaz. Alıcı adresini yalnızca Identity çözer.
- Hesap kapatma itiraz penceresi, zorunlu proposal e-postasının sağlayıcı tarafından kabul edildiği `CustomerNoticeDeliveredAtUtc` anında başlar. Bu kanıt ve `EligibleForExecutionAtUtc` oluşmadan closure execution ilerleyemez.
- Sağlayıcı kabulü kaydedildikten sonra Admin callback'i başarısız olursa retry aynı e-postayı yeniden göndermez; yalnızca eksik callback/finalization adımını tamamlar.
- Platform notification worker tenant context kullanmayan explicit global bir worker'dır; unique delivery key, row lock, lease, sınırlı retry ve terminal durum koruması olmadan yeni platform bildirimi eklenmez.
- Platform operasyon reconciliation varsayılan olarak salt-okunurdur; alarm veya health check geri döndürülemez state'i otomatik onaramaz ve doğrudan tablo mutasyonu yapamaz.
- Reconciliation log, health ve admin snapshot'ları PII, mesaj konu/gövdesi, internal reason veya `UserAccountId` sızdıramaz; yalnızca sayılar ve operasyonel kayıt GUID'leri taşıyabilir.
- Reconciliation recovery mevcut idempotent application/API akışları üzerinden yapılır. Terminal notification requeue gibi yeni bir mutasyon yüzeyi eklenirse ayrı authz, audit, idempotency, rate limit ve ADR olmadan yayınlanamaz; doğrudan DB düzeltmesi yasaktır.
- Production closure execution, gerçek SMTP teslimatı ve operasyon/reconciliation kapıları doğrulanana kadar güvenli varsayılanla kapalı tutulur; explicit configuration olmadan açılamaz.
- `UserAccount.Status != Active` olan authenticated istekler merkezi aktif hesap kapısında `401` ile reddedilir; yeni endpoint bu kapıyı bypass edemez.
- Online ödeme MVP varsayılanı değildir; `Payments` modülü production'da explicit konfigürasyon olmadan ödeme tahsilatı açamaz.
- Kart verisi, CVV, PAN veya raw ödeme sağlayıcı payload'u veritabanında, logda, audit detayında veya response'ta tutulamaz; ödeme yalnız hosted/redirect checkout ile tasarlanır.
- Ödeme webhook'ları provider event id + payload hash ile idempotent kaydedilir; raw payload saklanmaz ve provider secret'ları repo/config dosyasına gömülmez.
- Ödeme ayar mutation'ları `BusinessOwner` tenant-wide yetki + tenant-scope step-up veya `PlatformAdminWithStepUp` kararı netleşmeden yayınlanmaz; read-only readiness yüzeyi yalnız `PlatformAdminWithStepUp` olabilir.
- External API ve webhook delivery Phase 5'te explicit config olmadan kapalıdır; read-only integrations readiness yalnız `PlatformAdminWithStepUp` olabilir.
- Integration API key ve webhook signing secret raw değerleri veritabanı, log, audit veya response içine yazılamaz; yalnız prefix/hash gibi güvenli teknik kanıt saklanır.
- Integration API key ve webhook signing secret plaintext değeri yalnız create application service sonucunda tek seferlik dönebilir; tekrar okunabilir biçimde persist edilemez ve audit detayına eklenemez.
- Webhook delivery raw payload saklamaz; payload hash, correlation id, event type ve idempotent delivery durumu tutulur.
- İşletme integration mutation'ları `BusinessOwner` tenant-wide yetki + tenant-scope step-up kararı netleşmeden yayınlanmaz; platform readiness endpoint'i tenant mutation bypass'ı değildir.

---

## 7) Abuse / Yaptırım Sistemi (Slot Spam, Kötüye Kullanım)

Slot bloklanmadığı için **abuse beklenir**; sistem tasarımının parçası olmalı.

### 7.1 Önleme (MVP minimum)

- Kullanıcı hesabı zorunlu (booking request için).
- Kullanıcı başına:
  - eşzamanlı `PendingApproval` limitleri
  - gün/hafta bazlı booking request limiti
  - ardışık ret/expire sonrası cooldown
- İşletme bazlı limitler (aynı işletmeye kısa sürede aşırı istek engeli)

### 7.2 Kademeli yaptırım (strike/ban)

- Strike sayacı (neden + zaman damgası + tenant bağımsız kullanıcı profili)
- Merdiven:
  1) uyarı + kısa cooldown
  2) limit düşürme
  3) geçici ban (24–72 saat)
  4) kalıcı kapatma (itiraz/appeal ile)

IP ban sadece güçlü sinyal ve ağır abuse durumunda; NAT/CGNAT nedeniyle “tek başına IP” ile kalıcı ban uygulanmaz. Kalıcı hesap kapatma manuel inceleme ister.

### 7.3 İşletme geri bildirimi

- İşletme panelinde “spam/abuse” işaretleme aksiyonu olmalı.
- Admin panelde abuse olayları listelenebilir olmalı.
- İşletme abuse işaretlemesi yalnızca belirli appointment request için, tenant+branch scope doğrulanarak oluşturulur; tek başına strike veya sanction üretmez.
- Aynı tenant+appointment request raporu idempotent olmalı; raporlayan actor için günlük limit ve yarış koşulu koruması uygulanmalıdır.
- Strike yalnızca `PlatformAdminWithStepUp` confirm kararıyla üretilir; süreli, auditli ve revoke edilebilir olmalıdır.
- Aktif strike sayısından hesaplanan risk seviyesi yalnızca operasyon önerisidir; otomatik sanction veya kalıcı kapatma tetikleyemez.
- Appeal create/review, strike revoke, sanction apply/revoke ve closure proposal/execution aynı kullanıcı kapsamlı transaction lock ile sıralanmalıdır; risk kanıtını azaltan işlemle eski risk sayımına dayalı kapatma yarışı veya closure tamamlanırken paralel ikinci bloklayıcı sanction oluşturulamaz. Closure review/execute ayrıca auditli, rate limited ve row lock korumalı olmalıdır.

---

## 8) API Tasarım Kuralları (taslak)

- Public (müşteri) ve Admin (işletme) yüzeyleri ayrıştırılır.
- Swagger/OpenAPI yalnızca Development ortamında açık tutulur; production ortamında dokümantasyon UI veya JSON endpoint'i yayınlanmaz.
- Idempotency:
  - “onay” ve “iptal” gibi komutlar idempotent tasarlanır.
- Hata yönetimi:
  - validation hataları 400/422
  - auth 401, authz 403
  - tenant dışı kaynak erişimi 404

---

## 9) Test Politikası (başlangıçtan itibaren)

- Booking çekirdeği için test şart:
  - double booking denemesi fail
  - aynı staff + farklı resource çakışması fail
  - aynı resource + farklı staff çakışması fail
  - approval yarış koşulu (aynı slotu iki kere onaylamaya çalışma)
  - TTL expiry
  - tenant izolasyonu (başka tenant kaynağı görünmez)
- En az bir entegrasyon test katmanı planlanmalı (PostgreSQL ile).

### 9.1 UTF-8 / Mojibake Koruma Kapısı (her build check'te zorunlu)

- Bu repodaki tüm kaynak dosyaları (`.cs`, `.ts`, `.tsx`, `.css`, `.md`)** UTF-8 without BOM** olarak yazılır. ASCII veya Windows-1252/1254 decode'u ile karıştırılmaz.
- **Her build check (local `dotnet build`, `pnpm typecheck`, `pnpm test`, CI workflow) öncesinde mojibake kontrolü zorunludur.** Bu kapı olmadan PR merge edilmez.
- Kontrol şunları doğrular:
  1. Her dosya `TextDecoder('utf-8', { fatal: true })` ile hatasız decode edilebilmeli (geçersiz byte dizisi = fail).
  2. Yaygın mojibake pattern'leri dosyalarda geçmemeli: UTF-8 byte'larının Windows-1252 ile yanlış decode edilmiş halleri (Türkçe karakterler için: `0xC3 0xA7`, `0xC4 0xB1`, `0xC5 0x9F`, `0xC4 0x9F`, `0xC3 0xBC`, `0xC3 0xB6`, `0xC4 0xB0`, `0xC5 0x9E`, `0xC3 0x87`, `0xC4 0x9E`, `0xC3 0x9C`, `0xC3 0x96`) ve `U+FFFD` (REPLACEMENT CHARACTER).
- Kabul edilen referans script: `scripts/Check-SourceEncoding.ps1` (aşağıdaki kontratı uygular). Bu script `pre-commit`, CI lint job ve lokal `pnpm check:encoding` / `dotnet build` öncesi hook'ta çağrılır.
- **Windows + PowerShell tuzağı**: PowerShell 5.1'de `Get-Content -Raw -Encoding UTF8` **birlikte kullanılamaz** (parametre bağlama hatası) ve `Set-Content` default encoding Windows-1252'dir. Toplu dosya dönüştürme/düzenleme yaparken **mutlaka** `[System.IO.File]::ReadAllBytes` + `[System.Text.Encoding]::UTF8.GetString` ile oku, `[System.IO.File]::WriteAllBytes` ile UTF-8 byte'ı yaz. Konsol çıktısındaki `?` karakteri gerçek dosya içeriğini yansıtmaz; kontrol için raw byte okuma yap.
- Bir dosya mojibake tespit edilirse: build/test fail eder; PR'a dosya **orijinal haline** (`git checkout`) geri çekilip değişiklik encoding-safe yöntemle yeniden uygulanır. Mojibake'li dosya commit edilmez.

---

## 10) Git / PR Disiplini

- Ana dal: `main`
- Küçük, odaklı commit’ler (tek commit de kabul; ama dağınık değişiklik yok).
- “Hızlı olsun” diye güvenlik/tenancy kurallarını bypass eden PR merge edilmez.
- Doküman değişiklikleri ve kod değişiklikleri mümkünse ayrı commit’lerde tutulur.

---

## 11) Uygulama İskeleti ve Bağımlılık Kuralları

### 11.1 Solution yapısı

```text
src/
  Apps/RezSaaS.Api/                         Composition root
  BuildingBlocks/RezSaaS.BuildingBlocks/   Ortak teknik kontratlar
  Modules/RezSaaS.Modules.*/               Domain modülleri
tests/
  RezSaaS.ArchitectureTests/               Mimari sınır testleri
```

### 11.2 Referans yönü

- `RezSaaS.Api` tüm modülleri composition root olarak bir araya getirir.
- Her modül yalnızca `RezSaaS.BuildingBlocks` referansı alabilir.
- Modülden modüle doğrudan assembly referansı yasaktır; CI mimari testi bunu denetler.
- Ortak domain entity üretip `BuildingBlocks` içine taşımak yasaktır. `BuildingBlocks` yalnızca teknik kontratlar içindir.
- `BuildingBlocks` içinde cross-module teknik kontrat bulunabilir (`IAbuseEventRecorder`, `IAuditLogRecorder`, `ITransactionalMessageOutbox` gibi); bu kontratlar domain entity, EF model veya modül içi iş kuralı taşımaz.
- Modüller arası use-case ihtiyacı oluşursa açık contract/event tasarlanır ve ADR güncellenir.

### 11.3 Teknik sürüm ve yerel altyapı

- SDK sürümü repo kökündeki `global.json` ile sabitlenir.
- NuGet sürümleri `Directory.Packages.props` içinde merkezi tutulur.
- Ortak analyzer/build ayarları `Directory.Build.props` içindedir; warnings-as-errors gevşetilmez.
- Yerel PostgreSQL `compose.yaml` üzerinden çalışır. Local varsayılanlar staging/production ortamında kullanılmaz.
- Parola, connection string, token, kullanıcı hesabı veya değiştirilebilir operasyon verisi kaynak koda ve migration seed'ine gömülmez. Yerel değerler ignored `.env` dosyasından yalnızca Development ortamında okunabilir; shared ortam değerleri secret manager üzerinden sağlanır.
- Clock bağımlılığı için .NET `TimeProvider` kullanılır; domain/application kodunda `DateTime.UtcNow` doğrudan çağrılmaz.

---

## 12) Yeni Özellik Kontrol Listesi

Her yeni özellikte aşağıdaki etkileri kontrol et. Etki varsa ilgili dokümanı aynı PR içinde güncelle:

- Yeni veya değişen domain terimi: `docs/05-domain-sozlugu.md`
- Ürün/mimari kararı: `docs/06-karar-kaydi.md`
- Rol, tenant veya branch scope etkisi: `docs/07-yetki-matrisi.md`
- Bildirim etkisi: `docs/08-bildirim-kanali-stratejisi.md`
- Abuse yüzeyi veya yaptırım etkisi: `docs/09-abuse-yaptirim-politikasi.md`
- PII, log veya saklama etkisi: `docs/11-veri-envanteri-taslagi.md`
- Yeni açık karar: `docs/12-acik-sorular.md`
- Yeni endpoint: authn, authz, tenant isolation, idempotency, audit ve rate limit değerlendirmesi
- Yeni DB tablosu: tenant-scoped/global kararı, index, migration ve saklama politikası
- Yeni background job: explicit tenant scope, idempotency, retry ve audit/telemetry
- Yeni modül bağımlılığı: doğrudan reference ekleme; önce contract/event ve ADR

---

## 13) Frontend Sınırları

Frontend geliştirmeden önce:

- `docs/23-frontend-mimari-tasarim-kararlari.md`
- `docs/24-frontend-uygulama-plani.md`

okunur.

- Frontend ayrı repo yerine `src/Apps/RezSaaS.Web` altında başlar; yeni web app,
  ayrı repo veya micro-frontend için ADR gerekir.
- Browser auth cookie tabanlıdır; bearer/access token browser storage'a yazılmaz.
- Business tenant header yalnızca authenticated backend context'inden merkezi API
  client tarafından eklenir; kullanıcıya serbest tenant GUID seçtirilmez.
- API DTO'ları elle çoğaltılmaz; versioned OpenAPI artifact ve üretilen TypeScript
  tipleri kullanılır.
- Frontend route guard backend authz'nin yerine geçmez.
- `PendingApproval`, confirmed appointment gibi gösterilmez; branch timezone'u
  browser timezone'una sessizce çevrilmez.
- Tasarım sistemi Figma + semantic token + Storybook disiplinini izler. Hazır
  component library görünümü, sahte dashboard metriği ve backend'i olmayan form
  teslimat kabul edilmez.
- Her frontend dilimi lint, typecheck, test, Storybook a11y, ilgili Playwright
  journey ve responsive visual QA ile kapanır.

### 13.1 Proje UI/Metin Dili Türkçe'dir (zorunlu varsayılan)

- Projenin **varsayılan ve tek UI/metin dili Türkçe'dir**. Tüm kullanıcı yüzey
  metinleri (button label, empty state, error mesajı, toast, e-posta, SMS, audit
  reason copy, onay dialog metni, tooltip, placeholder vb.) Türkçe yazılır.
- Domain terimleri Türkçe ve tutarlı kullanılır; İngilizce'ye **çevrilmez veya
  karıştırılmaz**: "Talep" (`AppointmentRequest`), "Randevu" (`Appointment`),
  "Onay bekliyor" (`PendingApproval`), "Onaylandı" (`Confirmed`), "Reddedildi"
  (`Declined`), "Süresi doldu" (`Expired`), "Başka talep seçildi" (`Superseded`),
  "Şube" (`Branch`), "Personel" (`StaffMember`), "Hizmet" (`Service`),
  "Yetkinlik" (`Skill`), "Kaynak" (`Resource`), "İşletme" (`Tenant`),
  "İtiraz" (`Appeal`), "Ceza" (`Sanction`), "Hesap kapatma" (`AccountClosureCase`).
- Backend enum/değer adları (`PendingApproval`, `BusinessOwner` vb.) İngilizce
  kalır (kod/kontrat katmanı); UI'da gösterilirken Türkçe label'a map'lenir
  (`StatusBadge`, rol etiketleri). Enum adları kullanıcıya raw gösterilmez.
- Code comment, dosya adı, route path, CSS class adı, değişken adları ve API
  kontratı İngilizce kalır; sadece **kullanıcının gördüğü metinler** Türkçe'dir.
- Lokalizasyon/i18n altyapısı (multi-dil) MVP sonrasına ertelenmiştir ama
  **ilk ve varsayılan dil Türkçe'dir**; İngilizce veya başka bir dil PR'ında
  Türkçe varsayılan korunmadan merge edilmez.
- Çeviri tutarlılığı review'da kontrol edilir: aynı domain kavramı farklı
  sayfalarda farklı Türkçe kelimeyle ifade edilmez (örn "Şube" bir yerde "Salon"
  başka yerde "Mağaza" olmaz). Terim birliği için `docs/05-domain-sozlugu.md`
  tek kaynaktır.
- Tarihsel/saat gösterimleri `Intl.DateTimeFormat("tr-TR", ...)` ile Türkçe
  formatlanır; para birimi `TRY`/`₺` ve `tr-TR` locale kullanılır.

