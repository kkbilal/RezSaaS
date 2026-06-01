# Phase 1 — Çekirdek SaaS Temeli

## Amaç

Ürünün iskeletini kurmak: tenancy, kimlik, yetkilendirme, temel domain nesneleri ve rezervasyon motorunun temel kural seti.

## Kapsam (modüller)

- Tenancy: tenant, business, branch yapısı (shared DB + tenant izolasyonu disiplini)
- Identity & AuthN/AuthZ: admin authentication, role-based authorization, audit log omurgası
- Organizasyon: staff member, title, skill/capability
- Katalog: service, service variant (fiyat/variant/personel bazlı fiyat için temel model)
- Kaynaklar: resource type, resource, out-of-service/blocked time
- Zaman: working hours, leave/blocked time, availability rule
- Booking (temel): appointment yaratma, çakışma engelleme (DB constraint + uygulama doğrulamaları)

## Faz çıktıları

- Minimum yönetim paneli: işletme kurulumu + personel/hizmet/kaynak tanımlama + takvim görünümü
- Audit log + tenant sınırları için temel güvenlik testleri

## Kabul kriterleri (örnek)

- Aynı `staff + resource + time range` için çakışan rezervasyon DB seviyesinde engellenir.
- Tenant dışı entity erişimleri `403` ile reddedilir.
- Eşzamanlı edit’ler için optimistic concurrency stratejisi tanımlıdır.
- Rezervasyon çekirdeği “1 staff + 1 resource” zorunluluğunu model seviyesinde enforce eder.
