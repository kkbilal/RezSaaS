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
- İşletme onay/ret endpoint'leri tenant header + authenticated user + tenant membership authz ister; `BusinessOwner` tenant-wide, `BranchManager` branch-scoped, `Staff` varsayılan deny.
- Approve/decline API'leri mevcut Booking application servislerini kullanır; audit, transactional outbox ve `Superseded` davranışı bypass edilmez.

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

