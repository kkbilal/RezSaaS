# Phase 2 - Müşteri Keşif ve Rezervasyon MVP

## Amaç

Gerçek kullanıcı alabilecek ilk uçtan uca akışı üretmek: keşif → işletme profili → slot → doğrulama → rezervasyon.

## Kapsam

- Anonim keşif: şehir/ilçe, hizmet, salon türü, fiyat aralığı, puan ve uygun saat filtreleri
- İşletme profili: tanıtım, galeri, hizmet menüsü, çalışma saatleri, personel görünürlüğü, kurallar
- Slot bulma: hizmet(+variant), personel tercihi (opsiyonel), kaynak uygunluğu
- Rezervasyon oluşturma:
  - login/register (rezervasyon için **hesap şart**)
  - **işletme onayı**: randevu isteği gönderilir, işletme onaylayınca kesinleşir
- Bildirim: zorunlu e-posta; SMS/WhatsApp altyapısı sonraki fazlara hazır kalacak, SMS sağlayıcı seçimi maliyet nedeniyle Phase 2 lansman kapısı olmayacak
- Yorumlar: yalnızca **tamamlanmış randevu** sonrası (verified review)

## Notlar (MVP kararları)

- `PendingApproval` durumunda slot **bloklanmaz**; işletme bir isteği seçer.
- Bu seçim modeli nedeniyle Phase 2’de minimum abuse kontrolleri gerekir (kullanıcı başına limitler, cooldown, işletme “spam” işaretleme).
- Public işletme URL yapısı `/isletme/{businessSlug}` olarak başlar ve `businessSlug` tek domain altında global benzersizdir.

## Kabul kriterleri (örnek)

- Rezervasyon isteği `PendingApproval` iken işletme panelinde görünür ve onay/ret edilebilir.
- İşletme onayı olmadan randevu `Confirmed` duruma geçmez.
- İşletme bir isteği onaylayınca artık karşılanamayan çakışan `PendingApproval` istekleri `Superseded` gerekçesiyle kapanır.
- `PendingApproval` istekleri **24 saat** içinde yanıtlanmazsa `Expired` olur ve kullanıcı bilgilendirilir.
- TTL randevu başlangıç zamanını aşamaz; `responseBuffer` kuralı uygulanır.
- Kullanıcı başına `PendingApproval` limitleri ve cooldown’lar uygulanır (abuse önleme).
- Tamamlanmamış randevulardan yorum yazılamaz.
