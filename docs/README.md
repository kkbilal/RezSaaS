# RezSaaS Dokümantasyon Haritası

## Okuma Sırası

1. `00-kapsam-ozeti.md`: Ürün ve MVP sınırı
2. `06-karar-kaydi.md`: Kabul edilen ürün/mimari kararları
3. `05-domain-sozlugu.md`: Terimler ve anlamları
4. `01-mimari-ozet.md`: Modüller, sahiplik ve teknik sınırlar
5. `04-rezervasyon-akisi.md`: Booking yaşam döngüsü
6. `02-guvenlik-uyumluluk.md`: Güvenlik ve uyumluluk minimumları
7. `roadmap/README.md`: Faz bazlı uygulama planı

## Destekleyici Belgeler

- `03-gelir-modeli-odeme.md`: SaaS-first gelir yaklaşımı ve ertelenmiş ödeme fazı
- `07-yetki-matrisi.md`: İlk rol ve kapsam matrisi
- `08-bildirim-kanali-stratejisi.md`: E-posta, SMS ve WhatsApp yaklaşımı
- `09-abuse-yaptirim-politikasi.md`: Abuse sinyalleri ve yaptırım merdiveni
- `10-kalite-hedefleri.md`: NFR/SLO başlangıç hedefleri
- `11-veri-envanteri-taslagi.md`: KVKK teknik veri envanteri başlangıcı
- `12-acik-sorular.md`: Phase 0 kapanış soruları
- `13-referanslar.md`: Resmi kaynaklar

## Bakım Kuralı

- Çekirdek ürün veya mimari kararı değişirse önce `06-karar-kaydi.md` güncellenir.
- Yeni terim eklendiğinde `05-domain-sozlugu.md` güncellenir.
- Yeni endpoint veya use-case eklendiğinde yetki, audit ve abuse etkisi kontrol edilir.
