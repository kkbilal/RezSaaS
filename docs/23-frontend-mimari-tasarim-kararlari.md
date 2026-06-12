# Frontend Mimari ve Tasarım Kararları

Son güncelleme: 2026-06-13

## Amaç

Bu belge RezSaaS frontend'inin repo sınırını, teknik mimarisini, tasarım üretim
sürecini, güvenlik kurallarını ve backend sözleşmesi ihtiyaçlarını tanımlar.

Frontend yalnızca mevcut endpoint'lere ekran ekleyen bir katman değildir. Public
keşif, müşteri rezervasyon akışı, işletme operasyonu ve platform control-plane
yüzeyleri farklı bilgi yoğunluğu ve güvenlik seviyelerine sahiptir; ancak aynı
ürün dili ve tasarım sistemi içinde kalmalıdır.

## 2026-06-09 Uygulama Durumu

- `src/Apps/RezSaaS.Web` altında Next.js 16, React 19.2, Tailwind 4 ve
  OpenAPI-driven `openapi-fetch` client ile ilk web uygulaması başlatıldı.
- İlk görsel yön; RezSaaS domain odaklı `rezsaas-merkez` referansı ile
  `viktor-oddy-studio` modern studio hissi birleştirilerek business panel
  prototipine uygulandı.
- `/panel` route'u `GET /api/session/bootstrap`, `GET /api/business/context`
  ve `GET /api/business/appointment-requests` üzerinden canlı tenant + inbox
  verisiyle çalışır; preview appointment data kaldırılmıştır.
- Business appointment/request OpenAPI response content tipleri tamamlandı ve
  generated TypeScript client artık inbox kontratını typed olarak taşır.
- 2026-06-13: `/` route'u geliştirici/prototip açılışı yerine ürün landing
  sayfasına çevrildi; özellikler, işleyiş, paket fiyat aralıkları ve tek
  `Giriş yap` kapısı gösterilir. `/giris` rol bazlı ayrışmayan tek giriş
  ekranıdır; görülecek panel kullanıcı yetkileriyle belirlenir.
- 2026-06-13: Public discovery/profile response content metadata OpenAPI'ye
  eklendi; generated TypeScript client artık `/api/public/businesses`,
  `/api/public/businesses/{slug}/profile` ve slot response tiplerini taşır.
  `/kesfet` ve `/isletme/{businessSlug}` route'ları gerçek public API
  kontratıyla SSR olarak uygulanmaya başladı.
- 2026-06-13: Public appointment request create/list/detail/cancel response
  metadata OpenAPI'ye eklendi. İşletme profilinde hizmet, şube, tarih ve
  opsiyonel personel tercihiyle gerçek slot arayan booking paneli başladı;
  seçilen draft yalnızca `sessionStorage` içinde kısa TTL ile, PII/token
  taşımadan saklanır ve auth gerekiyorsa tek `/giris` kapısına döner.
- 2026-06-13: `/hesabim/talepler` private customer route'u eklendi. Route,
  `GET /api/customer/appointment-history` üzerinden kendi talep/randevu geçmişini
  okur; yalnız `PendingApproval` talepler public cancel endpoint'i ve
  `Idempotency-Key` ile iptal edilebilir.
- 2026-06-13: `/panel` işletme operasyon yüzeyi `GET /api/business/appointments`
  ile kesinleşmiş randevu listesini okumaya başladı. Confirmed randevular için
  cancel, complete, no-show ve business note aksiyonları generated client,
  tenant header ve `Idempotency-Key` ile bağlandı; complete/no-show butonları
  backend zaman kurallarıyla uyumlu şekilde gelecekteki randevularda kapalıdır.
- 2026-06-13: `/panel` appointment schedule içine rebook ve resource block
  aksiyonları eklendi. Rebook mevcut staff/resource ile yeni UTC aralığına
  taşır ve backend conflict kontrolüne bırakılır; resource block kullanıcıya
  internal GUID göstermeden seçili iç kaynağı operasyonel olarak kapatır.

## Backend Faz Analizi ve Frontend Karşılığı

