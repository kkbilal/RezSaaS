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
- `14-gelistirici-kurulumu.md`: Yerel geliştirme kurulumu
- `15-phase-1-uygulama-plani.md`: Phase 1 uygulama sırası ve mevcut durum
- `16-identity-auth-temeli.md`: Identity/Auth güvenlik kapısı ve mevcut uygulama
- `18-phase-2-uygulama-plani.md`: Phase 2 müşteri keşif ve rezervasyon uygulama planı
- `19-calisma-baglam-ozeti.md`: Uzun çalışma geçmişinin kompakt devralma özeti ve güncel faz durumu
- `20-admin-abuse-control-plane.md`: Abuse event inceleme, süreli sanction, revoke ve booking enforce sınırları
- `21-isletme-abuse-raporu-strike-risk.md`: İşletme abuse raporu, admin review, strike ve risk seviyesi sınırları
- `22-abuse-itiraz-hesap-kapatma.md`: Müşteri itirazı, iki-admin onaylı kalıcı kapatma ve Identity orchestration sınırları
- `23-frontend-mimari-tasarim-kararlari.md`: Frontend repo, runtime, güvenlik, API sözleşmesi ve tasarım sistemi kararları
- `24-frontend-uygulama-plani.md`: Backend fazlarına bağlı frontend `F0-F7` uygulama planı
- `25-platform-bildirim-outbox.md`: Platform-global transactional e-posta outbox'ı ve closure bildirim güvenliği
- `26-platform-operasyon-reconciliation-runbook.md`: Notification/closure alarm eşikleri ve güvenli manuel kurtarma adımları

## Bakım Kuralı

- Çekirdek ürün veya mimari kararı değişirse önce `06-karar-kaydi.md` güncellenir.
- Yeni terim eklendiğinde `05-domain-sozlugu.md` güncellenir.
- Yeni endpoint veya use-case eklendiğinde yetki, audit ve abuse etkisi kontrol edilir.
