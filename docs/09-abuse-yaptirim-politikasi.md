# Abuse ve Yaptırım Politikası

## Amaç

Slot bloklamayan `PendingApproval` modelini kötüye kullanımdan korumak; yanlış pozitiflerde masum kullanıcıları kalıcı olarak cezalandırmamak.

## Riskler

- Aynı işletmeye kısa sürede çok sayıda talep açma
- Aynı zaman aralığı için birçok işletmeye talep gönderme
- Sürekli iptal/no-show ile işletmenin kapasitesini tüketme
- Hesap çoğaltma, IP rotasyonu veya otomasyon ile limit aşma

## Sinyaller

| Sinyal | Kullanım |
| --- | --- |
| Hesap yaşı ve doğrulama durumu | Risk skoru |
| Açık `PendingApproval` sayısı | Hard limit |
| Günlük/haftalık talep sayısı | Hard limit veya cooldown |
| Ret, expire, iptal ve no-show oranı | Risk skoru |
| Aynı işletme ve zaman aralığı yoğunluğu | Hard limit |
| IP/device paterni | Yardımcı sinyal; tek başına kalıcı ban sebebi değil |
| İşletme spam işaretlemesi | İnceleme sinyali; tek başına otomatik kalıcı ban değil |

## Yaptırım Merdiveni

| Seviye | Aksiyon | Tipik Süre |
| --- | --- | --- |
| 0 | Normal kullanım | Süresiz |
| 1 | Uyarı ve kısa cooldown | Dakika/saat |
| 2 | Talep limitini düşürme | 1-7 gün |
| 3 | Geçici rezervasyon yasağı | 24-72 saat |
| 4 | Uzun süreli askıya alma ve manuel inceleme | 7-30 gün |
| 5 | Kalıcı hesap kapatma | Manuel karar ve itiraz yolu |

## Güvenlik İlkeleri

- Otomatik sistem kalıcı hesap kapatmaz; yüksek etkili yaptırım manuel inceleme ister.
- Her strike ve yaptırım neden, sinyal, actor, zaman ve süre ile auditlenir.
- IP ban dar kapsamlı, süreli ve geri alınabilir olmalıdır.
- İtiraz süreci ve işletme spam işaretleme suistimali ayrıca izlenir.

## MVP Minimumları

- Kullanıcı başına eşzamanlı açık talep limiti
- Aynı işletmeye kısa süreli talep limiti
- Aynı zaman aralığında paralel talep limiti
- Cooldown ve temel risk skoru
- İşletme panelinde spam işaretleme
- Admin panelinde olay, strike ve yaptırım geçmişi

## Uygulanan Control-plane Başlangıcı

- Abuse event'leri user, tenant ve severity filtreleriyle `PlatformAdminWithStepUp` yüzeyinde listelenir.
- Kullanıcı bazında abuse event ve sanction geçmişi görüntülenir.
- `Warning` booking'i bloklamaz.
- `Cooldown` en fazla 24 saat, `TemporaryBan` 24–72 saat olarak uygulanır ve yeni booking request'i bloklar.
- Aynı kullanıcıya eşzamanlı birden fazla aktif bloklayıcı sanction uygulanmaz; apply işlemi advisory transaction lock ile korunur.
- Aktif sanction geçmiş kaydı silinmeden, neden ve actor ile auditli revoke edilir.
- Kalıcı hesap kapatma sanction endpoint'inden uygulanmaz; manuel Identity hesap kapatma ve appeal workflow'u tamamlanmalıdır.

## Uygulanan İşletme Raporu ve Strike Akışı

- Yetkili `BusinessOwner` veya branch-scoped `BranchManager`, belirli appointment request'i abuse şüphesiyle işaretleyebilir.
- Aynı tenant+appointment request için tek rapor tutulur; retry yeni rapor üretmez.
- İşletme raporu tek başına strike veya sanction üretmez.
- `PlatformAdminWithStepUp` raporu confirm veya dismiss eder; confirmed rapor tek ve süreli strike üretir.
- Strike revoke edilebilir; geçmiş kayıt ve audit korunur.
- Aktif strike sayısı platform-global kullanıcı risk seviyesini üretir ancak otomatik sanction tetiklemez.
- Raporlayan işletme actor'ü tenant başına kayan 24 saatlik limit ile korunur; limit aşımı ayrıca abuse event üretir.