| Backend alanı | Mevcut durum | Frontend karşılığı | Eksik veya riskli nokta |
| --- | --- | --- | --- |
| Phase 1 Identity/Auth | Register, login, refresh, confirmation, reset ve manage endpoint'leri mevcut | Auth ekranları ve hesap güvenliği | Session/rol/tenant bootstrap kontratı ve gerçek MFA step-up oturumu tamamlanmalı |
| Phase 2 public keşif | İşletme arama, profil, hizmet, branch, staff, çalışma saati ve galeri mevcut | `/kesfet` ve `/isletme/{businessSlug}` SSR route'ları | Zengin facet/taksonomi ve görsel optimization allow-list sonraki iyileştirme |
| Phase 2 slot ve request create | Multi-service slot arama ve authenticated `PendingApproval` create mevcut | Rezervasyon sihirbazı | Frontend resource seçtirmez; optional staff tercihi backend kontratına bağlıdır |
| Phase 2 müşteri self-service | Business slug kapsamında request liste/detay/pending cancel ve global customer history mevcut | `/hesabim/talepler` müşteri geçmişi ve pending cancel | Cursor pagination ileride iyileştirilecek |
| Phase 2 işletme onayı | Pending/liste/detay, approve/decline, abuse report ve label enrichment mevcut | İşletme talep kutusu | Liste pagination/search contract'ı ileride iyileştirilecek |
| Phase 3 platform control-plane | Tenant, membership, lifecycle, abuse, appeal, closure ve step-up session API'leri mevcut | Platform operasyon paneli | UI açılışı F5 içinde güvenli route ve step-up UX ile yapılmalı |
| Phase 3 operasyon derinliği | Appointment calendar/detail, note, cancel, complete, no-show, rebook ve resource block API'leri mevcut | `/panel` içinde appointment schedule, rebook, resource block ve temel operasyon aksiyonları | Organization/Catalog/Resources/Availability ayar ekranları ve daha zengin calendar UX sonraki dilimlerdir |
| Reviews, Analytics, Payments | Backend fazları bekleniyor | Yorum, rapor ve ödeme ekranları | API olmadan sahte dashboard veya form üretilmez |

## Repo ve Deploy Kararı

- Frontend için ayrı Git reposu açılmaz. Backend ve frontend aynı ürün
  sözleşmesini paylaştığı için API + UI değişiklikleri aynı PR içinde atomik
  gözden geçirilebilmelidir.
- İlk frontend uygulaması `src/Apps/RezSaaS.Web` altında yaşar.
- Backend modüler monolith olarak kalır. Web host ayrı bir runtime artifact
  olabilir; bu durum backend domain'lerini mikroservislere bölmez.
- Production'da web ve API aynı origin altında yayınlanır:
  - `/api/*`: ASP.NET Core API
  - diğer route'lar: RezSaaS Web
- İlk aşamada tek Next.js uygulaması kullanılır. Public, customer, business ve
  platform admin yüzeyleri route/layout sınırlarıyla ayrılır.
- Ayrı frontend uygulamasına ancak bağımsız deploy zorunluluğu, ayrı ekip
  sahipliği veya ölçülmüş bundle/operasyon problemi oluşursa geçilir.

Önerilen başlangıç yapısı:

```text
package.json
pnpm-lock.yaml
pnpm-workspace.yaml
src/
  Apps/
    RezSaaS.Api/
    RezSaaS.Web/
      app/
        (public)/
        (auth)/
        hesabim/
        panel/
        platform/
      components/
        ui/
      features/
      lib/
        api/
      styles/
      tests/
tests/
  RezSaaS.Web.E2E/
```

Node projeleri `.sln` içine zorla eklenmez. Repo kökü script'leri .NET ve web
doğrulamalarını birlikte çalıştırabilir; iki toolchain kendi bağımlılık
sınırlarını korur.

## Route ve Ürün Yüzeyleri

