# Phase 5 - Platformlaştırma ve Genişleme

> ⚠️ **SUPERSEDED (ADR-068, 2026-06-20):** Bu tek-parça faz dokümanı artık
> güncel değildir ve beş bağımsız alt faza ayrılmıştır:
> - `phase-5a-isletme-yonetim-crud.md` (settings CRUD + yetki ağacı)
> - `phase-5b-analytics-modulu.md` (Analytics modülü)
> - `phase-5c-acik-api-ve-webhook.md` (public API + webhook teslimatı)
> - `phase-5d-mesajlasma-genisleme.md` (SMS/WhatsApp/İYS)
> - `phase-5e-platform-buyume-ve-i18n.md` (marketplace + i18n — en yüksek riskli)
>
> Bu dosya yalnızca geçmiş referans için korunur. Yeni çalışma ve kabul
> kriterleri yukarıdaki alt faz dosyalarında yürütülür.

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
- API client ve webhook subscription lifecycle servisleri application katmanında hazırdır; config kapalıyken create işlemi çalışmaz, plaintext secret yalnız create sonucunda tek seferlik döner.
- Webhook delivery raw payload saklamaz; payload hash, correlation id, event type ve teslimat durumu izlenir.
- İlk API yüzeyi yalnız `PlatformAdminWithStepUp` korumalı read-only `/api/admin/integrations/readiness` endpoint'idir.

## Henüz Yayınlanmayanlar

- Business self-service API client/webhook create/update/revoke mutation'ları.
- Public external API authentication ve scope enforcement.
- Webhook worker/delivery, retry ve signature gönderimi.
- CRM/export sağlayıcı adapter'ları, İYS ve WhatsApp pilot entegrasyonları.

