# Genel Incident Runbook

Bu runbook Phase 3 operasyon kapanışı için genel incident müdahale
çerçevesini tanımlar. Platform notification/closure özel runbook'u
`26-platform-operasyon-reconciliation-runbook.md` içinde kalır.

## Severity

| Seviye | Tanım | İlk aksiyon hedefi |
| --- | --- | --- |
| Critical | Veri kaybı, tenant izolasyon ihlali, auth bypass, closure/abuse geri döndürülemez hata | Hemen müdahale, güvenlik sahibi atanır |
| High | Booking tutarlılığı, ödeme dışı operasyon kesintisi, yaygın API hata oranı | 30 dakika içinde sahip atanır |
| Medium | Tek tenant/branch operasyon aksaması, degraded background job | Aynı iş günü içinde triage |
| Low | Dokümantasyon, gözlem veya lokal geliştirme sorunu | Planlı bakım akışına alınır |

## İlk Müdahale

1. Incident sahibini ve zamanını kaydet.
2. Etkilenen tenant, branch, user veya operation GUID'lerini PII olmadan not et.
3. `/health`, `/health/operations` ve ilgili admin snapshot'larını kontrol et.
4. Yeni deploy, migration veya config değişikliği olup olmadığını doğrula.
5. Geri döndürülemez mutasyon yapmadan önce ikinci kişi onayı al.

## Güvenlik İlkeleri

- Raw e-posta, telefon, token, OTP, password reset linki veya internal reason
  incident notlarına eklenmez.
- Tenant dışı veri görüntüleme şüphesi varsa ilgili endpoint hemen erişime
  kapatılır ve tenant isolation testleri çalıştırılır.
- Doğrudan DB düzeltmesi varsayılan olarak yasaktır; recovery mevcut
  idempotent API/application akışları üzerinden yapılır.
- Direct DB müdahalesi zorunluysa işlem öncesi backup alınır, iki kişi onayı ve
  postmortem maddesi zorunludur.

## Booking Operasyon Incident'leri

- Double-booking şüphesinde staff ve resource invariant'ları ayrı kontrol edilir.
- `PendingApproval` taleplerin slot bloklamadığı unutulmaz; yalnız `Confirmed`
  appointment çakışmaları capacity incident kabul edilir.
- Rebook, cancel, no-show veya complete komutunda hata varsa aynı
  `Idempotency-Key` ile replay denenmeden yeni komut üretilmez.
- Resource block incident'lerinde ilgili resource->branch yetki izi ve public
  slot etkisi beraber doğrulanır.

## Postmortem

Her High/Critical incident sonrasında:

- Kök neden
- Etkilenen kullanıcı/tenant sayısı
- Eksik alarm/test/runbook maddesi
- Önleyici aksiyon
- Sahip ve hedef tarih

kayıt altına alınır.
