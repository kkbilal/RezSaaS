# Açık Sorular

Bu sorular ürün/mimari karar noktalarıdır; her biri bir fazı **bloke edebilir**.
Kararlar `06-karar-kaydi.md` içine işlenir.

> **Güncelleme (ADR-068, 2026-06-20):** Phase 0 artık tamamlandı; ancak
> aşağıdaki soruların çoğu hâlâ açıktır. "Tümü Phase 0 öncesi" yerine artık
> **aşağıdaki "Faz Blokaj İzi" tablosu** kaynak doğrudur: hangi sorunun hangi
> fazı (veya MVP lansman kapısını) bloke ettiği orada izlenir. Açık soruyu
> blokaj olarak işaretlemeden ilgili faz başlatılmamalıdır.

## Faz Blokaj İzi (ADR-068)

| Açık soru / karar | Bloke ettiği faz veya kapı | Not |
| --- | --- | --- |
| Production SMTP sağlayıcısı + secret + confirmation/reset E2E doğrulaması | **MVP lansman kapısı** | ADR-019 fail-fast kuralı |
| Platform admin/işletme yönetimi MFA enrollment UI + güvenilir cihaz | **MVP lansman kapısı** (privileged yüzeyler) | ADR-059 |
| İlk `PlatformAdmin` bootstrap token runbook'u | **MVP lansman kapısı** (ilk tenant provisioning) | ADR-026/044 |
| `responseBuffer` ve hazırlık/temizlik buffer süresi kararı | MVP (slot hesabı tutarlılığı) | ADR-007 ile ilişkili |
| Booking idempotency kayıtları saklama/temizleme politikası | **MVP lansman kapısı** (operasyon) | ADR-037 |
| Çoklu aktif `Business` desteği + lifecycle contract | Phase 5a (genişletme), mevcut tenant tek-business varsayımı korunur | ADR-064 |
| SMS sağlayıcısı + gönderici adı + maliyet eşiği | Phase 5d | ADR-009/029 |
| WhatsApp Business pilot kategorisi/onboarding | Phase 5d | ADR-009 |
| Telefon doğrulaması MVP kapısı mı yoksa pilot mu? | Phase 5d (pilot) önerisi | Ürün çağrısı |
| Tenant slug değişimi + redirect/alias politikası | Phase 5a | ADR-030 |
| `BranchManager`/`Staff` branch scope Organization lifecycle contract | Phase 5a (yetki ağacı) | ADR-035/046 |
| Ödeme sağlayıcısı seçimi (hosted checkout adapter) | Phase 4a | ADR-065 |
| İptal politikası/no-show ücret kuralı taksonomisi | Phase 4b | ADR-008 sonrası |
| Paket/membership/gift card ürün doğrulaması | Phase 4c (opsiyonel) | Ürün çağrısı |
| CRM/export sağlayıcı adapter'ları | Phase 5c | ADR-066/067 |
| Public API scope + rate limit + partner onboarding | Phase 5c | ADR-066 |
| Marketplace sponsored placement hacmi | Phase 5e | ADR-051 sonrası |
| i18n locale/currency + provider abstraction | Phase 5e | En yüksek riskli |
| KVKK saklama/silme politikası + hukuk takvimi | **MVP lansman kapısı** (uyumluluk) | `docs/02`, `docs/11` |
| Abuse appeal SLA + reason-code taksonomisi | Phase 3 kapanışı (kısmen açık), Phase 5e'yi etkileyebilir | ADR-050 |
| Discovery facet taksonomisi (kategori/şehir/ilçe) statik mi read model mi? | MVP (F2 facet kapanışı) | ADR-031/032 |

## Ürün

- İşletme sayfası URL yapısı Phase 2 başlangıcında `/isletme/{businessSlug}` olarak kararlaştırıldı; şehir/kategori içeren SEO landing sayfaları ayrı discovery sayfaları olarak değerlendirilecek.
- MVP'de tenant self-service ayar yüzeyi tenant başına tek aktif `Business`
  varsayımıyla açıldı; çoklu aktif `Business` görülürse settings mutation `409`
  döner. Multi-business destek ayrı ADR ve lifecycle contract ister.
- İşletme talebi 24 saat içinde cevaplamazsa görünür bir yanıt süresi metriği veya yaptırım olacak mı?
- Müşterinin aynı zaman aralığında farklı işletmelere kaç açık talep göndermesine izin verilecek?

