# Bildirim Kanalı Stratejisi

## Karar

MVP'de:

- E-posta zorunlu transactional kanaldır.
- SMS altyapı olarak desteklenecek şekilde modellenir, ancak sağlayıcı seçimi ve aktif kullanım maliyet nedeniyle sonraki faza bırakılır.
- WhatsApp production zorunluluğu değildir; Messaging modülü sağlayıcı bağımsız tasarlanır ve WhatsApp sonraki fazda pilotlanır.

## Neden WhatsApp MVP Varsayılanı Değil?

WhatsApp Business Platform teknik olarak kullanılabilir; ancak mesaj template yönetimi, business onboarding, kanal politikaları, kullanıcı iletişim tercihi ve sağlayıcı operasyonu ister. SMS de maliyetli ve regülasyonlu bir kanaldır, fakat kritik fallback için daha geniş erişime sahiptir. MVP hedefi kanalları çoğaltmak değil, rezervasyon sürecinin güvenilirliğini ölçmektir.

## MVP Bildirim Matrisi

| Olay | E-posta | SMS | WhatsApp |
| --- | --- | --- | --- |
| E-posta doğrulama | Zorunlu | Hayır | Hayır |
| Rezervasyon isteği alındı | Zorunlu | Sonraki faz | Sonraki faz |
| Rezervasyon onaylandı | Zorunlu | Sonraki faz | Sonraki faz |
| Rezervasyon reddedildi/expired | Zorunlu | Sonraki faz | Sonraki faz |
| Hatırlatma | Zorunlu | Sonraki faz | Sonraki faz |
| Kampanya | MVP dışı | MVP dışı | MVP dışı |

## Mimari Gereksinimler

- `INotificationChannel` benzeri sağlayıcı bağımsız kontrat kullanılır.
- Mesajlar `TransactionalMessage` ve `CommercialMessage` olarak ayrılır.
- Template versiyonu, kanal, alıcı, gönderim zamanı, provider sonucu ve hata kodu kaydedilir.
- Retry idempotent olmalı; kalıcı hata ile geçici hata ayrılmalıdır.
- Telefon/e-posta loglarda maskelenir.

## Uyum Notu

- Ticari ileti sınıflandırması ve İYS yükümlülüğü hukuk danışmanıyla doğrulanır.
- SMS sağlayıcısı seçiminde gönderici adı, IP kısıtı ve güncel BTK gereksinimleri lansman öncesi teyit edilir.

## Referanslar

- İYS mevzuat sayfası: <https://iys.org.tr/mevzuat>
- Netgsm entegrasyon hazırlık rehberi: <https://www.netgsm.com.tr/dokumanlar/>
