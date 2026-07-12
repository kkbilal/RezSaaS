# 29 — Frontend Bilgi Mimarisi ve Sayfa Envanteri (CANONICAL)

> **Durum:** Geçerli. Tarih: 2026-07-12.
> Frontend sayfa / navigasyon / rol konularında **tek doğruluk kaynağıdır.**
> Buradaki her sayfa ve her buton, backend kodunda okunarak **kanıtlanmış** bir uca bağlıdır.

## Bu doküman neyi ezer (SUPERSEDED)

Aşağıdakiler frontend IA konusunda **artık geçersizdir**; çelişki halinde bu doküman kazanır:

| Doküman | Neden geçersiz |
|---|---|
| `ui-tasarim-promptu.md` | Dark-glassmorphism dayatıyor; `docs/23`'ün "light lansman kapısıdır, gradient/blur/glass'tan kaçın" kararına aykırı. **Light-first kararı alındı.** |
| `frontend-page-inventory.md` | Bu envanterle çelişiyor. |
| `prototype-page-inventory.md` | Yukarıdakiyle de kendi içinde çelişiyor. |
| `platform-admin-pages-implementation-plan.md` | **Var olmayan endpoint'ler uyduruyor** (`/api/admin/abuse/appeals/...`); progress dosyasıyla da çelişiyor. |
| `platform-admin-implementation-progress.md` | "COMPLETE" dediği sayfalar placeholder kabuğu. |
| `frontend-implementation-status.md` | "F6.2 Settings CRUD COMPLETED, tüm ayar sayfaları gerçek API kullanıyor" diyor; **gerçekte altı sayfa da 'yakında' EmptyState kabuğu.** Aktif olarak zararlı. |

**Geçerli kalanlar:** `docs/23` (mimari ADR), `docs/24` (F0-F7 planı), `AGENTS.md`.

---

## 0. MVP tanımı (tek filtre)

> "Amaç ilk adımda **müşterinin randevu alabilmesi** ve berber veya artık ne tür bir salon ise
> **bu tarafın da rezervasyonlarını yönetebilmesi**, bu rezervasyonların **fiyatlarını yönetebilmesi**,
> **elemanlarını yönetebilmesi** ve **müşteri de rezervasyon geçmişi / rezervasyonları** gibi yönetimi
> yapabilmesi. İlk adımda kesin hedef."

Bu döngüye hizmet etmeyen her şey MVP dışıdır.

### Kapsam dışı bırakılanlar ve gerekçeleri

