# Veri Envanteri Taslağı

Bu belge hukuki metin değildir; KVKK danışmanlığı öncesi teknik veri envanteri başlangıcıdır.

| Veri Grubu | Örnekler | Amaç | Erişim | Saklama Kararı |
| --- | --- | --- | --- | --- |
| Hesap kimliği | Ad, e-posta, telefon, doğrulama durumu, hesap durumu, lockout | Kimlik, iletişim ve güvenlik | Kullanıcı, sınırlı destek | Netleştirilecek |
| Tenant kaydı | Slug, görünen ad, durum, oluşturma/kapatma zamanı | Veri izolasyonu ve işletme yaşam döngüsü | İşletme sahibi, yetkili admin | Netleştirilecek |
| İşletme üyeliği | User account id, tenant, rol, branch scope, durum | Yetkilendirme | İşletme sahibi, yetkili admin | Netleştirilecek |
| Rezervasyon | Hizmet, zaman, staff, resource, durum | Operasyon | Müşteri, ilgili işletme | Netleştirilecek |
| Tenant audit | Actor id, tenant, aksiyon, zaman, JSON detay | Güvenlik, inceleme ve değişiklik kanıtı | Yetkili admin | Append-only; saklama süresi netleştirilecek |
| Audit | Actor, aksiyon, zaman, gerekçe | Güvenlik ve inceleme | Yetkili admin | Netleştirilecek |
| Abuse | Strike, sinyal, yaptırım, itiraz | Platform güvenliği | Platform admin | Netleştirilecek |
| Bildirim | Kanal, alıcı maskesi, template, provider sonucu | Teslimat ve hata çözümü | Sınırlı destek | Netleştirilecek |
| Teknik telemetri | IP, device sinyali, correlation id | Abuse ve hata çözümü | Sınırlı güvenlik ekibi | Netleştirilecek |

## İlkeler

- Minimum veri: iş akışı için gerekmeyen alan toplanmaz.
- Serbest metin müşteri notları sınırlanır; hassas veri girişi için uyarı ve erişim kısıtı planlanır.
- Silme, anonimleştirme, export ve itiraz süreçleri veri grubu bazında tasarlanır.
- Backup kopyaları ve log sistemleri saklama politikasına dahildir.
- Destek erişimi süreli, gerekçeli ve auditli olmalıdır.
