# Açık Sorular

Bu sorular Phase 0 kapanışından önce yanıtlanmalıdır. Kararlar `06-karar-kaydi.md` içine işlenir.

## Ürün

- İşletme sayfası URL yapısı Phase 2 başlangıcında `/isletme/{businessSlug}` olarak kararlaştırıldı; şehir/kategori içeren SEO landing sayfaları ayrı discovery sayfaları olarak değerlendirilecek.
- Bir tenant ilk sürümde tam olarak bir `Business` mı içerir?
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
- Bir tenant içinde birden fazla `Business` desteklenecekse MVP sınırı hangi noktada açılacak?
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
