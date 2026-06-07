# Identity ve Auth Temeli

## Amaç

Identity/Auth yüzeyi diğer domain API'lerinden önce tamamlanan zorunlu güvenlik kapısıdır.

## Uygulanan Temel

- ASP.NET Core Identity API endpoints
- PostgreSQL-backed EF Core Identity store (`identity` şeması)
- Platform-global `UserAccount`
- Hesap durumları: `Active`, `Suspended`, `Closed`
- Global platform rol kontratları: `PlatformAdmin`, `PlatformSupport`; rol kayıtları migration seed'i olarak gömülmez
- Platform policy'leri: `PlatformAdminOnly`, `PlatformSupportOrAdmin`
- Step-up policy: `PlatformAdminWithStepUp` (`PlatformAdmin` + `amr=mfa`)
- Cookie ve bearer login desteği
- IP bazlı authentication rate limit: `10/dakika`, reddetme `429`
- Identity lockout: 5 başarısız giriş, 15 dakika
- Password minimum: 12 karakter, en az 4 farklı karakter
- Production confirmed e-posta zorunluluğu ve fail-fast konfigürasyon
- Local development için token/link loglamayan e-posta sink
- Production için SMTP e-posta gönderici konfigürasyonu (`Identity:DeliveryMode=Smtp`)
- Token-hash kontrollü, auditli ilk `PlatformAdmin` bootstrap servisi
- Token-hash kontrollü, rate limited ilk `PlatformAdmin` HTTP bootstrap endpoint'i
- Hashlenmiş token saklayan, kısa ömürlü, httpOnly cookie tabanlı step-up session endpoint'i
- `PlatformAdminWithStepUp` için claim tabanlı test/entegrasyon sinyaline ek olarak DB-backed MFA step-up session doğrulaması
- Identity audit log tablosu

## Rol Ayrımı

- `PlatformAdmin`, `PlatformSupport`: global Identity rolleri
- `BusinessOwner`, `BranchManager`, `Staff`: tenant membership rolleri; sonraki dilimde
- `Customer`: ayrı bir Identity rolü değildir; aktif/doğrulanmış hesabın temel kullanım bağlamıdır

Bu ayrım tenant izolasyonunu korur. Tenant içi yetkiler global `AspNetRoles` içine taşınmaz.

## Endpoint Yüzeyi

Identity API endpoint'leri `/api/auth` altında map edilir:

- `POST /register`
- `POST /login`
- `POST /refresh`
- `GET/POST /confirmEmail`
- `POST /resendConfirmationEmail`
- `POST /forgotPassword`
- `POST /resetPassword`
- `POST /manage/2fa`
- `GET/POST /manage/info`

Admin bootstrap endpoint'i API composition root altında map edilir:

- `POST /api/admin/bootstrap/platform-admin`

Bu endpoint anonymous olabilir çünkü ilk admin henüz yoktur; güvenlik sınırı configured SHA-256 bootstrap token hash'i, auth rate limit, origin guard ve "zaten PlatformAdmin varsa reddet" davranışıdır.

Browser istemcileri cookie auth tercih eder. Bearer token yalnızca gerekli kontrollü istemciler için kullanılır.

## Session Bootstrap

Frontend, oturum ve rol bilgisini `GET /api/session/bootstrap` endpoint'inden alır.
Endpoint authenticated kullanıcı ister, tenant header kabul etmez ve session rate limit
policy'siyle korunur.

Response güvenli bootstrap bağlamını döndürür:

- aktif `UserAccount` kimliği, e-posta doğrulama durumu ve hesap durumu
- global platform rolleri (`PlatformAdmin`, `PlatformSupport`)
- mevcut step-up sinyali (`amr=mfa` varsa `isSatisfied=true`)
- kullanıcının aktif tenant membership listesi

Bu endpoint gerçek MFA enrollment veya güvenilir cihaz politikasını tamamlamaz; yalnızca
mevcut authentication claim'lerinden frontend bootstrap sinyali üretir.

## MFA / Kod Politikası

- Normal müşteri login akışı her girişte tek kullanımlık kod istemez.
- MFA ayrıcalıklı hesaplar, yüksek riskli oturumlar ve kritik aksiyonlar için step-up olarak kullanılır.
- `POST /api/session/step-up`, authenticated kullanıcıdan parola ve MFA kanıtı ister; privileged hesaplarda MFA/recovery-code olmadan step-up session üretmez.
- Başarılı step-up response'u token değerini dönmez; token yalnızca httpOnly cookie olarak yazılır ve DB'de SHA-256 hash'i saklanır.
- Varsayılan step-up süresi `Identity:StepUp:DurationMinutes` ile 30 dakikadır.
- `GET /api/session/bootstrap`, aktif step-up session varsa `stepUp.isSatisfied=true` ve expiry bilgisini döndürür.
- MFA enrollment UX'i frontend/platform yüzeyinde tamamlanacaktır; backend policy ve session enforcement hazırdır.

## Environment Davranışı

- Development: `Identity:DeliveryMode=DevelopmentSink`, `RequireConfirmedEmail=false`
- Production: `Identity:DeliveryMode=Smtp`, `RequireConfirmedEmail=true`

Parola ve connection string repoya yazılmaz. Local geliştirme değerleri ignored `.env`
dosyasından Development başlangıcında otomatik okunur. Gerçek ortamlar
`ConnectionStrings__IdentityDatabase` veya secret manager ile override eder.

Production e-posta sağlayıcısı bağlanmadan API başlangıçta hata verir. Bu kasıtlı güvenli varsayılandır. SMTP secret değerleri repoya yazılmaz.

## Platform Admin Bootstrap

İlk `PlatformAdmin` migration seed'iyle üretilmez. `IPlatformAdminBootstrapService`, yalnızca configured SHA-256 bootstrap token hash'i doğruysa ve henüz platform admin yoksa rol ve admin hesabı oluşturur.

- Konfigürasyon: `Identity:Bootstrap:PlatformAdminBootstrapTokenSha256`
- Audit action: `PlatformAdminBootstrapped`
- Bootstrap token, parola ve e-posta repo veya migration içine yazılmaz.
- HTTP endpoint sonucu token veya parola döndürmez; başarısız token durumunda detaylı secret bilgisi sızdırmaz.

## Migration

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/Modules/RezSaaS.Modules.Identity --startup-project src/Apps/RezSaaS.Api --context IdentityDbContext
```

## Auth Kapısı Açık İşleri

- Production SMTP sağlayıcısı seçimi ve secret yönetimi
- Confirmation/password reset teslimatının gerçek sağlayıcıyla uçtan uca testi
- Ayrıcalıklı hesap MFA enrollment ekranı ve trusted-device UX'i
