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

Browser istemcileri cookie auth tercih eder. Bearer token yalnızca gerekli kontrollü istemciler için kullanılır.

## MFA / Kod Politikası

- Normal müşteri login akışı her girişte tek kullanımlık kod istemez.
- MFA ayrıcalıklı hesaplar, yüksek riskli oturumlar ve kritik aksiyonlar için step-up olarak tasarlanır.
- Ayrıcalıklı MFA tamamlandığında kullanıcıyı yormamak için makul süreli güvenilir cihaz/oturum stratejisi belirlenir.
- Production admin veya işletme yönetim endpoint'leri bu step-up politikası tamamlanmadan yayınlanmaz.
- Kod tarafındaki policy hazırdır; enrollment ve güvenilir cihaz UX'i endpoint/UI açılışında tamamlanır.

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

## Migration

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/Modules/RezSaaS.Modules.Identity --startup-project src/Apps/RezSaaS.Api --context IdentityDbContext
```

## Auth Kapısı Açık İşleri

- Production SMTP sağlayıcısı seçimi ve secret yönetimi
- Confirmation/password reset teslimatının gerçek sağlayıcıyla uçtan uca testi
- Ayrıcalıklı hesap MFA enrollment ekranı ve güvenilir cihaz/oturum politikası
