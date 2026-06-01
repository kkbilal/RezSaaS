# Phase 3 — Güvenlik Sertleşmesi ve Operasyon

## Amaç

Ürünü “demo”dan “yaşayan ürün”e çevirmek: abuse önleme, izlenebilirlik, geri dönüş planı ve salon operasyon derinliği.

## Kapsam

- Rate limiting: login/register/OTP send/verify endpoint’leri için global + endpoint bazlı limitler
- Abuse kuralları: device/IP/account sayaçları, cooldown, günlük deneme sınırları
- Rezervasyon abuse önleme: kullanıcı başına `PendingApproval` limitleri, şüpheli paternlere kısıt, işletme spam işaretleme
- Yaptırım sistemi: strike/ban merdiveni, audit log, appeal/itiraz akışı
- Token/log hijyeni: OTP/plaintext yok, log masking, doğrulama URL/token loglanmaması
- Admin güvenliği: MFA (TOTP/passkey) ve kritik aksiyonlarda step-up auth
- Backup/restore testleri + incident runbook
- Unauthorized change monitoring (özellikle ödeme yönlendirme / checkout entegrasyonu)
- Operasyonel derinleşme:
  - iptal/no-show/rebook akışları
  - notlar
  - kaynak out-of-service
  - manuel takvim yönetimi

## Kabul kriterleri (örnek)

- OTP gönderimi belirgin maliyet patlamasına karşı kısıtlıdır (kota/overage ve abuse limitleri).
- Tenant izolasyonu için temel güvenlik testleri otomasyonda koşar.
