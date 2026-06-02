# Geliştirici Kurulumu

## Gereksinimler

- `.NET SDK 10.0.300`
- Docker Desktop
- Docker Compose

Repo kökündeki `global.json`, kullanılan SDK sürümünü sabitler. Yeni patch sürümüne geçiş bilinçli yapılır ve build doğrulanır.

## İlk Kurulum

Yerel environment dosyasını oluştur:

```powershell
Copy-Item .env.example .env
# .env içindeki REZSAAS_POSTGRES_PASSWORD değerini değiştir.
```

PostgreSQL container'ını başlat:

```powershell
docker compose up -d postgres
docker compose ps
```

Solution'ı doğrula:

```powershell
dotnet tool restore
dotnet restore RezSaaS.sln
dotnet tool run dotnet-ef database update --project src/Modules/RezSaaS.Modules.Identity --startup-project src/Apps/RezSaaS.Api --context IdentityDbContext
dotnet build RezSaaS.sln --no-restore
dotnet test RezSaaS.sln --no-build
```

Development ortamında API ve entegrasyon testleri repo kökündeki ignored `.env`
dosyasını otomatik okur. Ortam değişkeniyle override etmek için
`ConnectionStrings__IdentityDatabase` kullanılabilir.

API'yi çalıştır:

```powershell
dotnet run --project src/Apps/RezSaaS.Api
```

Healthcheck:

```text
GET /health
```

## Yerel PostgreSQL

- Image: `postgres:18.4-alpine3.23`
- Örnek local port: `5432`
- Örnek local database: `rezsaas`
- Örnek local kullanıcı: `rezsaas`

`.env.example` yalnızca şablondur. Parola repo dışında tutulan ignored `.env` dosyasında
değiştirilmelidir. Shared, staging veya production ortamlarında secret manager ve ayrı
kimlik bilgileri zorunludur.

`scripts/Import-LocalEnvironment.ps1`, `.env` değerlerini geçerli PowerShell sürecine
elle yüklemek gerektiğinde opsiyonel yardımcıdır; API çalıştırmak için zorunlu değildir.

## Solution Yapısı

```text
src/
  Apps/              API composition root
  BuildingBlocks/    Ortak teknik kontratlar
  Modules/           Domain modülleri
tests/
  RezSaaS.ArchitectureTests/
```

Her modül bağımsız assembly'dir. API host modülleri bir araya getirir; bir modül diğer modül assembly'sine doğrudan referans vermez.

Visual Studio üzerinde repo kökündeki `RezSaaS.sln` dosyasını aç.

## Identity Development Notu

- Local development `DevelopmentSink` e-posta modunu kullanır.
- Sink doğrulama token veya linklerini loglamaz.
- Production ortamında gerçek e-posta sağlayıcısı konfigüre edilmeden API fail-fast olur.

## Docker Desktop Notu

`docker compose up` komutu Docker engine kapalıyken çalışmaz. Windows üzerinde Docker Desktop başlatıldıktan sonra tekrar çalıştırılmalıdır.
