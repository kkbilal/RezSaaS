# Phase 3 - Güvenlik Sertleşmesi ve Operasyon

## Amaç

Phase 1-2 güvenlik minimumlarının üstüne sertleşme kontrolleri, geri dönüş planı ve salon operasyon derinliği eklemek.

## Kapsam

- Rate limiting: login/register/OTP send/verify endpoint’leri için global + endpoint bazlı limitler
- Abuse kuralları: device/IP/account sayaçları, cooldown, günlük deneme sınırları
- Rezervasyon abuse derinleşmesi: risk skoru, şüpheli paternlere kısıt ve inceleme ekranı
- Yaptırım sistemi: strike/ban merdiveni, audit log, appeal/itiraz akışı (`../09-abuse-yaptirim-politikasi.md`)
- Token/log hijyeni: OTP/plaintext yok, log masking, doğrulama URL/token loglanmaması
- Admin güvenliği: MFA (TOTP/passkey) ve kritik aksiyonlarda step-up auth
- Backup/restore testleri + incident runbook
- Dependency/secret/SAST tarama kapılarının sertleştirilmesi
- Operasyonel derinleşme:
  - iptal/no-show/rebook akışları
  - notlar
  - kaynak out-of-service
  - manuel takvim yönetimi

## Mevcut İlerleme

- Tamamlandı: token-hash kontrollü ve rate limited ilk `PlatformAdmin` bootstrap HTTP yüzeyi.
- Tamamlandı: `PlatformAdminWithStepUp` korumalı tenant provisioning endpoint'i.
- Tamamlandı: platform control-plane tenant liste/detay/membership okuma yüzeyleri.
- Tamamlandı: platform control-plane membership add/suspend/revoke komutları; aktif `UserAccount` doğrulaması, audit ve son aktif `BusinessOwner` koruması uygulanır.
- Tamamlandı: row-lock korumalı tenant suspend/reactivate/close lifecycle; suspended/closed tenant erişim kapıları.
- Tamamlandı: abuse event inceleme, süreli sanction apply/revoke ve yeni booking request enforce control-plane başlangıcı.
- Tamamlandı: branch-scope işletme abuse raporu, step-up admin review, süreli/revoke edilebilir strike ve otomatik sanction uygulamayan kullanıcı risk seviyesi.
- Tamamlandı: müşteri abuse itirazı, iki farklı step-up admin onaylı kalıcı hesap kapatma, Identity `Closed` orchestration'ı ve kapalı hesap aktif istek kapısı.
- Tamamlandı: platform-global closure/appeal e-posta teslimatı, bildirim zamanına bağlı itiraz penceresi, salt-okunur closure/notification reconciliation, operasyon health'i, PII-minimum alarm ve manuel kurtarma runbook'u.
- Tamamlandı: business appointment calendar/detail, internal note, cancel, complete, no-show ve rebook operasyonları; komutlar idempotent ve branch-scoped authz korumalıdır.
- Tamamlandı: resource out-of-service/block komutu resource->branch doğrulamasıyla açıldı ve public slot hesaplama resource block sinyalini kullanmaya devam eder.
- Tamamlandı: backup/restore tatbikat scriptleri, genel incident runbook, build/test/OpenAPI contract drift CI kapısı, secret scan, dependency audit ve CodeQL SAST workflow'ları.

## Kabul kriterleri (örnek)

- OTP gönderimi belirgin maliyet patlamasına karşı kısıtlıdır (kota/overage ve abuse limitleri).
- Tenant izolasyonu için temel güvenlik testleri otomasyonda koşar.
- Backup restore tatbikatı script/runbook ile doğrulanabilir; staging periyodu operasyon takvimine bağlanır.
- Kalıcı ban yalnızca manuel inceleme ile uygulanır.
