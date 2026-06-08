# Kalite Hedefleri

Bu hedefler ilk sürüm için doğrulanacak SLO/NFR taslağıdır. Kodlama sırasında test edilebilir hale getirilir.

## Güvenilirlik

- Rezervasyon onayı idempotent olmalıdır.
- Staff veya resource double-booking DB seviyesinde mümkün olmamalıdır.
- TTL expiry job tekrar çalıştığında aynı sonucu üretmelidir.
- Bildirim gönderimi başarısız olsa bile booking transaction geri alınmamalıdır; retry kuyruğu kullanılmalıdır.

## Güvenlik

- Tenant izolasyon testleri CI içinde çalışır (`.github/workflows/ci.yml`).
- Secret, dependency ve temel statik analiz taraması CI kapısıdır (`.github/workflows/security.yml`).
- Audit log append-only tutulmalıdır.
- PII log masking test edilmelidir.

## Performans

- Slot sorguları branch, tarih aralığı, hizmet ve staff/resource filtreleriyle ölçülmelidir.
- Arama ve keşif sorguları booking transaction yolundan ayrıştırılmalıdır.
- Kesin hedefler pilot veriyle belirlenecek; ölçüm olmadan cache eklenmeyecektir.

## Operasyon

- Health check, structured logging, correlation id ve hata izleme ilk deploy'dan itibaren olmalıdır.
- Backup/restore prosedürü `27-backup-restore-tatbikat-runbook.md` ve scriptlerle staging/local tatbikata bağlanır.
- Migration'lar geri dönüş veya ileri düzeltme planıyla yayınlanmalıdır.

## Erişilebilirlik ve SEO

- Public işletme sayfaları indexlenebilir, paylaşılabilir ve SSR/SSG uyumlu olmalıdır.
- Formlar klavye ile kullanılabilir ve temel erişilebilirlik kontrolünden geçmelidir.
- Frontend hedefi WCAG 2.2 AA'dır; otomatik axe kontrolleri manuel klavye ve
  yardımcı teknoloji incelemesinin yerine geçmez.
- Public sayfalarda 75. percentile hedefleri `LCP <= 2.5s`, `INP <= 200ms` ve
  `CLS <= 0.1` olarak ölçülür.
- Private customer/business/platform route'ları indexlenmez; public profil
  metadata, canonical, sitemap ve paylaşım görselleriyle doğrulanır.