| Yüzey | Başlangıç route'ları | Render/erişim yaklaşımı |
| --- | --- | --- |
| Public | `/`, `/kesfet`, `/isletme/{businessSlug}` | Indexlenebilir SSR/RSC, paylaşılabilir URL |
| Auth | `/giris`, `/kayit`, `/sifremi-unuttum`, `/sifre-sifirla`, `/eposta-dogrula` | Public ama `noindex`; `/giris` tek login kapısıdır, auth dönüş route'u güvenli allow-list ile korunur |
| Customer | `/hesabim/talepler`, `/hesabim/guvenlik`, `/hesabim/itirazlar` | Authenticated, private ve `no-store` |
| Business | `/panel/talepler`, sonraki fazlarda `/panel/takvim`, `/panel/ayarlar` | Tenant membership ve branch scope kontrollü |
| Platform | `/platform/tenantlar`, `/platform/abuse`, `/platform/itirazlar` | Yalnızca `PlatformAdminWithStepUp` |

İlk `PlatformAdmin` bootstrap için normal ürün UI'ı yapılmaz. Bootstrap,
runbook/operasyon yüzeyi olarak kalır ve public web navigasyonundan erişilemez.

## Teknoloji Kararları

| Alan | Karar |
| --- | --- |
| Runtime ve package manager | Node.js `24 LTS` ve `pnpm 11`; sürümler repo içinde sabitlenir |
| Framework | Next.js `16.x` App Router, React `19.2`, strict TypeScript |
| Rendering | Public sayfalarda Server Components/SSR; etkileşim gereken en küçük sınırda Client Components |
| Stil | Tailwind CSS `4.x` + semantic CSS design token'ları |
| Erişilebilir primitive | Radix Primitives seçici olarak kullanılır; görünüm tamamen RezSaaS'e aittir |
| API client | Versioned OpenAPI artifact + `openapi-typescript` + `openapi-fetch` |
| Client server-state | Yalnızca etkileşimli/private yüzeylerde TanStack Query |
| Form | React Hook Form + Zod; backend validation nihai otoritedir |
| Component laboratuvarı | Storybook |
| Mock | MSW; gerçek API kontratından kopuk elle yazılmış response şekilleri kullanılmaz |
| Unit/component test | Vitest + React Testing Library |
| E2E | Playwright |
| A11y | Storybook a11y + `@axe-core/playwright` + manuel klavye/screen reader kontrolü |

Kurallar:

- Axios eklenmez; standart `fetch` ve type-safe OpenAPI client yeterlidir.
- Global state kütüphanesi başlangıçta eklenmez. URL state, server state ve
  feature-local state tercih edilir.
- Booking draft gerekiyorsa version ve TTL taşıyan `sessionStorage` kaydı
  kullanılabilir; auth token, PII veya internal admin veri burada tutulmaz.
- `shadcn/ui` bir tasarım sistemi veya görsel yön değildir. Yalnızca ihtiyaç
  halinde incelenmiş component kaynak kodu başlangıç noktası olarak alınabilir;
  varsayılan görünümü ürün içine taşınmaz.
- Her dependency güncel desteklenen patch sürümünde tutulur; lockfile zorunludur.

## API Sözleşmesi ve Veri Akışı

- Backend OpenAPI dokümanı frontend'in type source-of-truth'udur.
- OpenAPI artifact ve üretilen TypeScript tipleri versiyonlanır; elle
  değiştirilmez.
- CI, API sözleşmesi değiştiğinde client generation çıktısının güncel olduğunu
  doğrular.
- Public Server Components API'yi server-side çağırabilir. Private ekranlar ve
  browser mutation'ları same-origin `/api` üzerinden cookie ile çalışır.
- Public cache/revalidation kararı endpoint bazlı verilir. Authenticated,
  tenant-scoped, PII veya control-plane response'ları `no-store` kabul edilir.
- `Idempotency-Key` gereken create/approve/decline/cancel komutlarında frontend
  aynı kullanıcı niyetinin retry'larında aynı key'i, yeni niyette yeni key'i
  kullanır.
- Kullanıcıya raw backend error, stack trace veya internal reason gösterilmez.
  Stabil `ErrorCode` değerleri merkezi ve Türkçe bir hata sözlüğüne çevrilir.
- `401`, `403`, tenant-dışı `404`, `409`, `422` ve `429` birbirinden farklı UX
  durumlarıdır; tek bir genel hata toast'ına indirgenmez.

## Auth, Tenant ve Güvenlik

