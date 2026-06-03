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
- Staff seçimi MVP'de müşteri için opsiyonel; seçilmezse uygun staff adayları döner. Staff skill/service required skill eşlemesi slot motoruna hangi sırayla eklenecek?
- İşletme manuel rezervasyon eklediğinde müşteri hesabı şart olacak mı?
- Public rezervasyon isteği create endpoint'i için idempotency key formatı, saklama süresi ve tekrar denemede dönecek response netleştirilecek.

## Bildirim

- SMS sağlayıcısı ve gönderici adı operasyonu sonraki fazda hangi maliyet eşiği ve kategori pilotuyla açılacak?
- Production e-posta sağlayıcısı hangisi olacak?
- Telefon doğrulaması MVP lansman kapısı mı, yoksa kontrollü pilot özelliği mi?
- İşletme onay bekleyen talepleri e-posta dışında hangi kanaldan alacak?

## Operasyon ve Uyum

- İlk pilot şehir ve işletme kategorileri hangileri?
- KVKK aydınlatma, saklama ve silme politikası için hukuk danışmanlığı takvimi nedir?
- Platform support erişiminde kim, hangi gerekçeyle ve ne kadar süreyle tenant verisi görebilir?

## Identity/Auth Kapısı

- Production SMTP sağlayıcısı seçilip secret yönetimi ve confirmation/password reset teslimatı uçtan uca doğrulanmalıdır.
- Platform admin ve işletme yönetim hesapları için MFA enrollment UI'ı ve güvenilir cihaz/oturum davranışı netleştirilmelidir.
- İlk `PlatformAdmin` bootstrap token'ının kim tarafından, hangi runbook ile ve ne kadar süre geçerli üretileceği belirlenmelidir.

## Tenant ve Organization

- İlk tenant oluşturma akışı self-service mi, platform onaylı bootstrap mı olacak?
- Tenant slug değiştirilebilir mi; değiştirilirse eski URL için redirect/alias tutulacak mı?
- Bir tenant içinde birden fazla `Business` desteklenecekse MVP sınırı hangi noktada açılacak?
