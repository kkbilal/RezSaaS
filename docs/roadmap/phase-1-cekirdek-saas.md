# Phase 1 - Çekirdek SaaS Temeli

## Amaç

Ürünün iskeletini kurmak: tenancy, kimlik, yetkilendirme, temel domain nesneleri ve rezervasyon motorunun temel kural seti.

## Kapsam (modüller)

- Tenancy: tenant, business, branch yapısı (shared DB + tenant izolasyonu disiplini)
- Identity & AuthN/AuthZ: müşteri ve işletme kimliği, üyelik, role-based authorization, audit log omurgası
- Organizasyon: staff member, title, skill/capability
- Katalog: service, service variant (fiyat/variant/personel bazlı fiyat için temel model)
- Kaynaklar: resource type, resource, out-of-service/blocked time
- Zaman: working hours, leave/blocked time, availability rule
- Booking (temel): request/appointment ayrımı, işletme onayı, TTL expiry, çakışma engelleme

## Faz çıktıları

- Minimum yönetim paneli: işletme kurulumu + personel/hizmet/kaynak tanımlama + takvim görünümü
- Audit log + tenant sınırları için temel güvenlik testleri

## Mevcut Durum

- Tamamlandı: `.NET 10` solution, API composition root ve modül assembly iskeleti
- Tamamlandı: modülden modüle doğrudan referansı engelleyen mimari test
- Tamamlandı: PostgreSQL 18.4 yerel compose tanımı ve healthcheck
- Tamamlandı: Identity/Auth temeli, platform rolleri, migration ve auth entegrasyon testleri
- Sıradaki kapı: production e-posta sağlayıcısı + MFA enforcement + ilk admin bootstrap
- Sonraki dilim: Tenant context + Tenant Management + Organization temel veri modeli

## Kabul kriterleri (örnek)

- Aynı `staff + time range` ve aynı `resource + time range` çakışmaları iki ayrı DB garantisiyle engellenir.
- Tenant dışı kaynak erişimi, kaynak varlığını sızdırmayacak şekilde reddedilir (`404`); tenant içi yetersiz rol `403` döner.
- Eşzamanlı edit’ler için optimistic concurrency stratejisi tanımlıdır.
- Rezervasyon çekirdeği “1 staff + 1 resource” zorunluluğunu model seviyesinde enforce eder.
- Audit log rol değişimi, rezervasyon onayı/reddi ve ayar değişikliklerini kapsar.
- Tenant izolasyon entegrasyon testleri CI içinde çalışır.
