# Frontend Uygulama Planı

Son güncelleme: 2026-06-20

Bu plan frontend geliştirmesini küçük, doğrulanabilir dikey dilimlere böler.
Frontend fazları mevcut ürün/backend `Phase 0-5` numaralarıyla karışmaması için
`F0-F7` olarak adlandırılır.

Her dilim yalnızca ekran çıktısıyla değil; API readiness, erişilebilirlik,
responsive davranış, test, doküman ve güvenlik kontrolüyle kapanır.

> **Güncelleme (ADR-068, 2026-06-20):** Backend yol haritası refactor edildi
> (bkz. `roadmap/README.md`). Eski tek-parça Phase 4/5 yerine bağımsız alt fazlar
> (4a/4b/4c ve 5a/5b/5c/5d/5e) kullanılır. F6; F6.1 (appointment ops),
> F6.2 (settings CRUD — **Phase 5a'ya bağımlı**), F6.3 (verified review) alt
> dilimlerine ayrıldı. MVP lansman eşiği artık `roadmap/mvp-lansman-kapisi.md`
> içinde açık bir kapıdır; F7 bu kapının frontend kontrol listesidir.

## Backend ile Eşleşme

| Frontend fazı | Ana backend dayanağı | Durum |
| --- | --- | --- |
| F0-F1 | Phase 1 Identity/Auth ve mimari temel | ✓ |
| F2 | Phase 2 public discovery/profile API | ✓ (facet/perf açık) |
| F3 | Phase 2 booking + Phase 3 customer abuse | ✓ (Playwright açık) |
| F4 | Phase 2 business approve/decline + Phase 3 abuse report | ✓ (pagination açık) |
| F5 | Phase 3 tenant lifecycle + abuse/appeal/closure read | ✓ (mutation'lar kapalı) |
| F6.1 | Phase 3 appointment calendar/ops (ADR-062) | ✓ |
| F6.2 | **Phase 5a** settings CRUD (branch/staff/service/variant/working hours/resource) | ⏳ 5a bekler |
| F6.3 | Phase 2 verified review | ✓ |
| F7 | MVP Lansman Kapısı sertleşmesi | ⏳ |
| Ödeme UI | Phase 4a/4b/4c | ⏳ |
| Analytics UI | Phase 5b | ⏳ |
| Entegrasyon UI | Phase 5c | ⏳ |
| SMS/WhatsApp UI | Phase 5d | ⏳ |
| Marketplace/i18n UI | Phase 5e | ⏳ |

## F0 - Ürün UX Keşfi, API Hazırlığı ve Repo İskeleti

Durum: büyük ölçüde tamamlandı; tasarım laboratuvarı ve a11y sertleşmesi F1/F7
altında açık iş olarak izleniyor.

Amaç: kod üretmeden önce kullanıcı akışını, görsel yönü, frontend sınırını ve
backend sözleşmesi kapılarını doğrulamak.

Teslimatlar:

- Customer, `BusinessOwner`, `BranchManager` ve `PlatformAdmin` için görev/akış
  haritası
- Public keşif -> profil -> multi-service -> slot -> auth -> `PendingApproval`
  uçtan uca low-fidelity prototipi
- İşletme request inbox ve platform control-plane low-fidelity prototipi
- Figma dosya yapısı, semantic token başlangıcı ve iki görsel yön denemesi
- Gerçek Türkçe içerikle responsive wireframe doğrulaması
- `src/Apps/RezSaaS.Web` Next.js iskeleti
- Node 24 LTS, pnpm 11, lockfile, strict TypeScript, lint ve format kapıları
- Same-origin local proxy ve environment sözleşmesi
- OpenAPI artifact/client generation başlangıcı
- Session/bootstrap, business context ve MFA step-up backend kontratlarının
  uygulanması veya ilgili frontend fazını bloke eden açık iş olarak bağlanması

2026-06-09 notu:

- `src/Apps/RezSaaS.Web` Next.js/Tailwind/OpenAPI client iskeleti oluştu.
- `/` route'u ürün landing sayfasına, `/giris` tek login kapısına ve `/panel`
  authenticated işletme yüzeyine dönüştü.
- Global `pnpm` bu geliştirme makinesinde PATH'te olmadığı için yerel
  doğrulamalar şimdilik `node_modules/.bin/*` komutları üzerinden koşuluyor.

Kapanış kriterleri:

- Route haritası ve kullanıcı akışları onaylıdır.
- Public ve operasyon yüzeylerinin görsel yönü birbirinden farklı yoğunlukta
  fakat aynı token sistemi içindedir.
- API tipleri elle yazılmadan üretilebilmektedir.
- Cookie auth, origin guard ve local web/API akışı test ortamında çalışmaktadır.
- Frontend'in kullanamayacağı mevcut backend boşlukları issue/dilim sahibiyle
  eşleştirilmiştir.

## F1 - Tasarım Sistemi ve Uygulama Kabukları

Durum: kısmen tamamlandı; uygulama kabukları, route guard, OpenAPI client ve
temel primitive'ler hazır, Storybook/a11y tooling kapanışı açık.

Amaç: sonraki ekranların tutarlı ve hızlı üretilebileceği, ancak hazır tema gibi
görünmeyen temel UI sistemini kurmak.

Teslimatlar:

- Public, auth, customer, business ve platform layout/shell'leri
- Semantic color, typography, spacing, radius, shadow, motion ve density token'ları
- Button, link, input, textarea, select, checkbox, radio, dialog, alert dialog,
  sheet, tabs, table, status badge, empty state, error state, skeleton ve toast
- Booking'e özel service selector, date strip, slot group ve request status
  component başlangıçları
- Operasyona özel filter bar, detail drawer ve high-risk action pattern'i
- Storybook kurulumu ve component state matrisi
- Keyboard, focus, reduced-motion ve a11y test başlangıcı
- Merkezi error-code ve domain status Türkçe sözlüğü

F1.1 uygulama sırası:

- Auth route'ları: `/giris`, `/kayit`, `/sifremi-unuttum`, `/sifre-sifirla`
- Cookie login/register/password reset formları
- `GET /api/session/bootstrap` tabanlı session guard
- `/panel` route'unun authenticated kapıya alınması
- Backend ulaşılamazken private veri render etmeyen güvenli unavailable state

2026-06-14 notu:

- Shared `DialogOverlay`, `DialogPanel` ve `DialogFormPanel` primitive'leri
  eklendi; business request ve appointment operasyon modalları tek ARIA/Escape
  sözleşmesine taşındı.
- `pnpm test` içine design-system contract testi eklendi. Storybook kurulumu
  tamamlanana kadar semantic token, reduced-motion, button varyant/focus ve
  dialog semantiklerinin kırılmasını yakalayan ara kalite kapısıdır.
- Storybook build, Storybook a11y ve `@axe-core/playwright` kontrolleri hâlâ
  F1/F7 kapanışı için ayrı tooling işi olarak durur.

Kapanış kriterleri:

- Component'ler mobile/desktop, loading/empty/error ve uzun Türkçe içerik
  durumlarında doğrulanmıştır.
- Semantic token, button ve dialog primitive contract testleri `pnpm test`
  içinde geçer.
- Storybook a11y kritik ihlallerde CI'ı düşürür.
- Ürün ekranları library varsayılan görünümüyle yayınlanmaz.
- Tasarım token'ı dışındaki keyfi renk/radius kullanımı review kuralıdır.

## F2 - Public Keşif ve İşletme Profili

Durum: büyük ölçüde tamamlandı; backend facet/taksonomi endpoint'i, production
görsel allow-list ve Playwright/perf smoke kapanışı açık.

Amaç: anonim kullanıcının güven veren, hızlı ve paylaşılabilir bir işletme
keşif deneyimi yaşaması.

Teslimatlar:

- Ana sayfa ve kategori/konum girişleri
- `/kesfet` arama, URL tabanlı filtre ve sonuç state'leri
- `/isletme/{businessSlug}` profil sayfası
- Galeri, açıklama, kurallar, hizmet/varyant menüsü, fiyat/süre, branch, çalışma
  saatleri, staff policy ve rating özeti
- Dynamic metadata, canonical, Open Graph, robots ve sitemap yaklaşımı
- Business/profile `404`, suspended/closed ve empty state'leri
- Public RSC/SSR cache ve revalidation politikası

Backend bağımlılıkları:

- Zengin discovery summary ve facet/taksonomi endpoint'i
- Galeri görsel domain/optimization allow-list kararı

2026-06-13 notu:

- Ana sayfa; ürün değeri, onaylı rezervasyon akışı, işletme paneli vaadi ve
  paket fiyat aralıklarını gösteren canlı ürün landing sayfasına çevrildi.
- Landing navigasyonu yalnızca tek `Giriş yap` kapısını öne çıkarır; ayrı rol
  login ekranı yoktur.
- Public OpenAPI response content metadata tamamlandı ve generated frontend
  tipleri yenilendi.
- `/kesfet` route'u query parametreli public arama formu ve gerçek discovery
  response kartlarıyla çalışır.
- `/isletme/{businessSlug}` route'u profil metadata, galeri, hizmet varyantları,
  şubeler, çalışma saatleri ve staff display policy bilgilerini gerçek public
  profile response üzerinden SSR gösterir.

2026-06-14 notu:

- `/kesfet` query parametreleri ortak normalize helper'ına taşındı; boş/uzun
  public filtreler temizlenir, canonical URL ve metadata aktif filtrelere göre
  üretilir.
- Discovery sonuç ekranı aktif filtre çipleri, filtre temizleme linki ve mevcut
  result-set içinden türetilmiş kategori/şehir/ilçe hızlı filtreleri gösterir.
  Backend facet endpoint'i olmadan sahte facet veya profil başına ek çağrı
  üretilmez.
- Public profil galerisinde relative/`https://` URL dışı görseller render
  edilmez; görseller boyut, lazy/eager loading ve async decode attribute'larıyla
  temel CLS/perf davranışına hazırlanır.

Kapanış kriterleri:

- İşletme profili JS kapalı veya yavaş bağlantıda temel içeriği gösterebilir.
- Public URL paylaşılabilir ve indexlenebilir metadata taşır.
- Search kartları ve hızlı filtreler tek discovery response'u üzerinden çalışır.
- Public kritik route'lar Core Web Vitals bütçesi ve Playwright smoke testini
  geçer.
- Search kartları N+1 profile çağrısı üretmez.

## F3 - Auth, Rezervasyon ve Müşteri Self-service

Durum: büyük ölçüde tamamlandı; Playwright booking journey, gerçek API recovery
smoke ve error-envelope detayları açık.

Amaç: kullanıcıyı slot seçiminden işletme onayı bekleyen gerçek talebe kadar
kesintisiz taşımak.

Teslimatlar:

- Register, login, confirmation, resend, forgot/reset password ve hesap bilgisi
- Auth sonrası güvenli return-to davranışı ve booking draft devamı
- Multi-service seçim ve toplam süre/fiyat özeti
- Branch, opsiyonel staff tercihi, tarih ve slot seçimi
- Final review ve idempotent appointment request create
- `PendingApproval` başarı ekranı, expiry açıklaması ve "kesinleşmedi" mesajı
- Müşteri talep listesi, detay ve pending cancel
- Customer abuse overview ve uygun hedefe appeal create
- `401`, `409`, `422`, `429`, expired ve slot-changed recovery akışları

Backend bağımlılıkları:

- Optional staff/internal resource create kontratı
- Global customer request + confirmed appointment read model'i
- Tutarlı session/bootstrap ve error envelope

2026-06-13 notu:

- Public appointment request create/list/detail/cancel response metadata
  OpenAPI'ye eklendi ve generated frontend tipleri yenilendi.
- `/isletme/{businessSlug}` profilinde booking başlangıç paneli eklendi:
  kullanıcı multi-service varyant, şube, tarih ve opsiyonel personel tercihiyle
  gerçek `/slots` endpoint'inden uygun saatleri arar.
- Seçilen slot, auth gerekirse yalnızca PII içermeyen ve kısa TTL'li
  `sessionStorage` draft olarak saklanır; kullanıcı tek `/giris` ekranından
  profile geri döner.
- `Talep gönder` authenticated durumda public appointment request create
  endpoint'ine `Idempotency-Key` ile gider ve başarı mesajı `PendingApproval`
  kesin randevu gibi göstermeden verilir.
- `/hesabim/talepler` authenticated customer route'u eklendi; global appointment
  history read model'iyle talep/randevu geçmişini gösterir ve yalnız
  `PendingApproval` talepler için idempotent müşteri iptali sunar.
- Customer abuse overview/appeal response metadata OpenAPI'ye eklendi ve
  `/hesabim/itirazlar` private route'u açıldı. Müşteri yalnızca kendi strike,
  aktif sanction ve uygun closure case kayıtlarına itiraz başlatabilir; internal
  reason, admin actor veya platform inceleme detayı UI'a taşınmaz.

2026-06-14 notu:

- Booking draft parse/create mantığı component dışındaki helper'a taşındı ve Node
  test runner ile kapsandı. Draft business slug, TTL ve shape doğrulaması yapar;
  PII/auth token taşımaz.
- Kullanıcı slot/hizmet/şube/personel/tarih seçimini değiştirirse önceki create
  idempotency intent'i temizlenir. Aynı seçimle retry aynı key'i korur; yeni
  seçim yeni niyet sayılır.
- Appointment request create `409` veya `422` döndürürse seçili slot ve draft
  temizlenir; kullanıcı saatleri yeniden aramaya yönlendirilir. `401` durumunda
  draft korunur ve tek `/giris` kapısına return-to ile gidilir.

Kapanış kriterleri:

- Kullanıcı login'e yönlendiğinde booking draft kaybolmaz ve PII saklanmaz.
- Aynı create/cancel kullanıcı niyetinin retry'ı çift işlem üretmez.
- Slot değişimi gerektiren create hataları eski idempotency key'i yeni seçimde
  tekrar kullanmaz.
- `PendingApproval`, `Approved`, `Declined`, `Expired`, `Superseded` ve
  `CancelledByCustomer` durumları doğru ve anlaşılır gösterilir.
- Customer appeal create yalnız generated OpenAPI tipiyle çalışır ve güvenli
  müşteri response alanları dışına çıkmaz.
- Booking create ve cancel Playwright ile gerçek API üzerinde doğrulanır.

## F4 - İşletme Talep Kutusu

Durum: büyük ölçüde tamamlandı; canlı API inbox, güvenli tenant seçimi,
idempotent kararlar, abuse report ve staff/resource conflict uyarısı hazır.
Cursor pagination/search contract'ı, Playwright business approve/decline journey
ve responsive visual QA açık.

Amaç: işletme kullanıcısının onay bekleyen talepleri hızlı, güvenli ve branch
scope'a uygun biçimde yönetmesi.

Teslimatlar:

- Authenticated business context ve tenant/branch switcher
- Pending inbox, filtreli request listesi ve request detail
- Maskelenmiş müşteri bilgisi, hizmetler, branch/staff/resource görünen adları,
  branch-local zaman ve expiry görünümü
- Idempotent approve/decline
- Approval conflict, TTL expiry ve `Superseded` açıklamaları
- Abuse report reason-code ve note akışı
- Mobilde hızlı approve/decline; desktop'ta yoğun liste/detail düzeni

Backend bağımlılıkları:

- Business context endpoint'i
- Request read model'inde branch/staff/resource label ve timezone
- Cursor pagination

2026-06-09 notu:

- Business panel görsel yönü, session guard ve request inbox interaction'ı uygulandı.
- `GET /api/business/context` server tarafından çağrılıyor ve unavailable/401
  durumları güvenli UX'e dönüştürülüyor.
- `/api/business/appointment-requests` ve `/api/business/appointments` response
  content tipleri OpenAPI artifact'a işlendi; `/panel` artık request inbox
  satırlarını typed generated client ile canlı API'den okur.

2026-06-13 notu:

- `/panel` request kartlarından business abuse report akışı açıldı. İşletme
  kullanıcıları belirli appointment request için reason code seçip 300 karakterlik
  PII/secret uyarılı operasyon notuyla
  `/api/business/appointment-requests/{appointmentRequestId}/abuse-reports`
  endpoint'ine merkezi tenant client üzerinden rapor gönderir.
- Raporlama onay/ret kararından ayrı tutulur; tek başına strike veya sanction
  üretmez ve backend'in tenant+branch authz, idempotency ve günlük limit
  kontrollerine bırakılır.
- 2026-06-14: `/panel` query tabanlı doğrulanmış tenant seçimine geçti. Seçim
  yalnızca business context içinde dönen tenant membership listesiyle eşleşirse
  aktif olur; panel ve ayar snapshot linkleri aktif tenant'ı korur.
- Approve/decline idempotency key'leri request+decision niyeti boyunca sabitlendi;
  başarısız istek veya retry aynı key'i kullanır, başarılı karar sonrası key
  temizlenir.
- 2026-06-14: Approval conflict uyarısı ve onay sonrası local `Superseded`
  güncellemesi, aynı staff/time ve aynı resource/time çakışmalarını ayrı ayrı
  değerlendiren testli helper'a taşındı. Kısmi zaman aralığı overlap'leri de
  uyarıya girer; salt aynı saat fakat farklı staff/resource talepler gereksiz
  conflict sayılmaz.

Kapanış kriterleri:

- `BusinessOwner` tenant-wide, `BranchManager` branch-scoped davranış gerçek API
  ile doğrulanır.
- Frontend hiçbir yerde tenant GUID veya internal resource GUID'yi kullanıcıya
  operasyon etiketi olarak göstermez.
- Approve/decline retry ve conflict durumları çift karar üretmez.
- Raw müşteri PII response, log veya analytics'e sızmaz.

## F5 - Platform Control-plane

Durum: orta seviyede. Read-only abuse, tenant ve appeal/closure overview
yüzeyleri hazır; tenant lifecycle suspend/reactivate/close akışı gerçek admin
API'lerine reason + exact confirmation kapısıyla bağlandı. Provisioning,
membership ve abuse/appeal/closure karar mutation'ları hâlâ kapalıdır.

Amaç: yüksek riskli platform operasyonlarını sıradan dashboard aksiyonlarına
indirgemeden güvenli ve denetlenebilir bir UI üretmek.

Teslimatlar:

- MFA enrollment, step-up ve güvenilir oturum UX'i
- Tenant liste/detay, provisioning, suspend/reactivate/close
- Membership liste/add/suspend/revoke
- Abuse event/report listesi, user overview, strike ve sanction aksiyonları
- Appeal list/detail ve accept/reject
- Closure proposal, ikinci admin review, appeal window ve execute görünümü
- Reason zorunluluğu, internal/customer notice ayrımı ve high-risk confirmation
- Production'da kapalı closure execution durumunun görünür ve güvenli açıklaması

Kurallar:

- İlk PlatformAdmin bootstrap formu normal web uygulamasına eklenmez.
- `InternalReason`, actor kimliği ve raw abuse details customer yüzeyine taşınmaz.
- Frontend backend'in iki farklı admin, risk, appeal window veya membership
  kontrollerini taklit ederek bypass etmeye çalışmaz.

2026-06-13 notu:

- `/platform/abuse` salt-okunur control-plane başlangıcı açıldı. Route,
  authenticated `PlatformAdmin` ve geçerli step-up oturumu olmadan admin
  endpoint'lerini çağırmaz; backend `PlatformAdminWithStepUp` kapısı nihai
  otorite olarak kalır.
- Geçerli step-up yoksa kullanıcı yalnız `POST /api/session/step-up` formunu
  görür; parola/MFA/recovery bilgisi browser storage'a yazılmaz.
- Ekran gerçek `GET /api/admin/abuse/events`,
  `/api/admin/abuse/reports`, `/api/admin/abuse/appeals`,
  `/api/admin/abuse/closure-cases` ve
  `/api/admin/operations/reconciliation` response'larını generated OpenAPI tipiyle
  okur. Strike/sanction/appeal review/closure mutation butonları bu dilimde
  açılmadı.
- `/platform/tenantlar` salt-okunur tenant control-plane yüzeyi eklendi. Route,
  `GET /api/admin/tenants` ve seçili tenant detayı için
  `GET /api/admin/tenants/{tenantId}` response'larını generated OpenAPI tipleriyle
  okur; tenant header taşımaz.
- Provisioning ve membership add/suspend/revoke mutation butonları açılmadı.
  `Closed` tenant ve `Revoked` membership terminal durumları yalnızca bilgi olarak
  gösterilir.
- 2026-06-18: `/platform/tenantlar` seçili tenant detayında
  suspend/reactivate/close lifecycle aksiyonları gerçek
  `/api/admin/tenants/{tenantId}/...` endpoint'lerine bağlandı. Aksiyonlar
  platform-global `apiClient` ile cookie + step-up üzerinden gider, tenant header
  göndermez, PII/secret içermeyen zorunlu reason ve exact confirmation ister.
  `Closed` tenant terminal olduğu için UI'da geri alınabilir aksiyon gibi
  gösterilmez.
- `/platform/itirazlar` salt-okunur appeal/closure desk olarak eklendi. Route,
  `GET /api/admin/abuse/appeals`,
  `GET /api/admin/abuse/appeals/{appealId}`,
  `GET /api/admin/abuse/closure-cases` ve
  `GET /api/admin/abuse/closure-cases/{closureCaseId}` response'larını generated
  OpenAPI tipleriyle okur; tenant header taşımaz.
- Appeal accept/reject, closure approve/reject/execute ve closure proposal
  mutationları açılmadı. Ekran yalnızca step-up adminin karar bağlamını, müşteri
  beyanını, `CustomerNotice`/`InternalReason` ayrımını ve execution eligibility
  zamanlarını gösterir.

Kapanış kriterleri:

- Step-up olmayan session platform route'una erişemez.
- Tenant `Closed` ve membership `Revoked` terminal durumları UI'da geri alınabilir
  aksiyon gibi gösterilmez.
- Kritik mutation'lar gerçek API ve Playwright ile doğrulanır.
- Reason alanları uzunluk ve PII/secret uyarılarını taşır.

## F6 - İşletme Operasyon Derinliği

Durum: kısmen tamamlandı. F6; ADR-068 ile üç alt dilime ayrıldı: F6.1
(appointment operasyonları) tamamlandı, F6.3 (verified review operasyonu)
tamamlandı, F6.2 (settings CRUD) **backend Phase 5a sözleşmesini bekler**.

Amaç: request onay kutusundan gerçek salon operasyon paneline geçmek.

### F6.1 - Appointment Operasyonları (tamamlandı)

- Gün/hafta appointment calendar (`GET /api/business/appointments`)
- Confirmed appointment detail, business cancel, complete, no-show, note ve
  rebook
- Branch-timezone date picker, resource block ve rebook için UTC dönüşümü

### F6.2 - Settings CRUD (Phase 5a'ya bağımlı)

> Backend dayanağı: `roadmap/phase-5a-isletme-yonetim-crud.md`. Aşağıdaki
> ekranlar ilgili backend endpoint OpenAPI'ye girmeden sahte veriyle teslim
> edilmez. MVP lansmanı için minimal alt küme kararı `mvp-lansman-kapisi.md`
> içindeki "F6.2 MVP için zorunlu mu?" bölümünde izlenir.

- Branch, staff, skill ve membership scope yönetimi
- Service, service variant ve required skill/resource type yönetimi
- Resource block ve out-of-service yönetimi
- Working hours ve staff unavailable yönetimi
- Public profil metadata, galeri ve slot ayarı yönetimi (profil ayar formu
  hariç; o F6.1 kapsamında açıldı)

### F6.3 - Verified Review Operasyonu (tamamlandı)

- İşletme onaylı rezervasyon sonrası verified review operasyon akışı

2026-06-13 notu:

- `/panel` içinde `GET /api/business/appointments` ile kesinleşmiş randevu
  listesi açıldı.
- Confirmed randevular için cancel, complete, no-show ve business note
  aksiyonları tenant header + `Idempotency-Key` ile generated client üzerinden
  bağlandı.
- Cancel/no-show/rebook/resource block reason modal üzerinden zorunlu alınır;
  complete yalnız randevu bitişinden sonra, no-show yalnız randevu
  başlangıcından sonra UI'da açılır.
- Rebook ve resource block aksiyonları aynı appointment schedule akışına eklendi.
  Rebook mevcut staff/resource ile yeni UTC aralığına taşır; resource block
  seçili iç kaynağı kullanıcıya GUID göstermeden operasyonel olarak kapatır.
- `/panel/ayarlar` işletme yönetim snapshot'ı ve profil ayar formu olarak açıldı. Route,
  `GET /api/business/context` ile aktif tenant/membership bilgisini alır ve
  tenant slug üzerinden public profile read model'iyle şube, public personel,
  hizmet, varyant, çalışma saati, galeri ve profil metni durumunu gösterir.
- Public profil metni, SEO metadata ve staff display policy formu
  `PATCH /api/business/settings/profile` ile gerçek API'ye bağlandı. Form
  yalnızca BusinessOwner capability'siyle açılır; BranchManager ve Staff için
  read-only state gösterilir.
- Rebook ve resource block formları kullanıcıya UTC alanı göstermek yerine şube
  zamanı alanı gösterir; frontend bu değeri UTC'ye çevirerek mevcut backend
  kontratına gönderir.
- Appointment operasyon idempotency key'i dialog açıldığında üretilir ve aynı
  dialog niyeti boyunca korunur. Customer pending cancel akışı da aynı
  appointment request için retry'da aynı key'i kullanır.
- Personel, hizmet, şube, çalışma saati ve galeri düzenleme formları bu dilimde
  açılmadı; ilgili Organization/Catalog/Availability/Resources CRUD endpointleri
  OpenAPI'ye girmeden sahte ayar teslim edilmeyecek.
- Gelişmiş branch-timezone date picker ve resource/staff CRUD sonraki F6
  dilimleridir.

Tamamlanan appointment operasyonları tenant header, branch-scope authz, audit,
idempotency ve conflict kontrolüyle; resource block operasyonları tenant header,
branch-scope authz, audit ve conflict kontrolüyle kullanılmalıdır.

## F7 - Lansman Sertleşmesi

Durum: başlanmadı; kalite işleri önceki fazlarda da sürekli uygulanır.

Teslimatlar:

- Kritik customer/business/platform journey E2E paketi
- WCAG 2.2 AA otomatik + manuel kontrol
- Mobil gerçek cihaz ve düşük ağ profili testi
- Core Web Vitals field ölçümü, bundle analizi ve image/font optimizasyonu
- SEO, canonical, sitemap, robots ve structured-data doğrulaması
- Error monitoring, correlation id görünürlüğü ve PII redaction
- Dependency/secret tarama ve frontend security header doğrulaması
- Production cookie/origin/reverse proxy smoke testi
- İçerik, boş durum, hata metni ve Türkçe dil QA

Kapanış kriterleri:

- Public sayfalar 75. percentile hedeflerinde `LCP <= 2.5s`, `INP <= 200ms`,
  `CLS <= 0.1` ölçüm planına sahiptir.
- Booking create ve business approve/decline production-benzeri ortamda geçer.
- Admin high-risk aksiyonları step-up olmadan çalışmaz.
- PII browser log, analytics ve monitoring payload'larında görünmez.

## İlk Uygulama Sırası

1. P0 backend kontratları kapandı: OpenAPI artifact, session/bootstrap, business
   context, MFA step-up, global customer history ve internal resource kontratı.
2. `src/Apps/RezSaaS.Web` UI iskeletini oluştur; API client başlangıç iskeleti
   hazırdır.
3. Auth ekranları ve session/bootstrap guard'ı tamamla.
4. Business paneli authenticated kapıya al; tenant context'i canlı tut.
5. Tamamlandı: Business appointment/request read model OpenAPI response tipleri
   ve canlı appointment inbox bağlantısı.
6. Kısmen tamamlandı: temel design-system primitive'leri ve Node contract testi
   eklendi; Storybook ve a11y tooling kurulumu açık.
7. Devam ediyor: Public işletme profili dikey dilimi gerçek API ile uygulanıyor.
8. Slot seçimi ve booking draft prototipini tamamla.
9. Auth dönüşü + idempotent request create ile customer journey'yi kapat.
10. Customer self-service ve abuse appeal yüzeyini tamamla.
11. Devam ediyor: MFA/step-up kapısıyla platform control-plane'i dilim dilim aç.

## Bilinçli Olarak Ertelenenler

- Backend endpoint'i olmadan calendar, analytics veya settings mutation/form ekranı
- Sahte dashboard metrikleri
- Online ödeme/depozito UI'ı
- WhatsApp/SMS tercih ekranı
- Dark mode
- Ayrı mobile app
- Ayrı frontend reposu veya micro-frontend
- Public PlatformAdmin bootstrap UI'ı

## Her Dilimde Kapanış

- `pnpm lint`
- `pnpm typecheck`
- `pnpm test`
- Node test runner ile saf helper testleri: idempotency ve branch-local zaman
  dönüşümü
- Node test runner ile design-system contract testi: semantic token, button ve
  dialog primitive sözleşmeleri
- Storybook build ve a11y kontrolü
- İlgili Playwright journey testi
- Responsive ve Browser MCP/manual visual QA
- API contract generation drift kontrolü
- Authn, authz, tenant header, PII, idempotency ve rate-limit UX incelemesi
- ADR/domain/yetki/veri envanteri etkisi incelemesi
