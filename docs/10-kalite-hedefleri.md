# Kalite Hedefleri

Bu hedefler ilk sürüm için doğrulanacak SLO/NFR taslağıdır. Kodlama sırasında test edilebilir hale getirilir.

## Güvenilirlik

- Rezervasyon onayı idempotent olmalıdır.
- Staff veya resource double-booking DB seviyesinde mümkün olmamalıdır.
- TTL expiry job tekrar çalıştığında aynı sonucu üretmelidir.
- Bildirim gönderimi başarısız olsa bile booking transaction geri alınmamalıdır; retry kuyruğu kullanılmalıdır.

## Güvenlik

- Tenant izolasyon testleri CI içinde çalışmalıdır.
- Secret, dependency ve temel statik analiz taraması CI kapısı olmalıdır.
- Audit log append-only tutulmalıdır.
- PII log masking test edilmelidir.

## Performans

- Slot sorguları branch, tarih aralığı, hizmet ve staff/resource filtreleriyle ölçülmelidir.
- Arama ve keşif sorguları booking transaction yolundan ayrıştırılmalıdır.
- Kesin hedefler pilot veriyle belirlenecek; ölçüm olmadan cache eklenmeyecektir.

## Operasyon

- Health check, structured logging, correlation id ve hata izleme ilk deploy'dan itibaren olmalıdır.
- Backup/restore prosedürü staging üzerinde periyodik denenmelidir.
- Migration'lar geri dönüş veya ileri düzeltme planıyla yayınlanmalıdır.

## Erişilebilirlik ve SEO

- Public işletme sayfaları indexlenebilir, paylaşılabilir ve SSR/SSG uyumlu olmalıdır.
- Formlar klavye ile kullanılabilir ve temel erişilebilirlik kontrolünden geçmelidir.
