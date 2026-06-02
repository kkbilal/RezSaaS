# Veri Envanteri Taslağı

Bu belge hukuki metin değildir; KVKK danışmanlığı öncesi teknik veri envanteri başlangıcıdır.

| Veri Grubu | Örnekler | Amaç | Erişim | Saklama Kararı |
| --- | --- | --- | --- | --- |
| Hesap kimliği | Ad, e-posta, telefon, doğrulama durumu, hesap durumu, lockout | Kimlik, iletişim ve güvenlik | Kullanıcı, sınırlı destek | Netleştirilecek |
| Tenant kaydı | Slug, görünen ad, durum, oluşturma/kapatma zamanı | Veri izolasyonu ve işletme yaşam döngüsü | İşletme sahibi, yetkili admin | Netleştirilecek |
| İşletme üyeliği | User account id, tenant, rol, branch scope, durum | Yetkilendirme | İşletme sahibi, yetkili admin | Netleştirilecek |
| Organizasyon | Business, branch, timezone, staff, skill | İşletme kurulumu ve operasyon | İşletme sahibi, branch manager | Netleştirilecek |
| Katalog ve kaynak | Hizmet, varyant, fiyat, resource type, resource, block | Rezervasyon uygunluğu ve planlama | İşletme rolleri | Netleştirilecek |
| Uygunluk | Çalışma saatleri, staff unavailable time | Slot hesaplama | İşletme rolleri | Netleştirilecek |
| Rezervasyon | Hizmet, zaman, staff, resource, durum | Operasyon | Müşteri, ilgili işletme | Netleştirilecek |
| Identity audit | Actor, subject, aksiyon, zaman, JSON detay | Güvenlik ve privileged bootstrap kanıtı | Platform admin | Append-only; saklama süresi netleştirilecek |
| Tenant audit | Actor id, tenant, aksiyon, zaman, JSON detay | Güvenlik, inceleme ve değişiklik kanıtı | Yetkili admin | Append-only; saklama süresi netleştirilecek |
| Admin audit | Actor, aksiyon, zaman, JSON detay | Platform operasyon incelemesi | Platform admin | Append-only; saklama süresi netleştirilecek |
| Abuse | Event, severity, user, tenant, sanction | Platform güvenliği | Platform admin | Netleştirilecek |
| Bildirim | Kanal, maskelenmiş alıcı, template, payload, provider sonucu | Teslimat ve hata çözümü | Sınırlı destek | Netleştirilecek |
| Teknik telemetri | IP, device sinyali, correlation id | Abuse ve hata çözümü | Sınırlı güvenlik ekibi | Netleştirilecek |

## İlkeler

- Minimum veri: iş akışı için gerekmeyen alan toplanmaz.
- Serbest metin müşteri notları sınırlanır; hassas veri girişi için uyarı ve erişim kısıtı planlanır.
- Silme, anonimleştirme, export ve itiraz süreçleri veri grubu bazında tasarlanır.
- Backup kopyaları ve log sistemleri saklama politikasına dahildir.
- Destek erişimi süreli, gerekçeli ve auditli olmalıdır.
