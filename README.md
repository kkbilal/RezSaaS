# RezSaaS

Türkiye odaklı, çoklu işletme/şube/personel/kaynak (koltuk/oda/yatak/istasyon) destekleyen **salon operasyon + rezervasyon SaaS**.

## Dokümanlar

- `docs/README.md`: Dokümantasyon haritası ve okuma sırası
- `docs/00-kapsam-ozeti.md`: Ürün vizyonu, MVP sınırı ve doğrulanmış kararlar
- `docs/roadmap/README.md`: Faz bazlı yol haritası (Phase 0–5)
- `docs/01-mimari-ozet.md`: Mimari sınırlar, modüller ve veri sahipliği
- `docs/02-guvenlik-uyumluluk.md`: Güvenlik, KVKK, İYS ve operasyon minimumları
- `docs/03-gelir-modeli-odeme.md`: Gelir modeli ve ertelenmiş ödeme stratejisi
- `docs/04-rezervasyon-akisi.md`: İşletme onaylı rezervasyon durum makinesi
- `docs/05-domain-sozlugu.md`: Ürün dilinin tek anlamlı sözlüğü
- `docs/06-karar-kaydi.md`: Mimari ve ürün karar günlüğü
- `docs/07-yetki-matrisi.md`: Rol ve kapsam bazlı erişim taslağı
- `docs/08-bildirim-kanali-stratejisi.md`: E-posta, SMS ve WhatsApp kararı
- `docs/09-abuse-yaptirim-politikasi.md`: Slot spam tespiti ve kademeli yaptırım
- `docs/10-kalite-hedefleri.md`: Güvenlik, performans ve operasyon hedefleri
- `docs/11-veri-envanteri-taslagi.md`: KVKK odaklı veri sınıflandırma taslağı
- `docs/12-acik-sorular.md`: Uygulamaya geçmeden önce kapanacak sorular
- `docs/13-referanslar.md`: Resmi teknik ve uyumluluk referansları
- `docs/22-abuse-itiraz-hesap-kapatma.md`: Müşteri itirazı ve kalıcı hesap kapatma güvenlik akışı
- `docs/23-frontend-mimari-tasarim-kararlari.md`: Frontend mimari ve tasarım sistemi kararları
- `docs/24-frontend-uygulama-plani.md`: Frontend fazları ve uygulama sırası
- `docs/27-backup-restore-tatbikat-runbook.md`: Backup/restore tatbikat runbook'u
- `docs/28-genel-incident-runbook.md`: Genel incident runbook'u

## Not

Repo dokümantasyon-first başlatıldı. Phase 1, Phase 2 ve Phase 3 tamamlandı; Phase 4 ödeme/gelir optimizasyonu backend hazırlığı provider-agnostic ve default kapalı `Payments` modülüyle başladı.

## Geliştirme

Gereksinimler: `.NET SDK 10.0.300`, Docker Desktop ve Docker Compose.

```powershell
Copy-Item .env.example .env
# .env içindeki REZSAAS_POSTGRES_PASSWORD değerini değiştir.
docker compose up -d postgres
dotnet tool restore
dotnet restore RezSaaS.sln
dotnet tool run dotnet-ef database update --project src/Modules/RezSaaS.Modules.Identity --startup-project src/Apps/RezSaaS.Api --context IdentityDbContext
dotnet build RezSaaS.sln --no-restore
dotnet test RezSaaS.sln --no-build
dotnet run --project src/Apps/RezSaaS.Api
```

Development ortamında API ve entegrasyon testleri repo kökündeki ignored `.env`
dosyasını otomatik okur. Ortam değişkeniyle override etmek için
`ConnectionStrings__IdentityDatabase` kullanılabilir.

API healthcheck: `GET /health`
Development Swagger UI: `GET /swagger`

Ayrıntılı kurulum: `docs/14-gelistirici-kurulumu.md`

Visual Studio ile geliştirme için `RezSaaS.sln` dosyasını aç.
