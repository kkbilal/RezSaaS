# Identity ve Auth Temeli

## Amaç

Identity/Auth yüzeyi diğer domain API'lerinden önce tamamlanan zorunlu güvenlik kapısıdır.

## Uygulanan Temel

- ASP.NET Core Identity API endpoints
- PostgreSQL-backed EF Core Identity store (`identity` şeması)
- Platform-global `UserAccount`
- Hesap durumları: `Active`, `Suspended`, `Closed`
- Global platform rolleri: `PlatformAdmin`, `PlatformSupport`
- Platform policy'leri: `PlatformAdminOnly`, `PlatformSupportOrAdmin`
- Cookie ve bearer login desteği
- IP bazlı authentication rate limit: `10/dakika`, reddetme `429`
- Identity lockout: 5 başarısız giriş, 15 dakika
- Password minimum: 12 karakter, en az 4 farklı karakter
- Production confirmed e-posta zorunluluğu ve fail-fast konfigürasyon
- Local development için token/link loglamayan e-posta sink

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

Browser istemcileri cookie auth tercih eder. Bearer token yalnızca gerekli kontrollü istemciler için kullanılır.

## Environment Davranışı

- Development: `Identity:EmailDeliveryMode=DevelopmentSink`, `RequireConfirmedEmail=false`
- Production: `Identity:EmailDeliveryMode=Unconfigured`, `RequireConfirmedEmail=true`

Production e-posta sağlayıcısı bağlanmadan API başlangıçta hata verir. Bu kasıtlı güvenli varsayılandır.

## Migration

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/Modules/RezSaaS.Modules.Identity --startup-project src/Apps/RezSaaS.Api --context IdentityDbContext
```

## Auth Kapısı Açık İşleri

- Production e-posta sağlayıcısı seçimi ve implementasyonu
- Confirmation/password reset teslimatının gerçek sağlayıcıyla uçtan uca testi
- Ayrıcalıklı hesap MFA enrollment ve enforcement politikası
- İlk `PlatformAdmin` hesabı için auditli bootstrap prosedürü