| Dışarıda | Gerekçe |
|---|---|
| Suistimal / moderasyon kontrol düzlemi (13 sayfa) | Tek kullanıcısı kurucunun kendisi. Sıfır kullanıcılı üründe moderasyon ürünü inşa etmek. İlk yıl `psql` + kayıtlı sorgu ile yönetilir. |
| Walk-in / telefonla elle randevu | **Kanıtlandı: ucuz değil.** `Appointment` domain'i "her randevunun bir müşteri hesabı vardır" invariant'ı üzerine kurulu (`Appointment.cs:25 RequireNonEmpty`); bu invariant Reviews ve Payments modüllerine yayılmış (~38 dosya). `Rebook` yeniden kullanılamaz — çalışmak için ortada zaten bir randevu ve müşteri hesabı olmasını şart koşuyor, yoktan yaratamıyor. Gerçekçi maliyet 3-4 gün + regresyon riski. |
| Yetkinlik (skill) ekranları | **Backend write-only.** Atama POST/DELETE var, mevcut seçimi okuyan GET **yok** (`BusinessVariantResponse`'ta `RequiredSkillIds` alanı yok; `StaffSkillService.GetSkillIdsForStaffAsync` yazılmış ama hiçbir yerden çağrılmıyor). Hangi kutunun işaretli olduğu bilinemez → ekran **çizilemez**. |
| Personel bazlı çalışma saati | Backend'de entity **yok**. Sadece `BranchWorkingHours` (şube seviyesi). Vaat edilmeyecek. |
| Galeri yönetimi | Entity + public okuma var, **yönetim endpoint'i yok**. |
| Ödeme, Geliştirici (API key / webhook) | `Payments` ve `Integrations` modülleri `Program.cs`'te **yorum satırında**. |

**Altın kural:** Backend ucu yoksa sahte ekran / mock / "yakında" placeholder **üretilmez**; menüde **hiç görünmez** (disabled bile değil).

---

## 1. Doğrulanmış bulgular

### 1.1 Çürütülen varsayımlar — bunlar SORUN DEĞİL

| Varsayım | Gerçek |
|---|---|
| "Multi-tenant izolasyon bir UI iddiası; `X-RezSaaS-Tenant` header'ı ile başka tenant okunabilir" | **YANLIŞ.** Header tenant'ı *seçer*, `TenantBookingAuthorizationService` üyelik tablosuyla *kesiştirir*. Üyelik tablosunda global query filter **yok** — otoritesi header'dan bağımsız. Saldırı testli: `PublicDiscoveryApiTests.BusinessAppointmentRequestsRequireTenantMembership` → **403**. 9 modülde 29 global query filter. |
| "Şube-scoped BranchManager başka şubenin kaydını okuyabilir (IDOR)" | **YANLIŞ.** Sunucu kaydın gerçek `BranchId`'sini okuyup karşılaştırıyor → **403**. Tenant dışı kaynak → **404** (varlık sızmıyor). |
| "Cookie auth + 60 mutation ucu var → CSRF açığı" | **YANLIŞ.** `UnsafeRequestOriginMiddleware` (Program.cs:351) tüm POST/PUT/PATCH/DELETE'te Origin/Referer doğruluyor, **fail-closed**; üstüne SameSite=Lax. Testli (`IdentityApiTests.cs:373`). **Anti-forgery token EKLENMEYECEK.** |
| "`Branch.TimeZoneId` Windows formatında → saatler sessizce yanlış" | **YANLIŞ.** Uçtan uca IANA (`Europe/Istanbul`); repoda "Turkey Standard Time" **sıfır** kez geçiyor. `GET /slots` hem UTC hem `LocalStart` dönüyor, frontend dönüşüm **yapmıyor**. **`date-fns-tz` EKLENMEYECEK.** |
| "Fiyat değişince mevcut randevular bozulur, UI uyarmalı" | **YANLIŞ.** `AppointmentLine` fiyatı **snapshot** tutuyor; fiyat talep anında sunucuda katalogdan okunuyor. **Uyarı KOYULMAYACAK** — yanlış olur. |

### 1.2 Gerçek bulgular — daha önce hiçbir dokümanda yazılmamıştı

**A. BranchManager, işletme yönetimi menüsünün TAMAMINDA 403 alıyor — ama sidebar hepsini gösteriyor.**

`src/Apps/RezSaaS.Api/Business/` altındaki **11 composer**'ın tamamı `CanManageBusinessSettingsAsync` çağırıyor;
`TenantBookingAuthorizationService.cs:74-96` bu metotta **yalnızca `Role == BusinessOwner`** üyeliğini kabul ediyor.
`BusinessContextComposer.cs:55-59` BranchManager'a sadece `appointmentRequests.manage` + `reportAbuse` veriyor —
`business.settings.manage` **yok**. Ama `panel-shell.tsx:81-92` Personel / Hizmetler / Şubeler / Kaynaklar /
Yetenekler / Kaynak türleri / Çalışma saatleri / Ayarlar öğelerini **role bakmadan herkese** basıyor.

→ Bugün çıksak şube müdürü menüdeki **7 öğeden 7'sinde** duvara çarpar.

**B. Personel adı güncelleme SESSİZCE çalışmıyor — 200 OK döner, isim değişmez.**

`StaffManagementService.cs:119-155` entity'yi çekiyor, `DisplayName`'i **uygulamadan** `SaveChanges` çağırıyor.
`StaffMember.cs` domain'inde `Rename` / `UpdateDisplayName` metodu **hiç yok** (sadece `Create` + `Archive`).

→ Yanlış yazılmış personel ismini düzeltmek — tipik ilk-kullanım senaryosu — **çalışmıyor ve kullanıcıya yalan söylüyor.**

**C. Müşteri onaylanmış (Confirmed) randevusunu iptal EDEMİYOR — endpoint yok.**

`/api/customer` altında 3 grup var: `abuse`, `appointment-history` (sadece GET), `reviews` (sadece POST).
Talep iptali sadece `PendingApproval`'da çalışıyor; onaydan sonra `APPOINTMENT_REQUEST_ALREADY_CLOSED` dönüyor.
İşletme tarafındaki cancel ucu tenant üyeliği arıyor → müşteri `Forbidden` alıyor. Kaçış yolu **yok**.

→ Müşterinin planı değişirse **yapacak hiçbir şeyi yok, salonu aramak zorunda** — ki bu tam olarak bu SaaS'in
çözmeyi vaat ettiği problem. MVP cümlesinin *"müşteri de rezervasyonlarını yönetebilmesi"* kısmını doğrudan deliyor.

### 1.3 Diğer kanıtlanmış tuzaklar

| Bulgu | Etki | Aksiyon |
|---|---|---|
| `appsettings.json:77` → `AllowedOrigins: []` (prod boş) | Deploy edilirse **randevu oluşturma dahil tüm POST 403 döner** | Deploy öncesi ortam değişkenine gerçek origin yaz. **Lansman günü patlayacak tek madde.** |
| `Branch.TimeZoneId` doğrulanmamış serbest metin (sadece `IsLength(1,80)`) | "Istanbul" yazılırsa o şube **sonsuza dek 0 slot** döner (200 OK, hata yok). "Turkey Standard Time" yazılırsa frontend `RangeError` ile **çöker**. | BE: validate + IANA'ya normalize. FE: küratörlü IANA `Select` + 3 formatlayıcıya try/catch. |
| `PATCH .../variants/{id}` adı PATCH, **davranışı PUT** — 5 alan da zorunlu | Kısmi gönderim veriyi bozar / 400 verir | `VariantPriceRow` bu tuzağı **kapsüller**; kısmi gönderim yapısal olarak imkansız olur. |
| `CurrencyCode` serbest string, ISO whitelist yok | Aynı işletmede varyant A `TRY`, B `USD` olabilir | UI'da **kullanıcıya açma**, sabit `TRY` gönder. |
| Müşteri geçmişinde `?status` filtresi **sadece taleplere** uygulanıyor | `/hesabim` sekmeleri yanlış veri gösterir | BE fix. FE: filtreye **güvenme**, tek çağrı + client-side ayır. |
| Personel arşivleme aktif randevu kontrolü yapmıyor | Gelecek randevusu olan personel arşivlenir, randevular sahipsiz kalır | MVP'de ertelenebilir; uyarı göster. |
| Ana oturum: 14 gün sliding, absolute timeout **yok**, `Secure=SameAsRequest` | Ortak / tezgah bilgisayarında süresiz oturum | Prod öncesi ~5 satır `ConfigureApplicationCookie`. |

---

## 2. Lansman blokajları

| # | Blokaj | Kim |
|---|---|---|
| 1 | BranchManager rolü işletme yönetimi menüsünün TAMAMINDA 403 alıyor, ama sidebar hepsini gösteriyor | Frontend |
| 2 | Müşteri onaylanmış (Confirmed) randevusunu iptal edemiyor — endpoint yok | Backend |
| 3 | Personel adı güncelleme sessizce çalışmıyor — 200 OK döner, isim değişmez | Backend |
| 4 | Prod'da AllowedOrigins boş — deploy edilirse randevu oluşturma dahil TÜM POST/PUT/PATCH/DELETE 403 döner | Backend |
| 5 | Şube saat dilimi (TimeZoneId) doğrulanmamış serbest metin — yanlış değer o tenant'ı sessizce lansman dışı bırakır | Ikisi |
| 6 | Üç canlı 404: çekirdek döngünün ana linkleri kırık | Frontend |
| 7 | Müşteri randevu geçmişinde status filtresi randevulara uygulanmıyor — /hesabim sekmeleri yanlış veri gösterir | Ikisi |
| 8 | Şube-scoped BranchManager, branchId göndermeden liste çağırırsa 403 alır (boş ekran değil, hata) | Frontend |

### Detay

#### 1. [Frontend] BranchManager rolü işletme yönetimi menüsünün TAMAMINDA 403 alıyor, ama sidebar hepsini gösteriyor

**Kanıt:** src/Apps/RezSaaS.Api/Business/ altındaki 11 composer (BusinessBranchComposer, BusinessResourceComposer, BusinessResourceTypeComposer, BusinessReviewComposer, BusinessServiceComposer, BusinessSettingsComposer, BusinessSkillComposer, BusinessStaffComposer, BusinessStaffUnavailableComposer, BusinessVariantComposer, BusinessWorkingHoursComposer) CanManageBusinessSettingsAsync çağırıyor; TenantBookingAuthorizationService.cs:74-96 bu metotta yalnızca Role == TenantMembershipRole.BusinessOwner üyeliğini kabul ediyor. BusinessContextComposer.cs:55-59 BranchManager'a sadece [business.appointmentRequests.manage, business.appointmentRequests.reportAbuse] veriyor; business.settings.manage YOK. panel-shell.tsx:81-92 ise Personel/Hizmetler/Şubeler/Kaynaklar/Yetenekler/Kaynak türleri/Çalışma saatleri/Ayarlar öğelerini role bakmadan herkese basıyor.

**Çözüm (Karar K1 ile güncellendi):** BranchManager rolü **V2'ye bırakıldı** → MVP'de bu role kimse atanmıyor, dolayısıyla 403 duvarı **oluşmuyor**. Bunun **zorunlu** koşulu: `/platform/tenantlar/[id]/uyeler` rol seçicisi MVP'de **sadece `BusinessOwner`** sunacak (`BranchManager` ve `Staff` seçenekleri listeden çıkarılacak — `Staff`'ın capability listesi backend'de zaten boş dizi).

Buna rağmen **nav-manifest yine de tipli kurulur**: `permission` alanı zorunlu union, `can()` fail-closed, sunucu tarafında her `/panel/*` sayfası `requireCapability()` ile korunur. Amacı bugünkü 403'ü çözmek değil — V2'de rol eklenirken "izin yazmayı unuttum → herkese açıldı" hatasını **derleme zamanında** yakalamaktır.

#### 2. [Backend] Müşteri onaylanmış (Confirmed) randevusunu iptal edemiyor — endpoint yok

**Kanıt:** /api/customer altında yalnızca 3 grup var: abuse, appointment-history (sadece MapGet), reviews (sadece MapPost). CustomerAppointmentHistoryEndpointExtensions.cs:12-52 tek MapGet. Business tarafındaki POST /api/business/appointments/{id}/cancel ise BusinessAppointmentComposer.cs:322-351 üzerinden tenant üyeliği arıyor, müşteri principal'i Forbidden alır. Talep iptali (CancelAppointmentRequestService.cs:92-95) sadece PendingApproval statüsünde çalışıyor; onaydan sonra APPOINTMENT_REQUEST_ALREADY_CLOSED döner.

**Çözüm:** Backend: POST /api/public/businesses/{slug}/appointments/{appointmentId}/cancel eklenecek (mevcut public request-cancel deseninin birebir kopyası; sahiplik = Appointment.CustomerUserAccountId == sub, sadece Status==Confirmed). Domain metodu Appointment.Cancel ZATEN var, sıfırdan domain yazılmayacak. Frontend: bu uç gelene kadar /hesabim/randevular'da Confirmed satırlarda iptal butonu GÖSTERİLMEZ (sahte ekran üretme yasağı).

#### 3. [Backend] Personel adı güncelleme sessizce çalışmıyor — 200 OK döner, isim değişmez

**Kanıt:** StaffManagementService.cs:119-155 UpdateAsync entity'yi çekiyor, DisplayName'i uygulamadan SaveChanges çağırıyor. StaffMember.cs:44,60 — domain'de yalnızca Create ve Archive var, Rename/UpdateDisplayName metodu HİÇ YOK. BusinessStaffComposer.cs:111 UpdateStaffCommand'i DisplayName ile çağırıyor ama boşa gidiyor.

**Çözüm:** Backend: StaffMember'a Rename(string displayName) ekle (2-200 uzunluk validasyonu) ve StaffManagementService.UpdateAsync içinde çağır. Bu düzelmeden /panel/personel düzenleme ekranı kullanıcıya yalan söyler — MVP'nin 'elemanlarını yönetebilmesi' maddesini doğrudan deler.

#### 4. [Backend] Prod'da AllowedOrigins boş — deploy edilirse randevu oluşturma dahil TÜM POST/PUT/PATCH/DELETE 403 döner

**Kanıt:** src/Apps/RezSaaS.Api/appsettings.json:77-79 → AllowedOrigins: []. UnsafeRequestOriginMiddleware.cs:36,47-49 fail-closed: Origin eşleşmezse 403, Origin ve Referer'ın ikisi de yoksa reddeder. Program.cs:189-208 CORS aynı listeyi okuyor. appsettings.Development.json:12-17 sadece localhost:3000 dolu.

**Çözüm:** Deploy öncesi Security:UnsafeRequestOrigins:AllowedOrigins ortam değişkenine gerçek web origin'i yazılacak (örn. https://app.rezsaas.com). .env.example ve deploy dokümanına eklenecek. Frontend'de anti-forgery token kodu YAZILMAYACAK — Origin doğrulama + SameSite=Lax zaten çift katman koruma sağlıyor.

#### 5. [Ikisi] Şube saat dilimi (TimeZoneId) doğrulanmamış serbest metin — yanlış değer o tenant'ı sessizce lansman dışı bırakır

**Kanıt:** BranchManagementService.cs:266 sadece IsLength(1,80) kontrolü yapıyor; Branch.cs:30 yalnızca Trim. Migration'da CHECK yok. business-branch-management-page.tsx:245 serbest metin input. Sonuç: 'Istanbul' yazılırsa PublicTimeZoneResolver.TryFind false döner, PublicSlotSearchComposer.cs:97-100 boş yanıt verir → o şube sonsuza dek 0 slot, 200 OK, hata mesajı yok. 'Turkey Standard Time' yazılırsa backend kabul eder ama date-time.ts:11-15/85-90/100-104 try/catch'siz Intl.DateTimeFormat RangeError fırlatır → sayfa çöker.

**Çözüm:** Backend: PublicTimeZoneResolver.TryFind'i BuildingBlocks'a taşıyıp BranchManagementService validasyonunda çağır, geçersizse 400 dön, saklamadan önce IANA'ya normalize et. Frontend: /panel/subeler'de serbest metin yerine küratörlü IANA Select (varsayılan Europe/Istanbul) + date-time.ts'teki 3 formatlayıcıya try/catch. date-fns-tz EKLENMEYECEK — projede zaten yok ve gerekmiyor.

#### 6. [Frontend] Üç canlı 404: çekirdek döngünün ana linkleri kırık

**Kanıt:** (1) panel-shell.tsx:68 sidebar 'Talepler' → routes.business.requests = /panel/talepler; src/app/panel/talepler/page.tsx YOK. (2) business-calendar-page.tsx:117 → routes.business.appointments = /panel/randevular; src/app/panel/randevular/page.tsx YOK. (3) customer-shell.tsx:20 'Genel bakış' → routes.customer.dashboard = /hesabim; src/app/hesabim/page.tsx YOK (sadece /hesabim/talepler, /randevular, /profil, /itirazlar var).

**Çözüm:** /panel/talepler ve /panel/randevular gerçek sayfa olarak yazılır (uçları hazır: GET /api/business/appointment-requests, GET /api/business/appointments). /hesabim, /hesabim/randevular'a server-side redirect yapar (yeni sayfa yazmadan 404 kapanır).

#### 7. [Ikisi] Müşteri randevu geçmişinde status filtresi randevulara uygulanmıyor — /hesabim sekmeleri yanlış veri gösterir

**Kanıt:** CustomerAppointmentHistoryComposer.cs:93-99 status'u sadece CustomerAppointmentRequestQueryService'e geçiriyor; :83-88 ConfirmedAppointmentQueryService.GetOwnAsync'in status parametresi bile yok (ConfirmedAppointmentQueryService.cs:47-67). status=PendingApproval gönderilse bile tüm randevular yine döner.

**Çözüm:** Backend: GetOwnAsync'e status parametresi eklenecek. Frontend: bu düzelene kadar backend status filtresine GÜVENİLMEYECEK — appointment-history TEK çağrıyla çekilip ItemType ve Status ile client-side ayrılacak.

#### 8. [Frontend] Şube-scoped BranchManager, branchId göndermeden liste çağırırsa 403 alır (boş ekran değil, hata)

**Kanıt:** TenantBookingAuthorizationService.cs:42-46 — BranchManager'ın m.BranchId'si doluysa ve çağrıda branchId yoksa koşul (m.BranchId is null || (branchId is not null && m.BranchId == branchId)) false döner → Forbidden. Fail-closed tasarım, backend bug'ı değil, frontend sözleşmesi.

**Çözüm:** GET /api/business/context yanıtındaki membership.branchId dolu ise, tüm business listeleme çağrılarına branchId ZORUNLU eklenecek. Bunu tek bir BranchScopeProvider/hook içine kapsülle; ham fetch yasak.

---

## 3. Faz 0 — Hijyen (tasarımdan ÖNCE)

> Kırık temel üzerine tasarım yapılmaz. `main` şu an **kırmızı** (9 typecheck hatası, 3 canlı 404).

- [1] `src/app/(` adlı UZANTISIZ, GEÇERSİZ dosyayı SİL. İçeriği login page'in bir kopyası — bozuk bir shell yönlendirmesinin artığı. Next bunu rota olarak almıyor ama repo kirliliği ve tsc gürültüsü yaratıyor.
- [2] 9 TYPECHECK HATASINI KAPAT: hepsi tek dosyada — src/features/business/components/business-settings-page.tsx. `routes` (satır 200,206,212,218,224,230,236) ve `Link` (satır 273,278) import EDİLMEMİŞ. İki import satırı ekle: `import Link from "next/link";` ve `import { routes } from "@/shared/config/routes";`. Sonra `pnpm typecheck` YEŞİL olmalı ve CI'a zorunlu adım olarak bağlanmalı.
- [3] CANLI 404 #1 — /panel/talepler: panel-shell.tsx:68 sidebar'da 'Talepler' öğesi (üstelik bekleyen sayısı rozetiyle) bu rotaya link veriyor ama src/app/panel/talepler/page.tsx YOK. Sayfa yazılana kadar en azından /panel'e redirect koy; kalıcı çözüm Adım 2'de gerçek sayfa.
- [4] CANLI 404 #2 — /panel/randevular: business-calendar-page.tsx:117 takvimden bu rotaya link veriyor, src/app/panel/randevular/page.tsx YOK.
- [5] CANLI 404 #3 — /hesabim: customer-shell.tsx:20 'Genel bakış' nav öğesi routes.customer.dashboard'a (= /hesabim) link veriyor, src/app/hesabim/page.tsx YOK. Çözüm: /hesabim → /hesabim/randevular server-side redirect (yeni sayfa yazmadan kapanır).
- [6] routes.ts'teki 12 ÖLÜ ROTA GİRDİSİNİ SİL: business.abuseReports, business.messaging, business.reviews, business.appointmentOperations, platform.auditLog, platform.identities, platform.sanctions, platform.support, customer.reviews, public.booking, public.businessReviews, auth.emailVerify. Hiçbirinin sayfası yok; sözlükte durmaları gelecekte yeni 404 üretir. Kalan her routes.* girdisi bir page.tsx ile eşleşmeli — bunu bir test kilitlesin.
- [7] /gelis ROL DAĞITIMI YANLIŞ HEDEFE GİDİYOR: gelis/page.tsx:32 platform admin'i routes.platform.abuse'e (/platform/abuse) atıyor. Abuse MVP dışı ve o şerit siliniyor → hedef /platform/tenantlar olacak. Aynı dosyada müşteri hedefi routes.customer.requests → /hesabim/randevular olacak (randevular birincil, talepler ikincil).
- [8] MVP DIŞI ŞERİTLERİ SÖK (sayfa + component + api client, hepsi): src/app/platform/abuse/**, src/app/platform/itirazlar/, src/app/hesabim/itirazlar/, src/app/panel/yetenekler/ (backend write-only: skill atama var, mevcut seçimi okuyan GET YOK → çizilemez). Bunlara bağlı componentler: platform-abuse-page, platform-appeals-page, platform-appeal-review-dialog, platform-report-review-dialog, platform-sanction-apply-dialog, platform-closure-proposal-dialog, platform-closure-review-dialog, platform-user-abuse-overview-page, customer-abuse-page, get-abuse-overview, get-platform-abuse-overview, get-platform-appeals-overview, get-platform-user-abuse-overview.
- [9] ÖLÜ/YASAKLI UI KODU SİL: src/shared/ui/tooltip.tsx (kural 8 — dokunmatikte tooltip yok, bilgi görünür etikete), src/shared/ui/animated-background.tsx (dark glassmorphism kararı iptal, light-first'e geçiliyor), src/features/public-discovery/components/enhanced-gallery.tsx (galeri backend'i YOK — profil yanıtındaki gallery alanı MVP'de doldurulmuyor).
- [10] /panel/kaynak-turleri ve /panel/kaynaklar sayfalarını TEK sayfaya (/panel/koltuklar, iki sekme) birleştir; eski iki rotayı redirect et. Backend entity isimleri üst seviye navdan çıkar.
- [11] /panel/calisma-saatleri → /panel/saatler'e yeniden adlandır + eskisini redirect et (URL'ler de kullanıcı dilinde olmalı).
- [12] BACKEND ÖLÜ KODU (bilgi olarak, FE işi değil): StaffSkillService.cs:98 GetSkillIdsForStaffAsync yazılmış ama HİÇBİR YERDEN çağrılmıyor. Yetkinlik UI'ı MVP sonrasına kaldığı için şimdilik dokunulmuyor; endpoint'e bağlanması ~yarım gün.

---

## 4. Sidebar — bilgi mimarisi

> **İlke:** Sidebar salon sahibinin **zihinsel modelini** yansıtır, backend şemasını değil.
> Salon sahibi "kaynak tipi" düşünmez — **koltuk** düşünür.

```
PANEL (İşletme kabuğu) — shadcn Sidebar, tablette kalıcı, telefonda Sheet
═══════════════════════════════════════════════════════════════════════

[İşletme değiştirici — Select]   ← SADECE birden fazla tenant üyeliği varsa render edilir
[Şube değiştirici — Select]      ← SADECE birden fazla şube varsa render edilir
                                    (tek tenant / tek şube = seçici HİÇ çizilmez, sadece isim metni)

GÜNLÜK İŞ                                   (capability: business.appointmentRequests.manage)
├── Bugün                        /panel
├── Talepler                (3)  /panel/talepler        ← rozet: bekleyen sayısı
├── Takvim                       /panel/takvim
└── Randevular                   /panel/randevular

İŞLETMEM                                    (capability: business.settings.manage — SADECE BusinessOwner)
├── Ekip                         /panel/personel
├── Hizmetler ve fiyatlar        /panel/hizmetler
├── Çalışma saatleri             /panel/saatler
├── Koltuklar ve ekipman         /panel/koltuklar
├── Şubeler                      /panel/subeler
└── İşletme ayarları             /panel/ayarlar

[Alt: Avatar + ad + e-posta + Çıkış]

GÖRÜNMEYEN AMA MANİFESTTE (hidden: true — yetki tablosunda VARLAR):
  /panel/talepler/[appointmentRequestId]     → business.appointmentRequests.manage
  /panel/randevular/[appointmentId]          → business.appointmentRequests.manage
  /panel/hizmetler/[serviceId]               → business.settings.manage
  /panel/personel/[staffId]                  → business.settings.manage

MVP'DE TEK İŞLETME ROLÜ VAR: BusinessOwner  (bkz. Karar K1)
  → BranchManager ve Staff rolleri V2'ye bırakıldı.
  → Yukarıdaki ağacın TAMAMINI tek rol görür; koşullu dal yok.
  → ZORUNLU: /platform/tenantlar/[id]/uyeler rol seçicisi SADECE "BusinessOwner" sunacak.
    BranchManager/Staff atanırsa o kullanıcı her öğede 403 duvarına çarpar
    (Staff'ın capability listesi backend'de zaten BOŞ DİZİ).
  → Nav yine de capability'den türetilir ve can() fail-closed kalır: amacı,
    V2'de rol eklerken "izin yazmayı unuttum → herkese açıldı" hatasını
    DERLEME ZAMANINDA yakalamaktır.

BACKEND ENTITY İSİMLERİ ÜST SEVİYEDEN KALDIRILDI:
  "Kaynak türleri"  → "Koltuklar ve ekipman" sayfasının 2. sekmesi
  "Kaynaklar"       → "Koltuklar ve ekipman" sayfasının 1. sekmesi
  "Yetenekler"      → MENÜDEN TAMAMEN SİLİNDİ (backend write-only: atama var, okuma GET'i yok)


MÜŞTERİ kabuğu (üst yatay nav, telefonda alt tab bar)
═════════════════════════════════════════════════════
├── Randevularım    /hesabim/randevular   ← /hesabim buraya redirect (canlı 404 kapanır)
├── Taleplerim      /hesabim/talepler
└── Profilim        /hesabim/profil
    → "İtirazlar" SİLİNDİ (moderasyon MVP dışı)


PUBLIC kabuğu (üst navbar)
══════════════════════════
├── Ana sayfa       /
├── Keşfet          /kesfet
└── [Giriş] / [Hesabım]     ← oturum durumuna göre
```

---

## 5. Rol bazlı navigasyon — nasıl çalışır

### 1. Capability union — BACKEND'İN GERÇEK DEĞERLERİ

Backend `BusinessCapabilityNames.cs`'te 3 sabit var ve `BusinessContextComposer.CreateCapabilities` role göre dağıtıyor. Bunu birebir kopyalıyoruz, UYDURMUYORUZ:

```ts
// src/shared/auth/capabilities.ts
// KAYNAK: src/Apps/RezSaaS.Api/Business/BusinessCapabilityNames.cs
//         src/Apps/RezSaaS.Api/Business/BusinessContextComposer.cs:45-61
export const CAPABILITIES = [
  "business.appointmentRequests.manage",   // BusinessOwner + BranchManager
  "business.settings.manage",              // SADECE BusinessOwner
  "business.appointmentRequests.reportAbuse", // MVP DIŞI, hiçbir nav öğesi kullanmaz
] as const;
export type Capability = (typeof CAPABILITIES)[number];

// Panel dışı kabuklar için sentetik kapılar (backend'de capability adı yok, auth durumu var)
export type Gate = Capability | "public.anon" | "customer.self" | "platform.admin";
```

KRİTİK GERÇEK: `business.settings.manage` yalnızca BusinessOwner'da. Katalog, personel, şube, kaynak, kaynak-tipi, çalışma-saati, ayar ve review composer'larının **11'i birden** `CanManageBusinessSettingsAsync` çağırıyor ve o metot `Role == BusinessOwner` şartını arıyor (TenantBookingAuthorizationService.cs:74-96). Yani BranchManager bu 8 sayfanın hepsinde 403 alır.

### 2. Nav-manifest tipi — DİNAMİK DETAY ROTALARI DAHİL (hidden: true)

```ts
// src/shared/nav/manifest.ts
export type NavNode = {
  id: string;
  /** Statik rota veya Next dinamik segment kalıbı. */
  route: string;                    // "/panel/hizmetler/[serviceId]"
  label: string;
  shell: "public" | "auth" | "customer" | "panel" | "platform";
  /** Sayfaya girmek için ZORUNLU kapı. Fail-closed: undefined = erişilemez. */
  gate: Gate;
  /** true → menüde çizilmez ama yetki tablosunda ve guard'da VARDIR. */
  hidden?: boolean;
  /** true → BranchManager bu sayfayı açarken branchId zorunlu (yoksa backend 403). */
  branchScoped?: boolean;
  group?: "gunluk" | "isletmem";
  badge?: "pendingRequests";
};

