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

## Değişiklik Süreci

- Çekirdek bir karar değişecekse bu dosyaya yeni ADR satırı eklenir.
- Eski karar `Superseded` olarak işaretlenir ve yeni ADR kimliğine referans verir.
- Rezervasyon, tenancy ve kimlik kararları test planı güncellenmeden değiştirilemez.
