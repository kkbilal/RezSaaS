# Çalışma Bağlamı Özeti

Son güncelleme: 2026-06-04

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
- Phase 3 devam ediyor:
  - Tamamlandı: ilk `PlatformAdmin` bootstrap.
  - Tamamlandı: tenant provisioning.
  - Tamamlandı: tenant liste/detay ve membership add/suspend/revoke control-plane.
  - Tamamlandı: auditli ve row-lock korumalı tenant suspend/reactivate/close lifecycle ile erişim kapıları.
  - Tamamlandı: abuse event inceleme, süreli sanction apply/revoke ve yeni booking enforce başlangıcı.
  - Tamamlandı: işletme abuse işaretleme, step-up admin review, süreli/revoke edilebilir strike ve yalnızca öneri niteliğinde risk seviyesi.
  - Tamamlandı: müşteri self-service abuse itirazı, iki farklı step-up admin onaylı kalıcı hesap kapatma, Identity `Closed` orchestration'ı ve aktif hesap istek kapısı.
  - Tamamlandı: raw e-posta taşımayan platform-global closure/appeal e-posta outbox'ı, retry worker'ı ve sağlayıcı kabul zamanına bağlı itiraz penceresi.
  - Sıradaki dilimler: notification/closure reconciliation ve alarm runbook'u; ardından backup/restore, incident runbook ve CI güvenlik kapıları.

## Çalışma Disiplini

- Kullanıcı commit izni verdi; push işlemlerini kullanıcı yapar.
- Bu çalışma öncesi son tamamlanan commit: `880f951 feat: add abuse appeals and account closure`.
- Her dilim sonunda solution build, tüm testler, doküman/ADR etkisi ve temiz git durumu doğrulanır.
