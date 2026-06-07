# Frontend Uygulama Planı

Son güncelleme: 2026-06-04

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

Durum: başlanmadı.

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

Kapanış kriterleri:

- Route haritası ve kullanıcı akışları onaylıdır.
- Public ve operasyon yüzeylerinin görsel yönü birbirinden farklı yoğunlukta
  fakat aynı token sistemi içindedir.
- API tipleri elle yazılmadan üretilebilmektedir.
- Cookie auth, origin guard ve local web/API akışı test ortamında çalışmaktadır.
- Frontend'in kullanamayacağı mevcut backend boşlukları issue/dilim sahibiyle
  eşleştirilmiştir.

## F1 - Tasarım Sistemi ve Uygulama Kabukları

Durum: başlanmadı.

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

Kapanış kriterleri:

- Component'ler mobile/desktop, loading/empty/error ve uzun Türkçe içerik
  durumlarında doğrulanmıştır.
- Storybook a11y kritik ihlallerde CI'ı düşürür.
- Ürün ekranları library varsayılan görünümüyle yayınlanmaz.
- Tasarım token'ı dışındaki keyfi renk/radius kullanımı review kuralıdır.

## F2 - Public Keşif ve İşletme Profili

Durum: başlanmadı.

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

Kapanış kriterleri:

- İşletme profili JS kapalı veya yavaş bağlantıda temel içeriği gösterebilir.
- Public URL paylaşılabilir ve indexlenebilir metadata taşır.
- Public kritik route'lar Core Web Vitals bütçesi ve Playwright smoke testini
  geçer.
- Search kartları N+1 profile çağrısı üretmez.

## F3 - Auth, Rezervasyon ve Müşteri Self-service

Durum: başlanmadı.

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

Kapanış kriterleri:

- Kullanıcı login'e yönlendiğinde booking draft kaybolmaz ve PII saklanmaz.
- Aynı create/cancel kullanıcı niyetinin retry'ı çift işlem üretmez.
- `PendingApproval`, `Approved`, `Declined`, `Expired`, `Superseded` ve
  `CancelledByCustomer` durumları doğru ve anlaşılır gösterilir.
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

Durum: backend API'leri bekleniyor.

Amaç: request onay kutusundan gerçek salon operasyon paneline geçmek.

Planlanan teslimatlar:

- Gün/hafta appointment calendar
- Confirmed appointment detail, business/customer cancel, complete, no-show ve
  rebook
- Branch, staff, skill ve membership scope yönetimi
- Service, service variant ve required skill/resource type yönetimi
- Resource, resource block ve out-of-service yönetimi
- Working hours ve staff unavailable yönetimi
- Public profil metadata, galeri ve slot ayarı yönetimi
- Verified review operasyonu

Bu fazda frontend başlamadan her use-case için backend endpoint, authz, tenant
isolation, audit, idempotency ve concurrency kontratı tamamlanır.

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
2. `src/Apps/RezSaaS.Web` UI iskeletini ve CI job'larını oluştur; API client
   başlangıç iskeleti hazırdır.
3. Figma flow/wireframe ve token çalışmasını başlat.
4. Storybook ve temel design-system component'lerini kur.
5. Public işletme profili dikey dilimini gerçek API ile tamamla.
6. Slot seçimi ve booking draft prototipini tamamla.
7. Auth dönüşü + idempotent request create ile customer journey'yi kapat.
8. Business request inbox + approve/decline dilimini tamamla.
9. Customer self-service ve abuse appeal yüzeyini tamamla.
10. MFA hazır olduğunda platform control-plane'i dilim dilim aç.

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
