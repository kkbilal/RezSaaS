# Frontend Uygulama Planı

Son güncelleme: 2026-06-13

Bu plan frontend geliştirmesini küçük, doğrulanabilir dikey dilimlere böler.
Frontend fazları mevcut ürün/backend `Phase 0-5` numaralarıyla karışmaması için
`F0-F7` olarak adlandırılır.

Her dilim yalnızca ekran çıktısıyla değil; API readiness, erişilebilirlik,
responsive davranış, test, doküman ve güvenlik kontrolüyle kapanır.

## Backend ile Eşleşme

| Frontend fazı | Ana backend dayanağı |
| --- | --- |
| F0-F1 | Phase 1 Identity/Auth ve mimari temel |
| F2-F4 | Tamamlanmış Phase 2 public discovery ve booking API'leri |
| F3 ve F5 | Devam eden Phase 3 abuse, appeal, tenant ve control-plane API'leri |
| F6 | Phase 3 operasyon derinliği için henüz açılmamış command/query API'leri |
| Sonraki ürün fazları | Phase 4 payments ve Phase 5 analytics/integration |

## F0 - Ürün UX Keşfi, API Hazırlığı ve Repo İskeleti

Durum: devam ediyor.

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

Durum: devam ediyor.

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

Kapanış kriterleri:

- Component'ler mobile/desktop, loading/empty/error ve uzun Türkçe içerik
  durumlarında doğrulanmıştır.
- Storybook a11y kritik ihlallerde CI'ı düşürür.
- Ürün ekranları library varsayılan görünümüyle yayınlanmaz.
- Tasarım token'ı dışındaki keyfi renk/radius kullanımı review kuralıdır.

## F2 - Public Keşif ve İşletme Profili

Durum: discovery/profile dikey dilimi başladı.

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

Kapanış kriterleri:

- İşletme profili JS kapalı veya yavaş bağlantıda temel içeriği gösterebilir.
- Public URL paylaşılabilir ve indexlenebilir metadata taşır.
- Public kritik route'lar Core Web Vitals bütçesi ve Playwright smoke testini
  geçer.
- Search kartları N+1 profile çağrısı üretmez.

## F3 - Auth, Rezervasyon ve Müşteri Self-service

Durum: booking başlangıç paneli başladı.

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

Kapanış kriterleri:

- Kullanıcı login'e yönlendiğinde booking draft kaybolmaz ve PII saklanmaz.
- Aynı create/cancel kullanıcı niyetinin retry'ı çift işlem üretmez.
- `PendingApproval`, `Approved`, `Declined`, `Expired`, `Superseded` ve
  `CancelledByCustomer` durumları doğru ve anlaşılır gösterilir.
- Customer appeal create yalnız generated OpenAPI tipiyle çalışır ve güvenli
  müşteri response alanları dışına çıkmaz.
- Booking create ve cancel Playwright ile gerçek API üzerinde doğrulanır.

## F4 - İşletme Talep Kutusu

Durum: başlanmadı.

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

Kapanış kriterleri:

- `BusinessOwner` tenant-wide, `BranchManager` branch-scoped davranış gerçek API
  ile doğrulanır.
- Frontend hiçbir yerde tenant GUID veya internal resource GUID'yi kullanıcıya
  operasyon etiketi olarak göstermez.
- Approve/decline retry ve conflict durumları çift karar üretmez.
- Raw müşteri PII response, log veya analytics'e sızmaz.

## F5 - Platform Control-plane

Durum: başlanmadı.

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

Kapanış kriterleri:

- Step-up olmayan session platform route'una erişemez.
- Tenant `Closed` ve membership `Revoked` terminal durumları UI'da geri alınabilir
  aksiyon gibi gösterilmez.
- Kritik mutation'lar gerçek API ve Playwright ile doğrulanır.
- Reason alanları uzunluk ve PII/secret uyarılarını taşır.

## F6 - İşletme Operasyon Derinliği

Durum: başladı. Appointment calendar/detail, note, cancel, complete, no-show,
rebook ve resource block backend API'leri hazır; ayar CRUD'larının bir kısmı
sonraki dilimlerde açılacak.

Amaç: request onay kutusundan gerçek salon operasyon paneline geçmek.

Planlanan teslimatlar:

- Gün/hafta appointment calendar (`GET /api/business/appointments`)
- Confirmed appointment detail, business cancel, complete, no-show, note ve
  rebook
- Branch, staff, skill ve membership scope yönetimi
- Service, service variant ve required skill/resource type yönetimi
- Resource block ve out-of-service yönetimi
- Working hours ve staff unavailable yönetimi
- Public profil metadata, galeri ve slot ayarı yönetimi
- Verified review operasyonu

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
- Ayar yönetimi, gelişmiş branch-timezone date picker ve resource/staff CRUD
  sonraki F6 dilimleridir.

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
6. Storybook ve temel design-system component'lerini kur.
7. Devam ediyor: Public işletme profili dikey dilimi gerçek API ile uygulanıyor.
8. Slot seçimi ve booking draft prototipini tamamla.
9. Auth dönüşü + idempotent request create ile customer journey'yi kapat.
10. Customer self-service ve abuse appeal yüzeyini tamamla.
11. MFA hazır olduğunda platform control-plane'i dilim dilim aç.

## Bilinçli Olarak Ertelenenler

- Backend endpoint'i olmadan calendar, analytics veya settings ekranı
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
- Storybook build ve a11y kontrolü
- İlgili Playwright journey testi
- Responsive ve Browser MCP/manual visual QA
- API contract generation drift kontrolü
- Authn, authz, tenant header, PII, idempotency ve rate-limit UX incelemesi
- ADR/domain/yetki/veri envanteri etkisi incelemesi
