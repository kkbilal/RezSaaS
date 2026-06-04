# Admin Abuse Control-plane

## Amaç

Admin abuse control-plane; otomatik üretilen abuse sinyallerini platform operasyonuna görünür kılar, süreli kullanıcı yaptırımlarını auditli uygular ve aktif yaptırımı yeni rezervasyon isteği oluşturma kapısında enforce eder.

Bu yüzey tenant işletme paneli değildir. Tüm endpoint'ler `PlatformAdminWithStepUp` ve admin operasyon rate limit'i ister.

## Uygulanan Yüzey

- `GET /api/admin/abuse/events`: user, tenant ve severity filtreli abuse event listesi
- `GET /api/admin/abuse/users/{userAccountId}`: kullanıcının abuse event ve sanction geçmişi
- `POST /api/admin/abuse/users/{userAccountId}/sanctions`: warning/cooldown/temporary ban uygulama
- `POST /api/admin/abuse/users/{userAccountId}/sanctions/{sanctionId}/revoke`: aktif yaptırımı geçmiş kaydı silmeden geri alma
- `GET /api/admin/abuse/appeals`: müşteri itirazlarını filtreli listeleme
- `GET /api/admin/abuse/appeals/{appealId}`: itiraz detayı
- `POST /api/admin/abuse/appeals/{appealId}/accept|reject`: itiraz kararı
- `GET /api/admin/abuse/closure-cases`: hesap kapatma vakalarını filtreli listeleme
- `GET /api/admin/abuse/closure-cases/{closureCaseId}`: hesap kapatma vaka detayı
- `POST /api/admin/abuse/users/{userAccountId}/closure-cases`: yüksek riskli hesap için kapatma teklifi
- `POST /api/admin/abuse/closure-cases/{closureCaseId}/approve|reject|execute`: bağımsız karar ve retry edilebilir execution
- `GET /api/admin/operations/reconciliation`: platform bildirim ve closure saga incident sayılarını PII sızdırmadan gösteren salt-okunur operasyon snapshot'ı

## Yaptırım Kuralları

- `Warning`: tarihsel ve auditli uyarıdır; booking bloklamaz ve revoke edilmez.
- `Cooldown`: yeni booking request oluşturmayı bloklar; bitiş zamanı zorunlu ve en fazla 24 saattir.
- `TemporaryBan`: yeni booking request oluşturmayı bloklar; süre 24–72 saat aralığındadır.
- `PermanentClosure`: sanction endpoint'i tarafından uygulanmaz; manuel inceleme, itiraz yolu ve Identity hesap kapatma orchestration'ı ister.
- Aynı kullanıcı için aynı anda birden fazla aktif bloklayıcı sanction uygulanmaz.
- Sanction apply işlemi PostgreSQL advisory transaction lock ile yarış koşuluna karşı korunur.
- Sanction revoke işlemi ilgili sanction satırını PostgreSQL row lock ile korur.
- Apply ve revoke state değişiklikleri `AdminAuditLogEntry` üretir; sanction geçmişi silinmez.

## Booking Enforce Sınırı

- Aktif `Cooldown`, `TemporaryBan` veya gelecekteki manuel `PermanentClosure` yeni public booking request oluşturmayı `403` ile reddeder.
- Idempotent booking replay, daha önce başarıyla üretilmiş cevabı korur.
- Warning booking'i bloklamaz.
- Sanction; müşterinin kendi mevcut request geçmişini görmesini veya izin verilen request iptalini engellemez.
- Sanction sona erdiğinde veya revoke edildiğinde yeni booking request tekrar açılır.

## Güvenlik ve Veri Kuralları

- Sanction hedefi aktif platform `UserAccount` olarak Identity üzerinden doğrulanır.
- Sanction ve revocation nedeni zorunlu, 300 karakterle sınırlı ve auditlidir.
- Neden alanlarına PII, secret, token veya erişim bilgisi yazılmaz.
- IP/device sinyali tek başına kalıcı kapatma sebebi olamaz.
- Abuse event `DetailsJson` yalnızca step-up platform admin yüzeyinde gösterilir.

İtiraz ve kalıcı kapatma ayrıntıları `22-abuse-itiraz-hesap-kapatma.md` içinde tanımlıdır.

## Açık İşler

- İşletme raporlama davranışının kötüye kullanım riski
- IP/device sinyal toplama, saklama ve privacy kuralları
- Abuse dashboard pagination, reason-code taksonomisi ve operasyon runbook'u

İşletme abuse raporu, admin review, strike ve risk seviyesi akışı `21-isletme-abuse-raporu-strike-risk.md` içinde tanımlıdır.
