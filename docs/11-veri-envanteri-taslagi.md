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
| Rezervasyon | Hizmet, zaman, staff, resource, durum, işletme iç notu, cancel/complete/no-show/rebook actor ve nedenleri | Operasyon | Müşteri yalnız kendi güvenli özetini görür; iç not ve operasyon nedenleri ilgili işletme rolleri | Netleştirilecek |
| Booking idempotency | Tenant, actor id, operation, key hash, request hash, response özeti | Komut retry güvenliği ve çift işlem engelleme | Sistem ve sınırlı operasyon | Saklama/temizleme süresi netleştirilecek |
| Ödeme hazırlığı | Payment policy, payment intent, provider key/reference, hosted checkout URL, webhook event id, payload hash, ödeme audit detayı | Phase 4 ödeme tahsilatı hazırlığı, webhook idempotency ve operasyon kanıtı | Varsayılan kapalı; readiness yalnız step-up platform admin, tenant ödeme kayıtları ileride yetkili işletme/admin | Kart verisi ve raw provider payload'u tutulmaz; saklama süresi netleştirilecek |
| Entegrasyon hazırlığı | API client display name, key prefix/hash, scope set, webhook target URL, event type, signing secret hash, payload hash, correlation id, delivery status, integration audit detayı | Phase 5 dış API/webhook ve CRM/export hazırlığı | Varsayılan kapalı; readiness yalnız step-up platform admin, tenant integration kayıtları ileride yetkili işletme/admin | Raw API key, signing secret ve raw webhook payload'u tutulmaz; saklama süresi netleştirilecek |
| Identity audit | Actor, subject, aksiyon, zaman, JSON detay | Güvenlik ve privileged bootstrap kanıtı | Platform admin | Append-only; saklama süresi netleştirilecek |
| Tenant audit | Actor id, tenant, aksiyon, zaman, JSON detay, lifecycle operasyon nedeni | Güvenlik, inceleme ve değişiklik kanıtı | Yetkili admin | Append-only; saklama süresi netleştirilecek |
| Admin audit | Actor, aksiyon, zaman, JSON detay | Platform operasyon incelemesi | Platform admin | Append-only; saklama süresi netleştirilecek |
| Abuse | Event, severity, user, tenant, business report, appointment request referansı, sınırlı note, review kararı, strike, risk seviyesi, sanction, appeal statement, closure internal reason/customer notice, apply/revoke/review actor ve neden | Platform güvenliği ve itiraz | Müşteri yalnızca kendi güvenli özetini görür; internal nedenler ve karar detayları step-up platform admin | Append-only geçmiş; strike expiry uygulanır, appeal/closure genel saklama süresi netleştirilecek |
| Bildirim | Tenant mesajlarında maskelenmiş alıcı; platform mesajlarında user account id, amaç, correlation/delivery key, müşteri-güvenli konu/gövde, deneme ve sağlayıcı kabul zamanı | Teslimat, güvenlik bildirimi ve hata çözümü | Sistem ve sınırlı destek | Raw e-posta platform outbox'a yazılmaz; saklama/temizleme süresi netleştirilecek |
| Teknik telemetri | IP, device sinyali, correlation id | Abuse ve hata çözümü | Sınırlı güvenlik ekibi | Netleştirilecek |

## İlkeler

- Minimum veri: iş akışı için gerekmeyen alan toplanmaz.
- Serbest metin müşteri notları sınırlanır; hassas veri girişi için uyarı ve erişim kısıtı planlanır.
- Tenant lifecycle operasyon nedeni uzunluk sınırlıdır; PII, secret veya erişim bilgisi içermemelidir.
- Abuse report note ve review/revoke nedenleri 300 karakterle sınırlıdır; PII, secret, token veya erişim bilgisi içermemelidir.
- Appeal statement ve closure metinleri uzunluk sınırlıdır; `InternalReason` müşteri response'una veya log'a eklenmez, `CustomerNotice` güvenli müşteri metni olarak ayrı tutulur.
- Silme, anonimleştirme, export ve itiraz süreçleri veri grubu bazında tasarlanır.
- Backup kopyaları ve log sistemleri saklama politikasına dahildir.
- Destek erişimi süreli, gerekçeli ve auditli olmalıdır.
