# Veri Envanteri Taslağı

Bu belge hukuki metin değildir; KVKK danışmanlığı öncesi teknik veri envanteri başlangıcıdır.

| Veri Grubu | Örnekler | Amaç | Erişim | Saklama Kararı |
| --- | --- | --- | --- | --- |
| Hesap kimliği | Ad, e-posta, telefon, doğrulama durumu, hesap durumu, lockout | Kimlik, iletişim ve güvenlik | Kullanıcı, sınırlı destek | Netleştirilecek |
| Tenant kaydı | Slug, görünen ad, durum, oluşturma/kapatma zamanı | Veri izolasyonu ve işletme yaşam döngüsü | İşletme sahibi, yetkili admin | Netleştirilecek |
| İşletme üyeliği | User account id, tenant, rol, branch scope, durum | Yetkilendirme | İşletme sahibi, yetkili admin | Netleştirilecek |
| Organizasyon | Business, public slug, açıklama, profil kuralları, SEO metadata, galeri URL/alt text, rating özeti, branch şehir/ilçe/adres, timezone, staff, skill | İşletme kurulumu, public keşif ve operasyon | Public özet/profil alanları anonim; detay/yönetim işletme rolleri | Netleştirilecek |
| Katalog ve kaynak | Hizmet, varyant, fiyat, resource type, resource, block | Rezervasyon uygunluğu ve planlama | Public hizmet/varyant menüsü anonim; kaynak ve yönetim işletme rolleri | Netleştirilecek |
| Uygunluk | Çalışma saatleri, staff unavailable time | Slot hesaplama | Public çalışma saatleri anonim; staff unavailable yönetimi işletme rolleri | Netleştirilecek |
| Rezervasyon | Hizmet, zaman, staff, resource, durum | Operasyon | Müşteri, ilgili işletme | Netleştirilecek |
| Booking idempotency | Tenant, actor id, operation, key hash, request hash, response özeti | Komut retry güvenliği ve çift işlem engelleme | Sistem ve sınırlı operasyon | Saklama/temizleme süresi netleştirilecek |
| Identity audit | Actor, subject, aksiyon, zaman, JSON detay | Güvenlik ve privileged bootstrap kanıtı | Platform admin | Append-only; saklama süresi netleştirilecek |
| Tenant audit | Actor id, tenant, aksiyon, zaman, JSON detay, lifecycle operasyon nedeni | Güvenlik, inceleme ve değişiklik kanıtı | Yetkili admin | Append-only; saklama süresi netleştirilecek |
| Admin audit | Actor, aksiyon, zaman, JSON detay | Platform operasyon incelemesi | Platform admin | Append-only; saklama süresi netleştirilecek |
| Abuse | Event, severity, user, tenant, business report, appointment request referansı, sınırlı note, review kararı, strike, risk seviyesi, sanction, apply/revoke actor ve neden | Platform güvenliği | İşletme yalnızca kendi oluşturma cevabını sınırlı görür; detay ve kararlar step-up platform admin | Append-only geçmiş; strike expiry uygulanır, genel saklama süresi netleştirilecek |
| Bildirim | Kanal, maskelenmiş alıcı, template, payload, provider sonucu | Teslimat ve hata çözümü | Sınırlı destek | Netleştirilecek |
| Teknik telemetri | IP, device sinyali, correlation id | Abuse ve hata çözümü | Sınırlı güvenlik ekibi | Netleştirilecek |

## İlkeler

- Minimum veri: iş akışı için gerekmeyen alan toplanmaz.
- Serbest metin müşteri notları sınırlanır; hassas veri girişi için uyarı ve erişim kısıtı planlanır.
- Tenant lifecycle operasyon nedeni uzunluk sınırlıdır; PII, secret veya erişim bilgisi içermemelidir.
- Abuse report note ve review/revoke nedenleri 300 karakterle sınırlıdır; PII, secret, token veya erişim bilgisi içermemelidir.
- Silme, anonimleştirme, export ve itiraz süreçleri veri grubu bazında tasarlanır.
- Backup kopyaları ve log sistemleri saklama politikasına dahildir.
- Destek erişimi süreli, gerekçeli ve auditli olmalıdır.
