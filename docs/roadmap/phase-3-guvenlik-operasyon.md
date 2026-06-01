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

## Kabul kriterleri (örnek)

- OTP gönderimi belirgin maliyet patlamasına karşı kısıtlıdır (kota/overage ve abuse limitleri).
- Tenant izolasyonu için temel güvenlik testleri otomasyonda koşar.
- Backup restore tatbikatı staging ortamında tamamlanmıştır.
- Kalıcı ban yalnızca manuel inceleme ile uygulanır.
