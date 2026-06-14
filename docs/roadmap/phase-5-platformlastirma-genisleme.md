# Phase 5 - Platformlaştırma ve Genişleme

## Amaç

RezSaaS’i gerçek bir “salon platformu”na taşımak: çoklu şube gelişmiş, entegrasyonlar ve büyüme araçları.

## Kapsam

- Çoklu şube karşılaştırma ve gelişmiş raporlama
- Gelişmiş yetki ağacı (rol + şube kapsamı + kritik aksiyon step-up)
- API/Webhook katmanı (entegrasyon + otomasyon)
- CRM/export entegrasyonları
- Kampanya/İYS izin yönetimi (transactional vs commercial ayrımı)
- WhatsApp Business Platform pilotu ve kanal tercihleri
- Resource capacity analytics
- Marketplace growth araçları (sponsored placement vb.) — discovery hacmi oluştuğunda
- Uluslararası açılım hazırlığı (locale/currency, provider abstraction, tenant/deployment taşıma politikası)

## Mevcut Başlangıç Dilimi

- `Integrations` modülü tenant-scoped persistence temeliyle açılır.
- External API ve webhook delivery varsayılan olarak kapalıdır.
- API key ve webhook signing secret raw saklanmaz; yalnız güvenli prefix/hash alanları tutulur.
- Webhook delivery raw payload saklamaz; payload hash, correlation id, event type ve teslimat durumu izlenir.
- İlk API yüzeyi yalnız `PlatformAdminWithStepUp` korumalı read-only `/api/admin/integrations/readiness` endpoint'idir.

## Henüz Yayınlanmayanlar

- Business self-service API client/webhook create/update/revoke mutation'ları.
- Public external API authentication ve scope enforcement.
- Webhook worker/delivery, retry ve signature gönderimi.
- CRM/export sağlayıcı adapter'ları, İYS ve WhatsApp pilot entegrasyonları.

