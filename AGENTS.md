# RezSaaS — Agent & Mimari Kuralları

Bu dosya; bu repoda çalışan insan/agent (Codex, Claude, vb.) için **mimari sınırlar, güvenlik minimumları, çalışma disiplini ve “mimarinin ezilmemesi”** için kuralları tanımlar.

> RezSaaS; tek domain altında çoklu işletme/şube/personel/kaynak destekleyen, **multi-category** salon rezervasyon + operasyon SaaS’idir. Rezervasyonlar **işletme onaylı** ilerler ve `PendingApproval` için **24 saat TTL** vardır. MVP’de her randevu **1 staff + 1 resource** ile planlanır.

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
- TTL: `PendingApproval` **24 saat** içinde yanıtlanmazsa `Expired`
- Slot davranışı: `PendingApproval` **slot bloklamaz**; işletme aynı slot için gelen birden fazla isteği arasından birini seçebilir.
- Zorunlu eşleme: her randevu **tam olarak 1 Staff + 1 Resource** ile planlanır.
- Multi-service: tek randevu içinde çoklu hizmet olabilir; MVP’de toplam süre “tek blok” olarak değerlendirilir ve aynı staff+resource ile planlanır.

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
- Branches
- Catalog
- Resources
- Availability
- Booking
- Messaging (email/SMS/WhatsApp)
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

### 4.2 Uygulama içi zorunluluklar

- Varsayılan: tüm sorgular tenant filtreli.
- EF Core kullanılıyorsa:
  - Global query filter veya repository katmanında merkezi tenant filtresi
  - “Tenant filtresi yok” olan sorgu **PR review’de reddedilir**
- Raw SQL gerekiyorsa:
  - `tenant_id` filtresi olmadan merge edilmez
  - Parametrik sorgu zorunlu (SQL injection önlemi)

### 4.3 Yetkilendirme

- RBAC (rol bazlı) + scope (branch gibi kapsam) ileride genişleyebilir.
- Tenant dışı erişim: sessiz “boş dönme” değil; net `403`.
- Kritik aksiyonlar (rol yönetimi, ban, ödeme ayarları gibi) için step-up/MFA planı Phase 3’te.

---

## 5) Rezervasyon Tutarlılığı ve Eşzamanlılık

### 5.1 Double booking engeli

- “Aynı staff + aynı resource + çakışan time range” kesinlikle oluşmamalı.
- Bu kural sadece application ile değil, DB constraint ile de desteklenmelidir (PostgreSQL hedefi).

### 5.2 İşletme onayı yarış koşulu

Slot bloklanmadığı için onay ekranında yarış olur:

- Onay işlemi transaction içinde çalışmalı.
- Onay anında çakışma tekrar kontrol edilmeli (DB ile).
- Çakışma varsa onay başarısız olmalı; işletme başka isteği seçebilmeli.

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

IP ban sadece güçlü sinyal ve ağır abuse durumunda; NAT/CGNAT nedeniyle “tek başına IP” ile kalıcı ban önerilmez.

### 7.3 İşletme geri bildirimi

- İşletme panelinde “spam/abuse” işaretleme aksiyonu olmalı.
- Admin panelde abuse olayları listelenebilir olmalı.

---

## 8) API Tasarım Kuralları (taslak)

- Public (müşteri) ve Admin (işletme) yüzeyleri ayrıştırılır.
- Idempotency:
  - “onay” ve “iptal” gibi komutlar idempotent tasarlanır.
- Hata yönetimi:
  - validation hataları 400/422
  - auth 401, authz 403
  - tenant dışı erişim 403

---

## 9) Test Politikası (başlangıçtan itibaren)

- Booking çekirdeği için test şart:
  - double booking denemesi fail
  - approval yarış koşulu (aynı slotu iki kere onaylamaya çalışma)
  - TTL expiry
  - tenant izolasyonu (başka tenant verisi 403)
- En az bir entegrasyon test katmanı planlanmalı (PostgreSQL ile).

---

## 10) Git / PR Disiplini

- Ana dal: `main`
- Küçük, odaklı commit’ler (tek commit de kabul; ama dağınık değişiklik yok).
- “Hızlı olsun” diye güvenlik/tenancy kurallarını bypass eden PR merge edilmez.
- Doküman değişiklikleri ve kod değişiklikleri mümkünse ayrı commit’lerde tutulur.

