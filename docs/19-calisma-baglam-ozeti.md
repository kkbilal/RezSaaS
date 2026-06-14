# Çalışma Bağlamı Özeti

Son güncelleme: 2026-06-14

Bu belge uzun çalışma geçmişinin kompakt devralma özetidir. Normatif ürün/mimari kararlar için ilgili ana dokümanlar ve ADR kayıtları geçerlidir.

## Değişmeyen Ürün Kararları

- Tek domain altında işletmelerin paylaşılabilir public sayfaları bulunur.
- Ürün multi-category ve multi-service çalışır; MVP'de hizmet süreleri tek blok olarak toplanır.
- Her kesinleşmiş randevu tam olarak `1 Staff + 1 Resource` ile planlanır.
- Rezervasyon isteği authenticated hesap gerektirir ve önce `PendingApproval` oluşur.
- `PendingApproval` slot bloklamaz; gerçek TTL üst sınırı 24 saattir.
- MVP'de ödeme alınmaz; e-posta kesin kanaldır, SMS/WhatsApp altyapısı sonraki fazlara bırakılmıştır.

## Mimari ve Güvenlik Sınırları

- Modüler monolith korunur; modüller birbirine doğrudan assembly veya tablo erişimiyle bağlanmaz.
- Tenant izolasyonu, explicit tenant scope, audit ve rate limit yeni endpoint kontrol listesinin parçasıdır.
- Global Identity rolleri yalnızca `PlatformAdmin` ve `PlatformSupport`; işletme rolleri tenant membership olarak tutulur.
- Kritik platform operasyonları `PlatformAdminWithStepUp` ister.
- Kaynak koda veya migration seed'ine kullanıcı, rol, parola, token ya da operasyon verisi gömülmez.

## Faz Durumu

- Phase 1 tamamlandı: solution/mimari temel, Identity/Auth, tenant temeli, güvenlik kapıları.
- Phase 2 tamamlandı: public keşif, slot bulma, request create, işletme onay/ret, müşteri self-service ve booking hardening.
- Phase 3 tamamlandı:
  - Tamamlandı: ilk `PlatformAdmin` bootstrap.
  - Tamamlandı: tenant provisioning.
  - Tamamlandı: tenant liste/detay ve membership add/suspend/revoke control-plane.
  - Tamamlandı: auditli ve row-lock korumalı tenant suspend/reactivate/close lifecycle ile erişim kapıları.
  - Tamamlandı: abuse event inceleme, süreli sanction apply/revoke ve yeni booking enforce başlangıcı.
  - Tamamlandı: işletme abuse işaretleme, step-up admin review, süreli/revoke edilebilir strike ve yalnızca öneri niteliğinde risk seviyesi.
  - Tamamlandı: müşteri self-service abuse itirazı, iki farklı step-up admin onaylı kalıcı hesap kapatma, Identity `Closed` orchestration'ı ve aktif hesap istek kapısı.
  - Tamamlandı: raw e-posta taşımayan platform-global closure/appeal e-posta outbox'ı, retry worker'ı ve sağlayıcı kabul zamanına bağlı itiraz penceresi.
  - Tamamlandı: salt-okunur notification/closure reconciliation, ayrı operasyon health yüzeyi, step-up admin snapshot'ı, PII-minimum alarmlar ve manuel kurtarma runbook'u.
  - Tamamlandı: business appointment calendar/detail, note, cancel, complete, no-show, rebook ve resource block operasyonları.
  - Tamamlandı: backup/restore tatbikat scriptleri, genel incident runbook ve CI güvenlik kapıları.
- Phase 4 başladı:
  - Tamamlandı: provider-agnostic `Payments` modülü, seed'siz persistence, hosted checkout only ilkesi ve raw payload saklamayan webhook idempotency temeli.
  - Tamamlandı: yalnız `PlatformAdminWithStepUp` korumalı read-only payment readiness endpoint'i.
  - Bekliyor: provider seçimi, hosted checkout adapter'ı, business/customer ödeme yüzeyleri, webhook signature doğrulaması ve refund/chargeback runbook'u.
- Phase 5 başladı:
  - Tamamlandı: default kapalı `Integrations` modülü tasarımı; API key/signing secret raw saklamayan, webhook payload hash'iyle çalışan persistence temeli.
  - Tamamlandı: yalnız `PlatformAdminWithStepUp` korumalı read-only integration readiness endpoint'i.
  - Bekliyor: business integration mutation'ları, public external API auth/scope enforcement, webhook delivery worker, CRM/export, İYS ve WhatsApp pilot adapter'ları.

## Frontend Durumu

- Frontend tam UI geliştirmesi henüz başlatılmadı; OpenAPI tabanlı API client iskeleti `src/Apps/RezSaaS.Web` altında başladı.
- Aynı repo içinde `src/Apps/RezSaaS.Web` altında tek Next.js web uygulamasıyla
  başlama kararı alındı; ayrı repo ve micro-frontend ilk fazda kullanılmayacak.
- Frontend mimari/tasarım kararları `23-frontend-mimari-tasarim-kararlari.md`,
  `F0-F7` uygulama sırası `24-frontend-uygulama-plani.md` içindedir.
- İlk kapı tamamlandı: OpenAPI artifact, session/bootstrap, business context, MFA step-up session, global customer history, business labels ve optional staff/internal resource kontratları uygulanmıştır.
- Sıradaki frontend adımı: `pnpm install`, gerçek generated API types, ardından F0/F1 route ve tasarım sistemi iskeleti.

## Çalışma Disiplini

- Kullanıcı commit izni verdi; push işlemlerini kullanıcı yapar.
- Bu çalışma öncesi son tamamlanan commit: `9345792 feat: add payments readiness foundation`.
- Her dilim sonunda solution build, tüm testler, doküman/ADR etkisi ve temiz git durumu doğrulanır.