## Rezervasyon

- `responseBuffer` kaç saat olacak? Örnek: başlangıca 2 saatten az kalan slot için talep açılamaz.
- Hizmetler arası hazırlık/temizlik buffer süresi MVP'de olacak mı?
- Staff seçimi MVP'de müşteri için opsiyonel; seçilmezse uygun staff adayları döner. Skill eşlemesi Phase 2'de slot motoru ve create doğrulamasına eklendi.
- İşletme manuel rezervasyon eklediğinde müşteri hesabı şart olacak mı?
- Booking idempotency kayıtları için saklama/temizleme süresi ve operasyonel dashboard ihtiyacı netleştirilecek.

## Bildirim

- SMS sağlayıcısı ve gönderici adı operasyonu sonraki fazda hangi maliyet eşiği ve kategori pilotuyla açılacak?
- Production e-posta sağlayıcısı hangisi olacak?
- Telefon doğrulaması MVP lansman kapısı mı, yoksa kontrollü pilot özelliği mi?
- İşletme onay bekleyen talepleri e-posta dışında hangi kanaldan alacak?

## Operasyon ve Uyum

- İlk pilot şehir ve işletme kategorileri hangileri?
- KVKK aydınlatma, saklama ve silme politikası için hukuk danışmanlığı takvimi nedir?
- Platform support erişiminde kim, hangi gerekçeyle ve ne kadar süreyle tenant verisi görebilir?
- Abuse appeal/itiraz SLA'sı, kalıcı hesap kapatma onay mercii ve reason-code taksonomisi nasıl netleşecek?
- Sağlayıcı kabulünden sonra bounce/kalıcı teslimat başarısızlığı webhook ile alınırsa closure case ve itiraz penceresi için hangi operasyonel aksiyon uygulanacak?
- Terminal `Failed` platform bildirimleri için step-up, auditli ve idempotent kontrollü requeue yüzeyi hangi sağlayıcı/incident gereksinimleriyle açılacak?
- İşletme abuse raporlarını kötüye kullanan tenant/actor için ayrı risk ve yaptırım politikası nasıl olacak?

## Identity/Auth Kapısı

- Production SMTP sağlayıcısı seçilip secret yönetimi ve confirmation/password reset teslimatı uçtan uca doğrulanmalıdır.
- Platform admin ve işletme yönetim hesapları için MFA enrollment UI'ı ve güvenilir cihaz/oturum davranışı netleştirilmelidir.
- İlk `PlatformAdmin` bootstrap token'ının kim tarafından, hangi runbook ile ve ne kadar süre geçerli üretileceği belirlenmelidir.

## Tenant ve Organization

- İlk tenant oluşturma akışı self-service mi, platform onaylı bootstrap mı olacak?
- Tenant slug değiştirilebilir mi; değiştirilirse eski URL için redirect/alias tutulacak mı?
- Bir tenant içinde birden fazla `Business` desteklenecekse MVP sonrası hangi
  onboarding, role scope, URL ve settings contract ile açılacak?
- `BranchManager`/`Staff` tenant membership branch scope doğrulaması Organization branch lifecycle kaynağına hangi contract ile bağlanacak?
- Tenant suspend/close sonrasında açık `PendingApproval` taleplerinin otomatik kapanma/expiry politikası hangi integration event veya maintenance job ile yürütülecek?

## Frontend ve API Sözleşmesi

- Authenticated session/bootstrap response'u platform rolleri, aktif tenant
  membership'leri ve MFA/step-up geçerlilik süresini hangi tekil kontratla
  döndürecek?
- Public booking'de staff tercihi "fark etmez" olduğunda exact staff ve internal
  resource create sırasında server tarafından nasıl seçilecek ve concurrency
  altında nasıl yeniden doğrulanacak?
- Müşterinin tüm işletmelerdeki request ve confirmed appointment geçmişi hangi
  global customer read model'i ile sunulacak?
- Business panel context response'u tenant, branch scope, branch/staff/resource
  görünen adları ve timezone bilgisini hangi composition contract ile döndürecek?
- Discovery filtreleri için kategori/şehir/ilçe facet taksonomisi statik ürün
  sözlüğü mü, backend read model'i mi olacak?