export const NAV: readonly NavNode[] = [
  // GÜNLÜK İŞ
  { id: "panel.today",     route: "/panel",            label: "Bugün",      shell: "panel", gate: "business.appointmentRequests.manage", group: "gunluk", branchScoped: true },
  { id: "panel.requests",  route: "/panel/talepler",   label: "Talepler",   shell: "panel", gate: "business.appointmentRequests.manage", group: "gunluk", branchScoped: true, badge: "pendingRequests" },
  { id: "panel.calendar",  route: "/panel/takvim",     label: "Takvim",     shell: "panel", gate: "business.appointmentRequests.manage", group: "gunluk", branchScoped: true },
  { id: "panel.appts",     route: "/panel/randevular", label: "Randevular", shell: "panel", gate: "business.appointmentRequests.manage", group: "gunluk", branchScoped: true },

  // GÜNLÜK İŞ — HIDDEN DETAY ROTALARI (eleştirmenin bulduğu yapısal delik burada kapanır)
  { id: "panel.request.detail", route: "/panel/talepler/[appointmentRequestId]", label: "Talep detayı",   shell: "panel", gate: "business.appointmentRequests.manage", hidden: true, branchScoped: true },
  { id: "panel.appt.detail",    route: "/panel/randevular/[appointmentId]",      label: "Randevu detayı", shell: "panel", gate: "business.appointmentRequests.manage", hidden: true, branchScoped: true },

  // İŞLETMEM — HEPSİ BusinessOwner
  { id: "panel.staff",     route: "/panel/personel",   label: "Ekip",                  shell: "panel", gate: "business.settings.manage", group: "isletmem", branchScoped: true },
  { id: "panel.services",  route: "/panel/hizmetler",  label: "Hizmetler ve fiyatlar", shell: "panel", gate: "business.settings.manage", group: "isletmem" },
  { id: "panel.hours",     route: "/panel/saatler",    label: "Çalışma saatleri",      shell: "panel", gate: "business.settings.manage", group: "isletmem", branchScoped: true },
  { id: "panel.seats",     route: "/panel/koltuklar",  label: "Koltuklar ve ekipman",  shell: "panel", gate: "business.settings.manage", group: "isletmem", branchScoped: true },
  { id: "panel.branches",  route: "/panel/subeler",    label: "Şubeler",               shell: "panel", gate: "business.settings.manage", group: "isletmem" },
  { id: "panel.settings",  route: "/panel/ayarlar",    label: "İşletme ayarları",      shell: "panel", gate: "business.settings.manage", group: "isletmem" },

  // İŞLETMEM — HIDDEN DETAY ROTALARI
  { id: "panel.service.detail", route: "/panel/hizmetler/[serviceId]", label: "Varyantlar",       shell: "panel", gate: "business.settings.manage", hidden: true },
  { id: "panel.staff.detail",   route: "/panel/personel/[staffId]",    label: "Personel detayı",  shell: "panel", gate: "business.settings.manage", hidden: true, branchScoped: true },

  // MÜŞTERİ
  { id: "cust.appts",   route: "/hesabim/randevular", label: "Randevularım", shell: "customer", gate: "customer.self" },
  { id: "cust.reqs",    route: "/hesabim/talepler",   label: "Taleplerim",   shell: "customer", gate: "customer.self" },
  { id: "cust.profile", route: "/hesabim/profil",     label: "Profilim",     shell: "customer", gate: "customer.self" },

  // PLATFORM (onboarding — MVP'nin var olabilmesi için zorunlu)
  { id: "plat.tenants", route: "/platform/tenantlar", label: "İşletmeler", shell: "platform", gate: "platform.admin" },
  { id: "plat.members", route: "/platform/tenantlar/[tenantId]/uyeler", label: "Üyeler", shell: "platform", gate: "platform.admin", hidden: true },
];
```

Manifest bir TEST ile kilitlenir: `src/app` altındaki her `page.tsx`'in rota kalıbı NAV içinde bir düğümle eşleşmek ZORUNDA. Eşleşmeyen sayfa = build kırılır. Bugünkü hata (12 rota manifest dışında, 3'ü canlı 404) böyle bir testle bir daha oluşamaz.

### 3. Fail-closed can()

```ts
// src/shared/auth/can.ts
export type Viewer =
  | { kind: "anon" }
  | { kind: "customer" }
  | { kind: "platformAdmin"; stepUpValid: boolean }
  | { kind: "business"; tenantId: string; branchId: string | null;
      capabilities: readonly string[] };  // GET /api/business/context → membership.capabilities