- Browser auth yalnızca secure/httpOnly cookie ile çalışır. Bearer/access token
  `localStorage`, `sessionStorage`, URL veya client log'una yazılmaz.
- Frontend route guard yalnızca yönlendirme ve UX sağlar; backend authz nihai
  otoritedir.
- Authenticated route'larda backend ulaşılamazsa private veri render edilmez;
  route, kullanıcıya oturum kapısının geçici kullanılamadığını söyleyen güvenli
  hata durumuna düşer.
- Business API çağrılarında `X-RezSaaS-Tenant`, yalnızca backend'in authenticated
  kullanıcı için döndürdüğü aktif tenant seçeneklerinden merkezi API client
  tarafından eklenir. Kullanıcının serbest metinle tenant GUID girmesi engellenir.
- Public ve customer çağrıları tenant header taşımaz.
- Platform admin çağrıları tenant header taşımaz; global control-plane olarak
  kalır.
- Kritik aksiyonlarda generic confirm yerine aksiyona özel açıklama, sonuç,
  zorunlu reason alanı ve gerektiğinde yeniden step-up kullanılır.
- PII browser console, analytics event, error monitoring breadcrumb veya
  screenshot fixture'ına yazılmaz.

## Zaman, Dil ve Durum Gösterimi

- UI dili MVP'de Türkçedir; ürün metinleri feature dosyalarına dağılmaz, merkezi
  mesaj sözlüğünde tutulur. İlk aşamada locale URL prefix'i eklenmez.
- Booking zamanı browser timezone'una sessizce çevrilmez. Şube timezone'u
  kullanıcıya görünür biçimde esas alınır.
- Public slot ve booking ekranları UTC değer ile branch local gösterimini birlikte
  korur.
- `AppointmentRequest` ile `Appointment` UI'da aynı kavram gibi gösterilmez.
- `PendingApproval`, "randevu kesinleşti" olarak yazılmaz. Talebin işletme onayı
  beklediği ve slotu bloklamadığı açıkça anlatılır.
- Status label, renk ve aksiyon matrisi tek bir domain UI sözlüğünden gelir.

## Tasarım Yönü

RezSaaS tek bir hazır dashboard temasına dönüştürülmez. Aynı tasarım sistemi
içinde iki yoğunluk modu kullanılır:

- Public/customer yüzeyi: sakin, güven veren, içerik ve gerçek işletme verisi
  odaklı; mobil kullanım öncelikli.
- Business/platform yüzeyi: taranabilir, daha yoğun, düşük görsel gürültülü;
  karar ve operasyon hızına odaklı.

Kaçınılacak kalıplar:

- Anlamsız gradient/blob, aşırı blur/glass ve her şeyi kart içine alma
- Sahte metrik, sahte testimonial veya backend'i olmayan dashboard
- Ürün bağlamı taşımayan AI görselleri ve generic salon stock görüntüsü
- Her öğede pill radius, gereksiz hover hareketi ve dekoratif animasyon
- Bir component library'nin varsayılan font, renk, radius ve spacing değerlerini
  aynen yayınlama

Zorunlu tasarım davranışları:

- Semantic token: renk, typography, spacing, radius, shadow, motion ve density
- Light theme lansman kapısıdır; dark mode ilk lansman kapısı değildir
- Motion yalnızca hiyerarşi, geçiş veya durum değişimini anlatır; reduced-motion
  tercihine uyar
- Türkçe karakter desteği, gerçek içerik uzunlukları ve dar ekran taşmaları
  tasarım aşamasında doğrulanır
- Public mobil-first; business hızlı approve/decline mobilde kullanılabilir,
  yoğun calendar/config ekranları desktop/tablet öncelikli olabilir

## Tasarım Araçları ve MCP Kullanımı

- Figma; kullanıcı akışı, wireframe, high-fidelity ekran, component ve token
  kaynağıdır.
- Figma MCP kullanılırsa remote MCP tercih edilir ve yalnızca seçilmiş frame,
  component veya variable bağlamı okunur.
- Figma-to-code çıktısı production kodu olarak körlemesine kabul edilmez. Domain
  davranışı, responsive yapı, erişilebilirlik ve component sınırı code review'dan
  geçer.
