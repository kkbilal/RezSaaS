# Phase 2 — Müşteri Keşif ve Rezervasyon MVP

## Amaç

Gerçek kullanıcı alabilecek ilk uçtan uca akışı üretmek: keşif → işletme profili → slot → doğrulama → rezervasyon.

## Kapsam

- Anonim keşif: şehir/ilçe, hizmet, salon türü, fiyat aralığı, puan ve uygun saat filtreleri
- İşletme profili: tanıtım, galeri, hizmet menüsü, çalışma saatleri, personel görünürlüğü, kurallar
- Slot bulma: hizmet(+variant), personel tercihi (opsiyonel), kaynak uygunluğu
- Rezervasyon oluşturma:
  - slot **hold** (geçici kilit) mantığı
  - login/register (rezervasyon için **hesap şart**)
  - **işletme onayı**: randevu isteği gönderilir, işletme onaylayınca kesinleşir
  - doğrulama başarısızsa hold release
- Yorumlar: yalnızca **tamamlanmış randevu** sonrası (verified review)

## Notlar (MVP kararları)

- `PendingApproval` durumunda slot **bloklanmaz**; işletme bir isteği seçer.
- Bu seçim modeli nedeniyle Phase 2’de minimum abuse kontrolleri gerekir (kullanıcı başına limitler, cooldown, işletme “spam” işaretleme).

## Kabul kriterleri (örnek)

- Hold süresi dolunca slot tekrar seçilebilir.
- Rezervasyon isteği `PendingApproval` iken işletme panelinde görünür ve onay/ret edilebilir.
- İşletme onayı olmadan randevu `Confirmed` duruma geçmez.
- İşletme bir isteği onaylayınca aynı slot için diğer `PendingApproval` istekleri otomatik kapanır (`Declined/Expired`).
- `PendingApproval` istekleri **24 saat** içinde yanıtlanmazsa `Expired` olur ve kullanıcı bilgilendirilir.
- Kullanıcı başına `PendingApproval` limitleri ve cooldown’lar uygulanır (abuse önleme).
- Tamamlanmamış randevulardan yorum yazılamaz.