/** Fail-closed: bilinmeyen kapı, bilinmeyen viewer, eksik veri → HER ZAMAN false. */
export function can(viewer: Viewer, gate: Gate): boolean {
  switch (gate) {
    case "public.anon":
      return true;
    case "customer.self":
      return viewer.kind === "customer" || viewer.kind === "business";
    case "platform.admin":
      return viewer.kind === "platformAdmin" && viewer.stepUpValid === true;
    case "business.appointmentRequests.manage":
    case "business.settings.manage":
    case "business.appointmentRequests.reportAbuse":
      return viewer.kind === "business" && viewer.capabilities.includes(gate);
    default:
      return false;   // ← yeni bir gate eklenip burada ele alınmazsa ERİŞİM KAPALI
  }
}

export function visibleNav(viewer: Viewer, shell: NavNode["shell"]) {
  return NAV.filter((n) => n.shell === shell && !n.hidden && can(viewer, n.gate));
}
```
`capabilities` dizisi UYDURULMAZ — `GET /api/business/context` yanıtındaki `tenants[].capabilities` aynen kullanılır. Frontend "BusinessOwner mı?" diye role bakmaz; capability'ye bakar. Böylece backend rol→capability haritasını değiştirdiğinde frontend kendiliğinden doğru kalır.

### 4. Guard katmanları (4 kat, hepsi zorunlu)

1. **middleware.ts** — `.AspNetCore.Identity.Application` cookie'si yoksa `/panel/*`, `/hesabim/*`, `/platform/*` → `/giris?returnTo=...`. Sadece "oturum var mı" der, yetki bilmez. (Bugün zaten var.)
2. **Sunucu sayfa guard'ı** — her korumalı `page.tsx`'in İLK satırı:
   ```ts
   const viewer = await requireViewer();                  // session bootstrap + business context
   requireGate(viewer, "business.settings.manage");       // yoksa notFound() ya da 403 ekranı
   ```
   Bu, URL'yi elle yazan BranchManager'ı durdurur. Nav'ı gizlemek yetmez.
3. **Nav filtresi** — `visibleNav()`. Yetkisi olmayan öğe MENÜDE HİÇ ÇİZİLMEZ (soluk/pasif değil, YOK). `hidden: true` düğümler zaten hiç çizilmez.
4. **Aksiyon seviyesi** — her buton kendi capability'sini sorar: `can(viewer, "business.settings.manage") && <Button>Fiyatı düzenle</Button>`. Endpoint'i olmayan aksiyon (walk-in randevu oluştur, randevuyu müşteri iptal et, yetkinlik ata) hiçbir koşulda render EDİLMEZ.

### 5. Şube kapsamı (branchScoped) — 403'ten korunma sözleşmesi

`GET /api/business/context` → `membership.branchId` DOLU ise (şube-scoped BranchManager), tüm business listeleme çağrılarına `branchId` ZORUNLU eklenmeli; yoksa `TenantBookingAuthorizationService.cs:42-46` false döner ve backend **403** verir (boş liste değil, hata). Bu bir backend bug'ı değil, fail-closed tasarım.

```ts
// src/shared/api/business-client.ts — ham fetch YASAK, herkes bunu kullanır
export function createBusinessClient(viewer: Extract<Viewer, { kind: "business" }>) {
  const client = createTenantApiClient(viewer.tenantId);   // X-RezSaaS-Tenant header
  return {
    ...client,
    /** branchId'yi otomatik enjekte eder; branch-scoped üyede eksikse çağrıyı hiç yapmaz. */
    scopedQuery(q: Record<string, unknown> = {}) {
      if (viewer.branchId) return { ...q, branchId: viewer.branchId };
      return q;
    },
  };
}
```

### 6. Yetkisiz erişim davranışı (backend'in gerçek yanıtlarına göre)

| Durum | Backend | Frontend |
|---|---|---|
| Oturum yok | 401 | `/giris?returnTo=...` |
| Tenant üyeliği yok / başka tenant'ın kaydı | **404** (global query filter → context null → NotFound; varlık sızmaz) | "Kayıt bulunamadı" — tenant varlığını İMA ETMEYEN nötr mesaj |
| Aynı tenant, BAŞKA ŞUBE (BranchManager) | **403** | "Bu şubenin kaydına erişim yetkin yok" + Bugün'e dön |
| Yetersiz capability (BranchManager → /panel/hizmetler) | 403 | Menüde hiç görünmez; URL elle yazılırsa sunucu guard'ı 403 ekranı basar |
| Tenant askıya alınmış | 403 (IsActiveTenantAsync false) | "İşletme hesabın askıda" tam sayfa durum ekranı, tüm panel kilitli |
| Platform admin step-up süresi doldu (30 dk) | 403 | Step-up gate yeniden gösterilir (mevcut platform-step-up-gate korunur) |

Yetkisiz öğe için **pasif buton + tooltip YAPILMAZ** (kural 8): dokunmatikte tooltip yoktur ve "neden yapamıyorum" bilgisi kaybolur. Öğe ya tamamen yoktur, ya da pasifse nedeni butonun ALTINDA görünür metindir ("Randevu saati geçmeden tamamlandı işaretlenemez").

---

## 6. Sayfa envanteri (27)

### Public (3)

| Route | Ad | Durum | Roller | Mobil |
|---|---|---|---|---|
| `/` | Ana sayfa | MEVCUT-REFACTOR | Anonim, Musteri, BusinessOwner, BranchManager | Tek sütun. Hero + arama kutusu ekranın üst yarısında, klavye açıldığında arama kutusu görü |
| `/kesfet` | Keşfet | MEVCUT-REFACTOR | Anonim, Musteri, BusinessOwner, BranchManager | Filtreler üstte yatay sticky bar (chip'ler). Sonuçlar tek sütun kart listesi. Boş sonuçta  |
| `/isletme/[businessSlug]` | İşletme profili + Randevu al | MEVCUT-REFACTOR | Anonim (görüntüleme), Musteri (talep) | BİRİNCİL CİHAZ TELEFON. Profil bilgisi üstte, 'Randevu al' sticky bottom bar butonu. Buton |

### Auth (5)

| Route | Ad | Durum | Roller | Mobil |
|---|---|---|---|---|
| `/giris` | Giriş | MEVCUT-KORUNUR | Anonim | Tek sütun, dikey ortalı, max-w-sm. inputMode/autocomplete doğru (email, current-password). |
| `/kayit` | Kayıt | MEVCUT-KORUNUR | Anonim | Giriş ile aynı. Parola kuralları input'un altında GÖRÜNÜR liste olarak yazılır (tooltip/ip |
| `/sifremi-unuttum` | Şifremi unuttum | MEVCUT-KORUNUR | Anonim | Tek alan + tek buton. Başarıda sayfa içi Alert (toast değil — kullanıcı e-posta uygulaması |
| `/sifre-sifirla` | Şifre sıfırla | MEVCUT-KORUNUR | Anonim | Kod alanı inputMode=numeric/one-time-code. Başarıda /giris'e yönlendirme. |
| `/gelis` | Rol dağıtıcı (hidden) | MEVCUT-REFACTOR | Musteri, BusinessOwner, BranchManager, PlatformAdmin | Yalnızca tam ekran yükleme iskeleti. Hedef: tenant üyeliği varsa /panel, yoksa /hesabim/ra |

### Musteri (3)

| Route | Ad | Durum | Roller | Mobil |
|---|---|---|---|---|
| `/hesabim/randevular` | Randevularım | KIRIK-DUZELT | Musteri | Telefon birincil. Sekmeler yatay segmented control (Tabs). Her randevu bir kart: tarih+saa |
| `/hesabim/talepler` | Taleplerim | MEVCUT-REFACTOR | Musteri | Kart üstünde ExpiresAtUtc'den türeyen GERİ SAYIM ETİKETİ görünür metin olarak ('2 sa 14 dk |
| `/hesabim/profil` | Profilim | MEVCUT-REFACTOR | Musteri | Tek sütun. Salt-okunur alanlar (input değil, tanım listesi). Çıkış butonu kırmızı/destruct |

### Panel (14)

| Route | Ad | Durum | Roller | Mobil |
|---|---|---|---|---|
| `/panel` | Bugün | MEVCUT-REFACTOR | BusinessOwner, BranchManager | Tablet (1024 yatay) birincil: 2 sütun — solda bugünün zaman çizelgesi, sağda bekleyen tale |
| `/panel/talepler` | Talepler | YENI | BusinessOwner, BranchManager | TABLET BİRİNCİL. Tablette 2 panel: solda talep listesi (yoğunluklu tablo), sağda seçili ta |
| `/panel/talepler/[appointmentRequestId]` | Talep detayı (hidden) | YENI | BusinessOwner, BranchManager | Tam sayfa. Üstte breadcrumb ile Talepler'e dönüş. Aksiyonlar sticky bottom bar. 404 (tenan |
| `/panel/randevular` | Randevular | YENI | BusinessOwner, BranchManager | Tablette yoğun tablo (saat, müşteri, personel, hizmet, fiyat, durum). Telefonda tablo YOK  |
| `/panel/randevular/[appointmentId]` | Randevu detayı ve işlemleri (hidden) | YENI | BusinessOwner, BranchManager | Tablette tam sayfa 2 sütun (solda bilgi, sağda işlem paneli). Telefonda tek sütun; 5 işlem |
| `/panel/takvim` | Takvim | MEVCUT-REFACTOR | BusinessOwner, BranchManager | Tablette hafta görünümü (personel sütunları). Telefonda hafta görünümü İMKANSIZ — otomatik |
| `/panel/personel` | Ekip | MEVCUT-REFACTOR | BusinessOwner | Sayfa başında ŞUBE SEÇİCİ (Select) — branchId olmadan liste çekilemez, tek şube varsa otom |
| `/panel/personel/[staffId]` | Personel detayı ve izinleri (hidden) | YENI | BusinessOwner | Tablette 2 sütun (solda kimlik kartı, sağda izin listesi). Telefonda tek sütun, izinler ka |
| `/panel/hizmetler` | Hizmetler ve fiyatlar | MEVCUT-REFACTOR | BusinessOwner | Tablette tablo (hizmet adı, kategori, varyant sayısı, fiyat aralığı, durum). Telefonda kar |
| `/panel/hizmetler/[serviceId]` | Varyantlar: fiyat ve süre (hidden) | YENI | BusinessOwner | Tablette inline düzenlenebilir tablo satırları (dokunmatikte 44px satır yüksekliği). Telef |
| `/panel/saatler` | Çalışma saatleri | MEVCUT-REFACTOR | BusinessOwner | Şube seçici üstte. 7 gün = 7 satır; her satırda gün adı + 'Açık/Kapalı' Switch + iki saat  |
| `/panel/koltuklar` | Koltuklar ve ekipman | MEVCUT-REFACTOR | BusinessOwner | İki Tab: 'Koltuklar' (varsayılan) ve 'Koltuk tipleri'. Salon sahibi %90 ilk tabda kalır. T |
| `/panel/subeler` | Şubeler | MEVCUT-REFACTOR | BusinessOwner | Telefonda kart listesi, düzenle → full-screen Sheet. SAAT DİLİMİ ALANI ARTIK SERBEST METİN |
| `/panel/ayarlar` | İşletme ayarları | KIRIK-DUZELT | BusinessOwner | Tek sütun, bölümlere ayrılmış Card'lar. Uzun metin alanları (Description, PublicRules) tel |

### Platform (2)

| Route | Ad | Durum | Roller | Mobil |
|---|---|---|---|---|
| `/platform/tenantlar` | İşletmeler (platform admin) | MEVCUT-KORUNUR | PlatformAdmin | Masaüstü birincil (admin aracı). Tablette tablo yatay scroll container içinde. Telefon des |
| `/platform/tenantlar/[tenantId]/uyeler` | İşletme üyeleri (platform admin) | MEVCUT-KORUNUR | PlatformAdmin | Masaüstü birincil. Telefonda kart listesi fallback'i yeterli, ek yatırım yapılmaz. |


## 7. Aksiyon x Capability matrisi

| Sayfa | Aksiyon | Gerekli capability | Backend ucu |
|---|---|---|---|
| `/` | Arama kutusuna yazıp keşfe git | `public.anon` | `yok (client-side yönlendirme, /kesfet?q=)` |
| `/` | Öne çıkan işletme kartına tıkla | `public.anon` | `GET /api/public/businesses` |
| `/` | Giriş yap / Kayıt ol | `public.anon` | `yok (yönlendirme)` |
| `/kesfet` | Metinle ara | `public.anon` | `GET /api/public/businesses?search=` |
| `/kesfet` | Kategoriye göre filtrele | `public.anon` | `GET /api/public/businesses (categoryKey)` |
| `/kesfet` | İşletme kartını aç | `public.anon` | `yok (yönlendirme /isletme/{slug})` |
| `/isletme/[businessSlug]` | Profili ve hizmet/varyant fiyatlarını görüntüle | `public.anon` | `GET /api/public/businesses/{slug}/profile` |
| `/isletme/[businessSlug]` | Şube + hizmet varyantı seçip müsait saatleri ara | `public.anon` | `GET /api/public/businesses/{slug}/slots` |
| `/isletme/[businessSlug]` | Randevu talebi gönder (Idempotency-Key ile) | `customer.self (RequireAuthorization — anonim talep İMKANSIZ)` | `POST /api/public/businesses/{slug}/appointment-requests` |
| `/isletme/[businessSlug]` | Değerlendirmeleri oku | `public.anon` | `GET /api/public/businesses/{slug}/reviews` |
| `/giris` | E-posta + parola ile giriş | `public.anon` | `POST /api/auth/login (useCookies=true)` |
| `/giris` | Girişten sonra rol dağıtımı | `customer.self` | `GET /api/session/bootstrap` |
| `/kayit` | Hesap oluştur | `public.anon` | `POST /api/auth/register` |
| `/kayit` | Kayıt sonrası otomatik giriş | `public.anon` | `POST /api/auth/login` |
| `/sifremi-unuttum` | Sıfırlama e-postası iste | `public.anon` | `POST /api/auth/forgotPassword` |
| `/sifre-sifirla` | Yeni parolayı kaydet | `public.anon` | `POST /api/auth/resetPassword` |
| `/gelis` | Oturumu ve üyelikleri oku, yönlendir | `customer.self` | `GET /api/session/bootstrap + GET /api/business/context` |
| `/hesabim/randevular` | Yaklaşan / Geçmiş sekmeleri (ItemType=='Appointment', client-side filtre) | `customer.self` | `GET /api/customer/appointment-history` |
| `/hesabim/randevular` | Randevu detayını aç (drawer — ayrı fetch YOK, history item tüm alanları taşıyor) | `customer.self` | `yok (liste verisinden çizilir; GET /api/customer/appointments/{id} MEVCUT DEĞİL)` |
| `/hesabim/randevular` | Randevuyu iptal et | `customer.self` | `ENDPOINT YOK → BUTON GÖSTERİLMEZ. Blokaj-2 kapanınca POST /api/public/businesses/{slug}/appointments/{id}/cancel'a bağlanacak.` |
| `/hesabim/randevular` | İşletme profiline git (yeniden randevu al) | `public.anon` | `yok (yönlendirme)` |
| `/hesabim/talepler` | Bekleyen/geçmiş talepleri listele (ItemType=='AppointmentRequest') | `customer.self` | `GET /api/customer/appointment-history` |
| `/hesabim/talepler` | Talep detayını aç | `customer.self` | `GET /api/public/businesses/{slug}/appointment-requests/{appointmentRequestId}` |
| `/hesabim/talepler` | Bekleyen talebi iptal et (SADECE Status=='PendingApproval' iken buton görünür) | `customer.self` | `POST /api/public/businesses/{slug}/appointment-requests/{appointmentRequestId}/cancel` |
| `/hesabim/profil` | Hesap bilgisini görüntüle | `customer.self` | `GET /api/session/bootstrap` |
| `/hesabim/profil` | Çıkış yap | `customer.self` | `POST /api/auth/logout` |
| `/panel` | Bugünün randevularını gör | `business.appointmentRequests.manage` | `GET /api/business/appointments` |
| `/panel` | Bekleyen talep sayısını gör → Talepler'e git | `business.appointmentRequests.manage` | `GET /api/business/appointment-requests/pending` |
| `/panel` | İşletme (tenant) değiştir | `business.appointmentRequests.manage` | `GET /api/business/context` |
| `/panel/talepler` | Bekleyen talepleri listele (TTL geri sayımı + çakışma uyarısıyla) | `business.appointmentRequests.manage` | `GET /api/business/appointment-requests/pending` |
| `/panel/talepler` | Tüm talepleri statüye göre listele | `business.appointmentRequests.manage` | `GET /api/business/appointment-requests` |
| `/panel/talepler` | Talebi ONAYLA (Appointment doğar) | `business.appointmentRequests.manage` | `POST /api/business/appointment-requests/{appointmentRequestId}/approve` |
| `/panel/talepler` | Talebi REDDET | `business.appointmentRequests.manage` | `POST /api/business/appointment-requests/{appointmentRequestId}/decline` |
| `/panel/talepler` | Suistimal bildir | `business.appointmentRequests.reportAbuse` | `MVP DIŞI — buton çizilmez (kullanıcı kararı: moderasyon kontrol düzlemi yok)` |
| `/panel/talepler/[appointmentRequestId]` | Tek talebin tüm satırlarını (hizmet/süre/fiyat), müşteri (maskeli) ve TTL bilgisini gör | `business.appointmentRequests.manage` | `GET /api/business/appointment-requests/{appointmentRequestId}` |
| `/panel/talepler/[appointmentRequestId]` | Onayla | `business.appointmentRequests.manage` | `POST /api/business/appointment-requests/{appointmentRequestId}/approve` |
| `/panel/talepler/[appointmentRequestId]` | Reddet | `business.appointmentRequests.manage` | `POST /api/business/appointment-requests/{appointmentRequestId}/decline` |
| `/panel/randevular` | Randevuları tarih aralığı + statü ile listele | `business.appointmentRequests.manage` | `GET /api/business/appointments` |
| `/panel/randevular` | Randevu detayına git | `business.appointmentRequests.manage` | `GET /api/business/appointments/{appointmentId}` |
| `/panel/randevular/[appointmentId]` | Detayı gör (satırlar, fiyat snapshot'ı, personel, kaynak, müşteri maskeli) | `business.appointmentRequests.manage` | `GET /api/business/appointments/{appointmentId}` |
| `/panel/randevular/[appointmentId]` | İptal et (sadece Confirmed iken aktif) | `business.appointmentRequests.manage` | `POST /api/business/appointments/{appointmentId}/cancel` |
| `/panel/randevular/[appointmentId]` | Tamamlandı işaretle (EndUtc <= şimdi değilse buton PASİF — APPOINTMENT_COMPLETE_TOO_EARLY) | `business.appointmentRequests.manage` | `POST /api/business/appointments/{appointmentId}/complete` |
| `/panel/randevular/[appointmentId]` | Gelmedi işaretle (StartUtc <= şimdi değilse buton PASİF — APPOINTMENT_NO_SHOW_TOO_EARLY) | `business.appointmentRequests.manage` | `POST /api/business/appointments/{appointmentId}/no-show` |
| `/panel/randevular/[appointmentId]` | Not ekle/güncelle | `business.appointmentRequests.manage` | `POST /api/business/appointments/{appointmentId}/notes` |
| `/panel/randevular/[appointmentId]` | Yeniden planla (yeni saat seç; eski kayıt Rebooked olur, müşteri hesabı eskiden kopyalanır) | `business.appointmentRequests.manage` | `POST /api/business/appointments/{appointmentId}/rebook` |
| `/panel/takvim` | Gün/hafta aralığındaki randevuları gör | `business.appointmentRequests.manage` | `GET /api/business/appointments` |
| `/panel/takvim` | Randevu bloğuna tıkla → detay | `business.appointmentRequests.manage` | `GET /api/business/appointments/{appointmentId}` |
| `/panel/takvim` | Boş slota tıklayıp randevu OLUŞTUR | `—` | `ENDPOINT YOK (işletme tarafında Appointment yaratan POST yok). BOŞ SLOT TIKLANABİLİR DEĞİL, '+' butonu çizilmez.` |
| `/panel/personel` | Şubedeki personeli listele (branchId ZORUNLU — uç şube altında nested) | `business.settings.manage` | `GET /api/business/branches/{branchId}/staff` |
| `/panel/personel` | Personel ekle | `business.settings.manage` | `POST /api/business/branches/{branchId}/staff` |
| `/panel/personel` | Personel adını düzenle | `business.settings.manage` | `PATCH /api/business/branches/{branchId}/staff/{staffId} — DİKKAT: Blokaj-3 kapanana kadar SESSİZ NO-OP. Backend Rename fix'i gelmeden bu form AÇILMAZ.` |
| `/panel/personel` | Personeli arşivle | `business.settings.manage` | `POST /api/business/branches/{branchId}/staff/{staffId}/archive` |
| `/panel/personel` | Personelin UserAccountId'sini göster/bağla | `—` | `GÖSTERİLMEZ. Alan nullable, doğrulanmamış, TenantMembership'e FK yok — UI'da açmak yanlış zihinsel model kurar.` |
| `/panel/personel/[staffId]` | Personel bilgisini gör | `business.settings.manage` | `GET /api/business/branches/{branchId}/staff/{staffId}` |
| `/panel/personel/[staffId]` | İzin/müsaitsizlik listesini gör | `business.settings.manage` | `GET /api/business/staff/{staffMemberId}/unavailable` |
| `/panel/personel/[staffId]` | İzin ekle (başlangıç, bitiş, sebep) | `business.settings.manage` | `POST /api/business/staff/{staffMemberId}/unavailable` |
| `/panel/personel/[staffId]` | İzni sil | `business.settings.manage` | `DELETE /api/business/staff/{staffMemberId}/unavailable/{unavailableId}` |
| `/panel/personel/[staffId]` | Yetkinlik (skill) ata/kaldır | `business.settings.manage` | `MVP DIŞI — POST/DELETE /api/business/staff/{id}/skills VAR ama MEVCUT SEÇİMİ OKUYAN GET YOK (StaffSkillService.GetSkillIdsForStaffAsync ölü kod). Write-only ekran çizilemez, sekme AÇILMAZ.` |
| `/panel/hizmetler` | Hizmetleri listele | `business.settings.manage` | `GET /api/business/services` |
| `/panel/hizmetler` | Hizmet ekle | `business.settings.manage` | `POST /api/business/services` |
| `/panel/hizmetler` | Hizmet adı/kategorisini düzenle | `business.settings.manage` | `PATCH /api/business/services/{serviceId}` |
| `/panel/hizmetler` | Hizmeti arşivle (SİL butonu YOK — backend'de DELETE yok) | `business.settings.manage` | `POST /api/business/services/{serviceId}/archive` |
| `/panel/hizmetler` | Hizmetin varyantlarına (fiyat/süre) git | `business.settings.manage` | `yok (yönlendirme /panel/hizmetler/{serviceId})` |
| `/panel/hizmetler/[serviceId]` | Varyantları listele | `business.settings.manage` | `GET /api/business/services/{serviceId}/variants` |
| `/panel/hizmetler/[serviceId]` | Varyant ekle (ad, süre, fiyat, gereken koltuk tipi) | `business.settings.manage` | `POST /api/business/services/{serviceId}/variants` |
| `/panel/hizmetler/[serviceId]` | Fiyat/süre düzenle (5 alan birlikte gönderilir; CurrencyCode UI'da AÇILMAZ, sabit 'TRY') | `business.settings.manage` | `PATCH /api/business/services/{serviceId}/variants/{variantId}` |
| `/panel/hizmetler/[serviceId]` | Varyantı sil | `business.settings.manage` | `DELETE /api/business/services/{serviceId}/variants/{variantId}` |
| `/panel/hizmetler/[serviceId]` | Varyanta gereken yetkinlik ata | `business.settings.manage` | `MVP DIŞI — POST/DELETE .../required-skills/{skillId} VAR ama BusinessVariantResponse'ta RequiredSkillIds YOK ve GET yok. Hangi kutunun işaretli olduğu okunamaz → çizilmez.` |
| `/panel/saatler` | Haftalık saatleri gör | `business.settings.manage` | `GET /api/business/branches/{branchId}/working-hours` |
| `/panel/saatler` | Bir günün saatlerini kaydet (açılış, kapanış, kapalı) | `business.settings.manage` | `PUT /api/business/branches/{branchId}/working-hours/{dayOfWeek}` |
| `/panel/saatler` | Bir günün saatini kaldır | `business.settings.manage` | `DELETE /api/business/branches/{branchId}/working-hours` |
| `/panel/koltuklar` | Koltuk tiplerini listele/ekle/sil (örn. 'Berber koltuğu', 'Yıkama ünitesi') | `business.settings.manage` | `GET/POST /api/business/resource-types, DELETE /api/business/resource-types/{resourceTypeId}` |
| `/panel/koltuklar` | Şubedeki koltukları listele | `business.settings.manage` | `GET /api/business/branches/{branchId}/resources` |
| `/panel/koltuklar` | Koltuk ekle | `business.settings.manage` | `POST /api/business/branches/{branchId}/resources` |
| `/panel/koltuklar` | Koltuğu düzenle | `business.settings.manage` | `PATCH /api/business/branches/{branchId}/resources/{resourceId}` |
| `/panel/koltuklar` | Servis dışı bırak / geri al | `business.settings.manage` | `POST /api/business/branches/{branchId}/resources/{resourceId}/out-of-service, POST .../restore` |
| `/panel/subeler` | Şubeleri listele | `business.settings.manage` | `GET /api/business/branches` |
| `/panel/subeler` | Şube ekle (ad, slug, SAAT DİLİMİ) | `business.settings.manage` | `POST /api/business/branches` |
| `/panel/subeler` | Şube bilgisini düzenle | `business.settings.manage` | `PATCH /api/business/branches/{branchId}` |
| `/panel/subeler` | Slot ayarlarını düzenle (slot aralığı vb.) | `business.settings.manage` | `PATCH /api/business/branches/{branchId}/slot-settings` |
| `/panel/subeler` | Şubeyi arşivle | `business.settings.manage` | `POST /api/business/branches/{branchId}/archive` |
| `/panel/ayarlar` | Ayarları oku | `business.settings.manage` | `GET /api/business/settings` |
| `/panel/ayarlar` | Profili güncelle (DisplayName, Description, PublicRules, SeoTitle, SeoDescription, StaffDisplayPolicy) | `business.settings.manage` | `PATCH /api/business/settings` |
| `/panel/ayarlar` | Halka açık profili önizle | `public.anon` | `yok (yeni sekmede /isletme/{slug})` |
| `/platform/tenantlar` | Tenant listesi | `platform.admin (step-up MFA)` | `GET /api/platform/tenants (mevcut platform composer)` |
| `/platform/tenantlar` | Yeni işletme aç (provision dialog) | `platform.admin` | `POST /api/platform/tenants` |
| `/platform/tenantlar` | Üyelere git | `platform.admin` | `yok (yönlendirme)` |
| `/platform/tenantlar/[tenantId]/uyeler` | Üyeleri listele | `platform.admin` | `GET /api/platform/tenants/{tenantId}/memberships` |
| `/platform/tenantlar/[tenantId]/uyeler` | Üye ekle (BusinessOwner / BranchManager + branchId) | `platform.admin` | `POST /api/platform/tenants/{tenantId}/memberships` |
| `/platform/tenantlar/[tenantId]/uyeler` | Üyeliği askıya al/kaldır | `platform.admin` | `PATCH /api/platform/tenants/{tenantId}/memberships/{membershipId}` |


---

## 8. Component stratejisi — shadcn/ui

> **shadcn/ui bir bağımlılık değildir.** Kaynak kodu repo'ya kopyalanır, dosyalar **senin** olur.
> Altında Radix (erişilebilirlik) + Tailwind var — yani zaten planlanan mimarinin ta kendisi. MIT.

**Reddedilenler:** DevExtreme, `@dataliva/livalib`, Syncfusion, Bryntum, Kendo, AG Grid Enterprise (hepsi ticari/lisanslı).
**FullCalendar da reddedildi** — core MIT ama `resource-timeline` eklentisi **premium (ücretli)** ve bizim en kritik ekranımız tam olarak o.

### shadcn/ui'dan alınacaklar

`button — tüm aksiyonlar`, `input, label, textarea — form alanları`, `form (react-hook-form + zod resolver) — 12 formun tamamı`, `select — şube/tenant/kategori/süre seçicileri`, `dialog — masaüstü modal (personel ekle, varyant ekle, tenant provision)`, `sheet — MOBİL BİRİNCİL modal (rezervasyon akışı, randevu detayı, filtreler). Tablet portre + telefonda dialog yerine bunu kullan.`, `alert-dialog — yıkıcı onaylar (iptal et, gelmedi, arşivle, çıkış). Onaysız yıkıcı aksiyon YOK.`, `sidebar — shadcn'in kendi sidebar bloğu; panel-shell'in elle yazılmış collapse/mobile mantığı SÖKÜLÜR, buna devredilir`, `tabs — /hesabim sekmeleri, /panel/koltuklar iki bölümü, /panel/talepler statü sekmeleri`, `table — tablet yoğun listeler (telefonda kullanılmaz)`, `badge — statü rozetleri temeli`, `card — telefon liste öğeleri, panel kutuları`, `avatar — personel ve oturum`, `calendar — gün seçici (rezervasyon, izin, rebook)`, `popover — takvim/tarih açılırları`, `dropdown-menu — randevu işlemleri menüsü`, `command — /kesfet arama`, `sonner (toast) — kaydetme geri bildirimi`, `skeleton — tüm server component yükleme durumları`, `separator, scroll-area, switch, checkbox, radio-group, collapsible, breadcrumb, pagination, alert, empty`

### Sıfırdan yazılacaklar (sadece domain — 8 kalem)

| Component | Gün | Neden |
|---|---|---|
| **SlotPicker (gün şeridi + saat ızgarası)** | 2 | shadcn'de karşılığı yok. GET /slots yanıtındaki LocalStart string'inden HH:mm keser — TARAYICIDA SAAT DİLİMİ DÖNÜŞÜMÜ YAPMAZ (mevcut public-booking-panel.tsx:734-740 mantığı korunur, doğrudur). Mobilde 3 sütun, min 44px dokunma hedefi. StaffCandidates'ten personel seçimi de burada. |
| **RequestInboxCard (TTL geri sayım + çakışma sinyali)** | 1.5 | MVP'nin kalbi. Mevcut request-ttl.ts ve business-request-conflicts.ts iş mantığı KORUNUR, sadece görsel kabuk shadcn Card+Badge üzerine yeniden yazılır. TTL ve çakışma bilgisi GÖRÜNÜR ETİKET (tooltip yasak). |
| **StaffDayCalendar (personel sütunlu gün/hafta ızgarası)** | 3 | shadcn Calendar sadece tarih seçicidir, zaman çizelgesi değil. Dış kütüphane (FullCalendar vb.) getirilmez — bundle ve tema kontrolü kaybı. Tablette personel sütunları, telefonda tek gün. Bloklar min 44px. |
| **VariantPriceRow (fiyat/süre satırı)** | 1 | Backend PATCH'i aslında PUT — 5 alanın hepsi her istekte gitmeli. Bu tuzağı componentin içine kapsüller: kısmi gönderim yapısal olarak imkansız olur. CurrencyCode dışarı açılmaz, sabit 'TRY' basar. |
| **WeeklyHoursGrid (7 gün x açık/kapalı/saat)** | 1 | Gün bazlı PUT ucuna (working-hours/{dayOfWeek}) birebir oturan, satır satır kaydeden ızgara. 'Pazartesiyi haftaya kopyala' kısayolu 7 PUT atar. |
| **AppointmentStatusBadge** | 0.5 | Backend'in 5 Appointment + 6 AppointmentRequest statüsünü tek görsel dile çevirir (Confirmed/Cancelled/Completed/NoShow/Rebooked + PendingApproval/Approved/Declined/Expired/Superseded/CancelledByCustomer). Mevcut status-badge.tsx bunun yerine geçer, shadcn Badge üzerine kurulur. Renk TEK sinyal olmaz — metin her zaman yazılır (erişilebilirlik). |
| **TenantBranchSwitcher** | 0.5 | Tenant + şube seçimini tek yerde toplar ve BranchManager'da şubeyi KİLİTLER. branchId'yi tüm business çağrılarına enjekte eden context'i besler — bu olmadan branch-scoped müdür her listede 403 alır. |
| **GateBoundary (sunucu tarafı yetki sınırı)** | 0.5 | requireViewer() + requireGate() sarmalayıcısı. 404 (tenant dışı) ve 403 (şube/capability) farklı ekranlara ayrılır. Her korumalı sayfanın ilk satırı bu olur. |

### Kurulum adımları

1. 1) FAZ 0 hijyeni bitmeden BAŞLAMA (typecheck yeşil, 3 canlı 404 kapalı, 'src/app/(' silinmiş olmalı).
2. 2) `pnpm dlx shadcn@latest init` — Tailwind v4 (projede zaten 4.3.0), style: new-york, base color: neutral, CSS variables: yes, RSC: yes, alias: @/shared/ui. components.json repoya commit edilir.
3. 3) Bağımlılıklar: @radix-ui/* (shadcn kendi çeker), class-variance-authority, tailwind-merge, lucide-react, react-hook-form, zod, @hookform/resolvers, sonner, vaul (sheet/drawer). date-fns-tz EKLENMEZ — native Intl ve mevcut date-time.ts doğru çalışıyor.
4. 4) globals.css'te @theme bloğu: LIGHT-FIRST token seti + prefers-color-scheme dark override + [data-theme] manuel geçiş. Mevcut --rs-* değişkenleri shadcn'in --background/--foreground/--primary token'larına EŞLENİR (tek geçişte, bulk find-replace ile). animated-background.tsx ve dark glassmorphism efektleri SİLİNİR.
5. 5) `pnpm dlx shadcn@latest add button input label textarea form select dialog sheet alert-dialog sidebar tabs table badge card avatar calendar popover dropdown-menu command sonner skeleton separator scroll-area switch checkbox radio-group collapsible breadcrumb pagination alert` — tek komut.
6. 6) TOOLTIP EKLENMEZ ve mevcut src/shared/ui/tooltip.tsx SİLİNİR (kural 8). Bilgi taşıyan her tooltip görünür etikete/yardım metnine dönüştürülür.
7. 7) design-system-contract.test.ts güncellenir: (a) tooltip importu olan dosya = test kırılır, (b) src/app altındaki her page.tsx nav-manifest'te bir düğümle eşleşmeli, (c) her nav düğümünün gate'i Capability union'ında olmalı.
8. 8) Şerit şerit geçiş (strangler): mevcut src/shared/ui/* componentleri SİLİNMEZ, yeni shadcn karşılığı geldikçe o şeridin sayfaları taşınır. Son adımda kullanılmayanlar temizlenir. BIG-BANG REFACTOR YASAK.

### Bağlayıcı kurallar

- **Tooltip bilgi taşımaz.** Dokunmatik cihazda tooltip **yoktur**; panelin birincil cihazı resepsiyon tableti. Bilgi taşıyan her tooltip → **görünür etiket**. `src/shared/ui/tooltip.tsx` **silinir**.
- **Renk tek sinyal olmaz.** Statü rozetlerinde metin her zaman yazılır (erişilebilirlik).
- **Mobilde `Dialog` değil `Sheet`.** Tablet portre + telefonda birincil modal Sheet'tir.
- **Minimum dokunma hedefi 44px** — takvim blokları ve slot butonları dahil.
- **Big-bang refactor YASAK.** Şerit şerit (strangler); eski `src/shared/ui/*` yeni karşılığı gelene kadar silinmez.
- **Light-first + çift tema.** `animated-background.tsx` ve dark glassmorphism silinir. Mevcut `--rs-*` token'ları shadcn token'larına eşlenir.

---

## 9. Uygulama sırası (strangler)

| Adım | İş | Tahmin | Çıktı |
|---|---|---|---|
| **0** | FAZ 0 HİJYEN: 12 maddenin tamamı (typecheck yeşil, 3 canlı 404 kapalı, 'src/app/(' silindi, ölü rotalar ve MVP dışı şeritler söküldü). Hiçbir tasarım işi yapılmaz. | 1 gün | `pnpm typecheck` ve `pnpm test` yeşil; menüdeki hiçbir link 404 vermiyor; repo MVP kapsamına daraldı. |
| **1** | SHADCN TEMELİ + NAV-MANIFEST + GATE KATMANI. shadcn init + component'ler; capabilities.ts, can.ts, manifest.ts, GateBoundary, TenantBranchSwitcher, BusinessClient (branchId enjeksiyonu). panel-shell'in elle yazılmış sidebar'ı shadcn Sidebar'a taşınır. Manifest-vs-app testi devreye girer. HİÇBİR SAYFA İÇERİĞİ DEĞİŞMEZ — sadece kabuk ve yetki altyapısı. | 3 gün | BranchManager artık 'İŞLETMEM' grubunu GÖRMÜYOR (Blokaj-1 kapandı). Şube-scoped müdür listelerde 403 almıyor (Blokaj-8 kapandı). Manifest dışı sayfa build'i kırıyor. |
| **2** | ŞERİT A — TALEP → RANDEVU (MVP'nin kalbi). /panel/talepler (yeni, RequestInboxCard) + /panel/talepler/[id] + /panel/randevular (yeni) + /panel/randevular/[id] (6 operasyon: cancel/complete/no-show/notes/rebook). Inbox /panel'den SÖKÜLÜR, dashboard sadeleşir. Boş slota tıklayıp randevu oluşturma YOK (endpoint yok). | 5 gün | Salon sahibi tabletten talebi onaylıyor, randevu doğuyor, tamamlandı/gelmedi işaretleyebiliyor. Kullanıcının MVP cümlesinin 'rezervasyonlarını yönetebilmesi' kısmı BİTTİ. |
| **3** | ŞERİT B — MÜŞTERİ. /hesabim → /hesabim/randevular redirect; /hesabim/randevular (yeni, sekmeli, drawer detay); /hesabim/talepler (refactor, bekleyen talep iptali + TTL geri sayım); /hesabim/profil (sadeleştir). Status filtresine GÜVENİLMEZ, client-side ayrılır (backend BUG'ı). Confirmed randevuda iptal butonu GÖSTERİLMEZ — Blokaj-2 backend'i bitene kadar. | 3 gün | Müşteri geçmişini fiyatlarıyla görüyor, bekleyen talebini iptal edebiliyor. MVP cümlesinin 'müşteri de rezervasyonlarını yönetebilmesi' kısmı KISMEN bitti (tam bitmesi Blokaj-2'ye bağlı). |
| **4** | ŞERİT C — FİYAT. /panel/hizmetler (refactor) + /panel/hizmetler/[serviceId] (yeni, VariantPriceRow). 5-alanlı PATCH tuzağı componentin içine kapsüllenir. CurrencyCode gizli, sabit TRY. 'Sil' yerine 'Arşivle'. Yetkinlik ataması YOK. | 3 gün | MVP cümlesinin 'fiyatlarını yönetebilmesi' kısmı BİTTİ. |
| **5** | ŞERİT D — EKİP. /panel/personel (refactor, şube seçici zorunlu) + /panel/personel/[staffId] (yeni, izin/müsaitsizlik sekmesi). DÜZENLEME FORMU, backend Rename fix'i (Blokaj-3) merge edilmeden AÇILMAZ. 'Çalışma saatleri' sekmesi ve 'Yetkinlikler' sekmesi AÇILMAZ (backend yok / write-only). | 3 gün | MVP cümlesinin 'elemanlarını yönetebilmesi' kısmı BİTTİ (backend Rename fix'i şartıyla). |
| **6** | ŞERİT E — KURULUM ŞERİDİ. /panel/saatler (WeeklyHoursGrid) + /panel/koltuklar (iki sekme birleşimi) + /panel/subeler (IANA Select — Blokaj-5 FE tarafı) + /panel/ayarlar (typecheck fix'i zaten Faz 0'da, burada shadcn form'a taşınır) + /panel/takvim (StaffDayCalendar). | 5 gün | Yeni bir salon panele girip sıfırdan kurulum yapabiliyor: şube → saatler → koltuklar → ekip → hizmet/fiyat. Onboarding döngüsü tam. |
| **7** | ŞERİT F — PUBLIC. /isletme/[slug] (SlotPicker, mobil full-screen Sheet akışı) + /kesfet + /. Light-first tema uygulanır, animated-background ve galeri sökülür. | 4 gün | Müşteri telefondan slot seçip talep gönderiyor. MVP cümlesinin 'müşterinin randevu alabilmesi' kısmı BİTTİ. Uçtan uca döngü kapandı. |
| **8** | TEMİZLİK + BLOKAJ ENTEGRASYONU. Kullanılmayan eski src/shared/ui componentleri silinir. Backend'den gelen Blokaj-2 (müşteri randevu iptali) ucu /hesabim/randevular'a bağlanır — butonu aç. Blokaj-4 (AllowedOrigins) deploy dokümanına yazılır ve staging'de doğrulanır. | 2 gün | Lansmana hazır. Ölü kod sıfır, tüm blokajlar kapalı. |

**Toplam frontend tahmini: ~29 iş günü.** Backend blokaj işleri hariç (müşteri iptal ucu ~2 gün, personel `Rename` fix ~0.5 gün).

---
## 10. Kararlar (KAPANDI — 2026-07-12)

Dört açık karar da kullanıcı tarafından verildi. Bunlar artık **bağlayıcıdır**.

### K1 — Şube müdürü (BranchManager) rolü: **V2'ye bırakıldı**

Lansmanda tek işletme rolü var: **BusinessOwner**. Gerekçe: kod zaten şunu söylüyordu — şube müdürü
`/api/business` altındaki hizmet/fiyat/personel/şube/koltuk/saat/ayar uçlarının hiçbirine dokunamıyor
(11 composer da `BusinessOwner` şartı arıyor). Rolü MVP'ye almak, ~2 günlük capability filtreleme ve
şube-scope enjeksiyonu işini bugün yapmak demekti; karşılığında kullanıcıya sıfır değer.

**⚠️ ZORUNLU SONUÇ — bu atlanırsa tuzak kurulur:**
`/platform/tenantlar/[tenantId]/uyeler` sayfasındaki rol seçicisi MVP'de **sadece `BusinessOwner`** sunacak.
`BranchManager` ve `Staff` seçenekleri **listeden çıkarılacak**. Aksi halde bu rollerden biri atanan kullanıcı
panele girer ve her öğede 403 duvarına çarpar (`Staff`'ın capability listesi backend'de zaten **boş dizi**).

**Yine de kurulacak olan:** `nav-manifest` tipli kalır, `permission` alanı **zorunlu union** olur ve `can()`
**fail-closed** çalışır. Tek capability seti ile bile bu altyapı kurulur — çünkü asıl amacı, V2'de rol
eklerken "izin yazmayı unuttum → herkese açıldı" hatasını **derleme zamanında** yakalamaktır. Bu, referans
projelerdeki gerçek fail-open zaafının (`buildFallbackNavigation()`) yapısal karşılığıdır. Maliyeti düşük,
sonradan eklemek pahalı.

### K2 — Müşterinin onaylanmış randevuyu iptali: **backend açılacak, MVP'ye giriyor**

Yeni uç: `POST /api/public/businesses/{slug}/appointments/{appointmentId}/cancel`
- Mevcut `CancelAppointmentRequestService` deseninin **birebir kopyası** (idempotency + audit dahil).
- Sahiplik: `Appointment.CustomerUserAccountId == sub` claim. Sadece `Status == Confirmed` iptal edilebilir.
- Domain metodu `Appointment.Cancel(...)` **zaten var** — sıfırdan domain yazılmayacak.
- Tahmin: ~2 gün.

**Frontend sözleşmesi:** Adım 3'te (`/hesabim/randevular`) `Confirmed` satırlarda iptal butonu
**gösterilmez**; uç merge edilince Adım 8'de açılır. (Sahte ekran üretme yasağı.)

### K3 — Salon onboarding: **elle (concierge)**

Self-servis işletme kaydı **yapılmayacak**. İlk salonları platform admin (kurucu) elle kuruyor.

**Sonuç:** `/platform/tenantlar` ve `/platform/tenantlar/[tenantId]/uyeler` sayfaları MVP'nin **zorunlu**
parçasıdır (2 sayfa, mevcut ve çalışıyor — dokunulmuyor). Backend'de yeni bir tenant-provisioning akışı
**gerekmiyor**, kapsam büyümüyor.

### K4 — İptal politikası: **ayarlanabilir alan eklenecek**

`BusinessSettings`'e `CancellationCutoffHours` (int) alanı eklenir.
- Kural: `now + CutoffHours > StartUtc` ise `APPOINTMENT_CANCEL_TOO_LATE`.
- Varsayılan: `2` (0 = her zaman iptal edilebilir).
- `/panel/ayarlar`'da tek bir sayı alanı olarak açılır.
- K2'deki yeni iptal ucunda **ve** mevcut talep-iptal ucunda uygulanır.
- Tahmin: ~1 gün (migration + alan + kontrol + UI).

Gerekçe: sabit kural her salona uymaz (kimi 24 saat ister, kimi hiç istemez) ve migration borcunu
sonra ödemek daha pahalı.

---

## 11. Backend iş listesi (bu doküman kaynaklı)

Frontend bu uçlar olmadan ilerleyemez veya kullanıcıya yalan söyler. Öncelik sırasıyla:

| # | İş | Tahmin | Neden |
|---|---|---|---|
| B1 | `StaffMember.Rename()` domain metodu + `StaffManagementService.UpdateAsync` içinde çağrılması | 0.5 gün | **Bugün 200 OK dönüp hiçbir şey yapmıyor.** Personel adı düzeltilemiyor → MVP'nin "elemanlarını yönetebilmesi" maddesi delik. |
| B2 | `POST .../appointments/{id}/cancel` (müşteri iptali) — K2 | 2 gün | MVP'nin "müşteri de rezervasyonlarını yönetebilmesi" maddesi. |
| B3 | `BusinessSettings.CancellationCutoffHours` — K4 | 1 gün | İptal politikası. B2 ile birlikte yapılmalı. |
| B4 | `Branch.TimeZoneId` validasyonu + IANA normalizasyonu | 0.5 gün | "Istanbul" yazan salon **sonsuza dek 0 slot** döndürür (200 OK, hata yok) — sessizce lansman dışı kalır. |
| B5 | `ConfirmedAppointmentQueryService.GetOwnAsync`'e `status` parametresi | 0.5 gün | Müşteri geçmişinde status filtresi randevulara uygulanmıyor. |
| B6 | `ConfigureApplicationCookie`: idle 8 saat, `SecurePolicy = Always` | 0.2 gün | Bugün 14 gün sliding, absolute timeout yok, `Secure=SameAsRequest`. Ortak/tezgah bilgisayarı riski. |
| B7 | `Security:UnsafeRequestOrigins:AllowedOrigins` prod ortam değişkeni | — | **Deploy blokajı.** Boş kalırsa randevu oluşturma dahil her POST 403 döner. |
| B8 | (Ertelenebilir) Personel arşivlemede aktif randevu kontrolü → 409 | 0.5 gün | Gelecek randevusu olan personel arşivlenip randevular sahipsiz kalıyor. |

**Yapılmayacaklar (bilinçli):** anti-forgery token altyapısı (Origin doğrulama + SameSite=Lax zaten var),
`date-fns-tz` (native `Intl` doğru çalışıyor), walk-in randevu (domain invariant'ı kırıyor, ~38 dosya),
yetkinlik okuma uçları (yetkinlik UI'ı MVP dışı).