- AI üretimli veya indirilen UI prototipleri referans olarak kullanılabilir;
  ancak bozuk encoding, sahte veri, emoji/dekorasyon fazlalığı, stok görsel ve
  domain-dışı etkileşimler production koda doğrudan taşınmaz.
- Storybook, implement edilmiş tasarım sisteminin yaşayan kataloğudur.
- Browser MCP, önemli frontend değişikliklerinden sonra local uygulamayı gerçek
  viewport'larda incelemek için kullanılır; otomatik testlerin yerine geçmez.
- Image generation yalnızca onaylı art direction altında yardımcı görsel üretimi
  için kullanılabilir. UI, ikon sistemi veya gerçek işletme içeriği yerine
  kullanılmaz.

## Backend Hazırlık Kapıları

Frontend başlamadan veya ilgili faza gelmeden aşağıdaki kontratlar tamamlanmalıdır:

2026-06-07 durumu: P0 backend kontratları uygulanmıştır. OpenAPI artifact,
`GET /api/session/bootstrap`, `POST /api/session/step-up`,
`GET /api/business/context`, global customer history, business request label
read model'i ve optional staff/internal resource create kontratı mevcuttur.

| Öncelik | İhtiyaç | Gerekçe |
| --- | --- | --- |
| P0 | Versioned ve CI'da doğrulanan OpenAPI artifact | Type-safe client ve contract drift kontrolü |
| P0 | Authenticated session/bootstrap endpoint'i | Hesap durumu, platform rolleri, MFA/step-up durumu ve güvenli yönlendirme |
| P0 | Business context endpoint'i | Kullanıcının aktif tenant/membership/branch scope seçeneklerini güvenle seçmesi |
| P0 | Gerçek MFA enrollment + step-up session akışı | Platform control-plane UI'ının production'da kullanılabilmesi |
| P0 | Same-origin web/API local ve production routing kararı | Cookie auth ve origin guard'ın güvenli çalışması |
| P0 | Optional staff + internal resource create kontratı kararı | Frontend'in kullanıcı adına rastgele staff/resource GUID seçmemesi |
| P1 | Zengin discovery summary ve facet/taksonomi endpoint'i | Güzel, filtrelenebilir ve N+1 üretmeyen keşif kartları |
| P1 | Global customer booking/appointment read model'i | Müşterinin slug bilmeden tüm taleplerini ve confirmed randevularını görmesi |
| P1 | Business request read model'inde branch/staff/resource adları ve timezone | Operasyon panelinde GUID gösterilmemesi |
| P1 | Tutarlı error envelope ve validation sözleşmesi | Güvenilir form ve hata UX'i |
| P1 | Cursor pagination/search sözleşmesi | Business/admin listelerinin `take` ile sınırlı kalmaması |
| P2 | Appointment calendar ve command API'leri | Tamamlandı; takvim, cancel, complete, no-show ve rebook frontend F6'da kullanılabilir |
| P2 | Organization/Catalog/Resources/Availability yönetim API'leri | Gerçek işletme yönetim ekranları |
| P2 | Reviews API | Verified review deneyimi |

Optional staff ve internal resource kararı booking çekirdeğini değiştirmeden
çözülmelidir: persisted request yine tam olarak `1 staff + 1 resource` taşır;
ancak "fark etmez" seçen müşteriye teknik kapasite kimliği seçtirilmez. Bu
kontrat uygulanmadan önce ayrı ADR ve concurrency testi gerekir.

## Kalite Kapıları

- Hedef: WCAG 2.2 AA.
- Public sayfalarda 75. percentile hedefleri: `LCP <= 2.5s`, `INP <= 200ms`,
  `CLS <= 0.1`.
- Public profile, auth, booking create, customer cancel, business approve/decline
  ve kritik admin kararları Playwright ile uçtan uca doğrulanır.
- Storybook component'leri loading, empty, error, long-content, keyboard ve
  reduced-motion durumlarını kapsar.
- Görsel kalite yalnızca screenshot benzerliği değildir; gerçek içerik,
  responsive davranış, erişilebilirlik ve domain doğruluğu birlikte incelenir.
