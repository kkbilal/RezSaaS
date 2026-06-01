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
Set-ExecutionPolicy -Scope Process Bypass
. .\scripts\Import-LocalEnvironment.ps1
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

Her yeni terminal oturumunda API veya entegrasyon testlerinden önce
`Set-ExecutionPolicy -Scope Process Bypass` ve ardından
`. .\scripts\Import-LocalEnvironment.ps1` komutunu yeniden çalıştır.

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
