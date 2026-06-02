# Karar Kaydı

Bu dosya ürün ve mimari kararlarının kısa günlüğüdür. Değişiklikler gerekçesiyle birlikte eklenir; sessizce silinmez.

| ID | Durum | Karar | Gerekçe |
| --- | --- | --- | --- |
| ADR-001 | Kabul | Ürün tek domain altında keşif ve işletme sayfaları sunar | İşletmeler kendi sayfalarını paylaşırken merkezi keşif büyüme alanı sağlar |
| ADR-002 | Kabul | Multi-category domain dili kullanılır | Modelin berber/kuaför dışına genişlemesini korur |
| ADR-003 | Kabul | Backend modüler monolith olarak başlar | Rezervasyon transaction sınırını sade tutar ve erken operasyon yükünü azaltır |
| ADR-004 | Kabul | Shared DB + `tenant_id` izolasyonu kullanılır | İlk aşama maliyet/operasyon dengesi |
| ADR-005 | Kabul | Her MVP randevusu tam olarak `1 staff + 1 resource` kullanır | Fiziksel kapasite planlaması ürünün temel farklılaşmasıdır |
| ADR-006 | Kabul | `PendingApproval` slotu bloklamaz | Kötü niyetli slot bloklamayı önler |
| ADR-007 | Kabul | `PendingApproval` TTL üst sınırı 24 saattir | Gece gelen taleplerde işletmeye cevap zamanı tanır |
| ADR-008 | Kabul | MVP'de online ödeme yoktur | Booking çekirdeği ve operasyon akışı önce doğrulanır |
| ADR-009 | Kabul | MVP'de e-posta zorunlu, SMS sınırlı transactional kanal, WhatsApp sonraki faz pilotudur | WhatsApp entegrasyonu template/onboarding/politika yönetimi gerektirir; SMS kritik fallback olarak daha genel erişime sahiptir |
| ADR-010 | Kabul | Çakışma kuralı staff ve resource için ayrı DB garantileriyle uygulanır | Aynı staff farklı resource ile veya aynı resource farklı staff ile çakışmamalıdır |
| ADR-011 | Kabul | Backend SDK sürümü `.NET 10.0.300` olarak `global.json` ile sabitlenir | Güncel LTS çizgisinde tekrar üretilebilir build sağlar |
| ADR-012 | Kabul | API host composition root olur; her domain modülü ayrı assembly olarak yalnızca `BuildingBlocks` referansı alır | Modüler monolith sınırlarını derleme ve mimari test seviyesinde görünür kılar |
| ADR-013 | Kabul | Yerel geliştirme PostgreSQL `18.4-alpine3.23` container ile çalışır | PostgreSQL range/exclusion constraint hedefiyle uyumlu, tekrar üretilebilir geliştirme ortamı sağlar |
| ADR-014 | Kabul | Ortak analyzer ayarlarında warnings-as-errors aktiftir; NuGet sürümleri merkezi tutulur | Kod kalitesini başlangıçtan itibaren tutarlı tutar |
| ADR-015 | Kabul | Visual Studio geliştirmesi için klasik `RezSaaS.sln` ana solution dosyasıdır | Mevcut Visual Studio kurulumlarıyla doğrudan açılabilirlik sağlar |
| ADR-016 | Kabul | Auth temeli ASP.NET Core Identity API endpoint'leri ve PostgreSQL store ile kurulur | Olgun framework güvenlik davranışlarını kullanır; özel auth protokolü yazmayı önler |
| ADR-017 | Kabul | Browser istemcileri cookie auth tercih eder; bearer token kontrollü istemciler için açık tutulur | Browser token sızıntısı yüzeyini azaltır ve resmi Identity yaklaşımıyla uyumludur |
| ADR-018 | Kabul | Platform rolleri Identity içinde; tenant işletme rolleri tenant membership içinde tutulur | Global hesap ve tenant kapsamlı yetkilendirmeyi birbirine karıştırmaz |
| ADR-019 | Kabul | Production confirmed e-posta ister ve gerçek sağlayıcı olmadan fail-fast olur; development token loglamayan sink kullanır | Güvenli production varsayılanı ve yerel geliştirme akışını birlikte korur |
| ADR-020 | Kabul | Auth yüzeyi IP bazlı `10/dakika` rate limit, `429` cevabı ve Identity lockout ile korunur | Brute-force ve otomatik kaynak tüketimini ilk günden sınırlar |
| ADR-021 | Kabul | Değiştirilebilir çalışma verileri ve secret değerleri kaynak koda veya migration seed'ine gömülmez; platform rolleri auditli bootstrap ile üretilir | Ortamlar arası veri sızıntısını önler ve ayrıcalıklı yetki üretimini denetlenebilir tutar |
| ADR-022 | Kabul | Tenant Management kendi `tenant_management` schema'sında başlar; persistence/migration hazır olsa da yönetim endpoint'leri privileged auth kapısı kapanmadan yayınlanmaz | Tenant izolasyon temelini erken test ederken yetki, MFA ve bootstrap eksikleriyle yönetim yüzeyi açılmasını önler |
| ADR-023 | Kabul | Phase 1 modülleri kendi PostgreSQL schema'sı ve DbContext'i ile persistence temeli kurar; modülden modüle assembly referansı eklenmez | Modüler monolith sınırları korunurken gerçek DB invariant'ları erken test edilir |
| ADR-024 | Kabul | Tenant-scoped DbContext sorguları request-scope tenant context yoksa veri döndürmez | Tenant filtresinin unutulması yerine güvenli varsayılanla veri sızıntısı riski azaltılır |
| ADR-025 | Kabul | Confirmed booking çakışmaları PostgreSQL exclusion constraint ile staff ve resource için ayrı ayrı engellenir | Application kontrolü atlansa bile double-booking DB seviyesinde durdurulur |
| ADR-026 | Kabul | İlk `PlatformAdmin` hesabı SMTP/secret gibi dış ayarlardan bağımsız, token-hash kontrollü servisle ve audit kaydıyla oluşturulur | Migration seed'i olmadan denetlenebilir ve tekrar çalıştırılamayan bootstrap sağlar |

## Değişiklik Süreci

- Çekirdek bir karar değişecekse bu dosyaya yeni ADR satırı eklenir.
- Eski karar `Superseded` olarak işaretlenir ve yeni ADR kimliğine referans verir.
- Rezervasyon, tenancy ve kimlik kararları test planı güncellenmeden değiştirilemez.
