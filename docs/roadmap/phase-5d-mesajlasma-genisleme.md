# Phase 5d - Mesajlaşma Genişleme (SMS, WhatsApp, İYS)

> Bu dosya, eski tek parça `phase-5-platformlastirma-genisleme.md`'nin
> parçalanmasıyla oluştu (bkz. ADR-068). MVP'de e-posta zorunlu, SMS sınırlı
> transactional, WhatsApp sonraki faz pilotu kararı (ADR-009/029) bu fazda
> açılır.

## Amaç

`Messaging` modülünün e-posta merkezli MVP temeli üzerine, maliyet ve onay
aşamaları kontrol altında tutularak: SMS sağlayıcı seçimi, WhatsApp Business
Platform pilotu ve İYS (İleti Yönetim Sistemi) ticari/transactional ayrımı ile
kanal tercihlerini eklemek.

## Kapsam

- SMS sağlayıcı seçimi ve sınırlandırılmış transactional SMS (onay bildirimi vb.)
- WhatsApp Business Platform pilotu (template onboarding + politik yönetim)
- İYS izin yönetimi: transactional vs commercial mesaj ayrımı
- Kanal tercihleri (müşteri ve işletme bazında)
- Maliyet kota/overage yönetimi ve fiyatlandırma hipotezine bağlama

## Backend teslimatları

- `Messaging` modülünde SMS sağlayıcı adapter'ı; transactional kanal sınırlı,
  maliyet eşiği ADR ile netleşir (`docs/12` "SMS sağlayıcısı ... maliyet eşiği").
- WhatsApp Business Platform pilot: template onboarding, opt-in/opt-out, politik
  yönetim; tüm mesajlar tenant-scoped outbox üzerinden (AGENTS.md §6.6).
- İYS izin yönetimi: commercial mesaj için İYS kaydı ve izin durumu; transactional
  ile commercial ayrımı zorunlu.
- Kanal tercihleri: müşteri (`Identity` global profile) ve işletme bazında; tercih
  olmayan kullanıcıya commercial mesaj gönderilmez.
- Maliyet kota/overage takibi; sayaçlar tenant-scoped, fiyatlandırma planına bağlı.
- Tüm bildirimler platform-global outbox kullanıyorsa yalnızca `UserAccountId`
  taşır, raw e-posta/telefon `Messaging` tablosuna yazılmaz (AGENTS.md §6.6, ADR-057).

## Frontend teslimatları

- Müşteri kanal tercihleri (`/hesabim/bildirim-tercihleri`): e-posta zorunlu,
  SMS/WhatsApp opsiyonel opt-in; İYS commercial izin yönetimi.
- İşletme kanal/bildirim tercihleri (`/panel/ayarlar/bildirimler`).
- WhatsApp/SMS tercih ekranı `docs/24` "Bilinçli Olarak Ertelenenler"den çıkar
  ve bu fazla açılır.

## Bağımlılıklar

- **Ön koşul faz:** Phase 3 (transactional outbox, platform bildirim worker'ı
  ADR-057/058 hazır). MVP e-posta akışı production SMTP ile doğrulanmış olmalı.
- **ADRs:** ADR-009 (bildirim kanalı stratejisi), ADR-029 (SMS sonraki faz),
  ADR-057 (platform-global outbox PII sınırı), ADR-068.
- **Açık sorular (blokaj):** `docs/12` Bildirim bölümü — "SMS sağlayıcısı ve
  gönderici adı ... hangi maliyet eşiği ve kategori pilotuyla açılacak?",
  "Production e-posta sağlayıcısı hangisi olacak?", "Telefon doğrulaması MVP
  lansman kapısı mı, yoksa kontrollü pilot özelliği mi?" — bu faz bu soruları
  yanıtlamadan başlayamaz.
- **Diğer fazlar:** Phase 5d, 5a/5b/5c'den bağımsız paralel başlayabilir.

## Kabul kriterleri

- SMS yalnızca sınırlandırılmış transactional amaçlı; commercial SMS için İYS izni
  zorunlu, izinsiz gönderim yapılamaz.
- WhatsApp yalnızca opt-in müşteriye ve onaylı template ile gönderilir.
- Kanal tercihini kapatan kullanıcıya o kanaldan mesaj gitmez.
- Tüm mesajlar tenant-scoped outbox + platform-global outbox PII kuralıyla uyumlu;
  raw telefon/e-posta `Messaging` tablosuna/log/response'a yazılmaz.
- Maliyet kota/overage raporu işletme ve platform için görülebilir.
- Explicit konfigürasyon olmadan SMS/WhatsApp sağlayıcıları aktif olmaz.

## Güvenlik / tenant minimumları

- Telefon doğrulaması OTP maliyeti için rate limit ve brute-force koruması
  (AGENTS.md §6.1/§6.2).
- İYS izin durumu silinmez, auditlenir; commercial mesaj öncesi doğrulanır.
- OTP/verification token/log linkleri loglanmaz; PII maskelenir (AGENTS.md §6.2).
- Tenant izolasyonu: tüm bildirim sorguları tenant filtreli (outbox global olsa
  bile alıcı resolution yalnız `UserAccountId` üzerinden).

## Mevcut durum

- Başlamadı. MVP'de e-posta zorunlu kanal; SMS sınırlı transactional altyapı
  hazır ama aktif sağlayıcı yok; WhatsApp sonraki faz pilotu (ADR-009/029).
- Platform-global transactional bildirim outbox mekanizması (ADR-057/058) ve
  platform notification worker hazır; bu faz bu temel üzerine SMS/WhatsApp/İYS
  katmanını ekler.