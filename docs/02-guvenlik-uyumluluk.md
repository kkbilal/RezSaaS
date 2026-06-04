# Güvenlik, Uyumluluk ve Operasyon (Taslak)

## Kimlik doğrulama ve abuse önleme

- Login/register/password reset yüzeyinde IP bazlı auth rate limit (`10/dakika`, reddetme `429`)
- Identity lockout: 5 başarısız giriş sonrası 15 dakika
- IP/account/device sayaçları, cooldown, retry-after
- OTP maliyetini kontrol için kota ve/veya kullanım bazlı ücret
- Müşteri hesabı için e-posta doğrulaması MVP zorunludur.
- Telefon doğrulaması, SMS sağlayıcısı ve maliyet/onboarding kararı kesinleştikten sonra kademeli açılır.
- Production e-posta sağlayıcısı olmadan API fail-fast olur; development sink token veya link loglamaz.

## Rezervasyon abuse (slot spam) ve yaptırımlar

MVP’de `PendingApproval` slotu bloklamadığı için kötüye kullanım riski vardır (aynı slot için çok sayıda istek açma, işletmeyi meşgul etme). Bu nedenle “önleme + tespit + kademeli yaptırım” birlikte tasarlanır.

Önleme (proaktif kontroller):

- Kullanıcı hesabı zorunlu (rezervasyon isteği için login şart).
- Kullanıcı başına eşzamanlı `PendingApproval` limitleri (ör. aynı gün içinde N adet).
- Aynı işletmeye kısa süre içinde açılabilecek istek sayısı limitleri.
- Şüpheli paternlere cooldown (ör. ardışık ret alan kullanıcı).
- Device/IP bazlı rate limiting (IP tek başına yeterli değil; NAT/CGNAT nedeniyle dikkatli).

Tespit (sinyaller):

- Kısa sürede çok sayıda `PendingApproval` açıp iptal/expiry oranı yüksek kullanıcılar.
- Çok sayıda işletmeye “aynı saat aralığı” için istek açan kullanıcılar.
- İşletmelerden gelen “spam/abuse” işaretlemeleri.

Yaptırım merdiveni (kademeli):

1) Uyarı + geçici cooldown (dakika/saat)  
2) Rezervasyon isteği oluşturma limiti düşürme (günlük/haftalık)  
3) Geçici hesap askıya alma (örn. 24–72 saat)  
4) Kalıcı hesap kapatma (appeal/itiraz süreci ile)  

Opsiyonel: IP ban yalnızca ağır abuse ve güvenilir sinyal varsa; aksi halde NAT/CGNAT nedeniyle masum kullanıcıları da etkileyebilir. IP tek başına kalıcı ban sebebi değildir.

Operasyon:

- İşletme panelinde “isteği spam olarak işaretle” aksiyonu
- Admin panelde abuse olayları, strike geçmişi, ban gerekçesi ve audit log
- Müşteri yalnızca kendi yaptırımına itiraz eder; internal karar nedenleri müşteri response'una eklenmez.
- Kalıcı hesap kapatma iki farklı step-up admin, itiraz penceresi, açık itiraz kontrolü ve platform rolü/aktif tenant membership engeli ister.
- Kapatılmış veya suspended hesabın eski cookie/bearer token ile authenticated isteği merkezi aktif hesap kapısında reddedilir.

## MFA ayrımı

- Müşteri tarafı: e-posta/telefon doğrulama **iletişim sahipliği doğrulaması** olarak konumlanır.
- İşletme/admin tarafı: kritik aksiyonlar için MFA (TOTP/passkey) ve step-up auth.
- Normal müşteri login akışı her girişte tek kullanımlık kod istemez.
- Ayrıcalıklı MFA tasarımında güvenilir cihaz/oturum stratejisi belirlenmeden her login'de kod dayatılmaz.
- Platform admin veya işletme yönetim endpoint'leri MFA enforcement tamamlanmadan yayınlanmaz.

## Log ve token hijyeni

- OTP kodları plaintext tutulmaz
- Log’larda e-posta/telefon maskelenir
- Doğrulama/reset token’ları URL/token olarak loglanmaz

## KVKK

- Veri envanteri + saklama süreleri
- Erişim matrisi
- Silme/anonymization akışları
- Backup şifreleme ve incident runbook
- İhlal bildirim süreçleri (72 saat çerçevesi için operasyon planı)
- Hassas müşteri notları serbest metin olarak sınırsız tutulmaz; minimum veri ilkesi uygulanır.

## İYS / mesajlaşma

- Transactional vs commercial mesaj ayrımı (mimari olarak)
- Pazarlama akışları için onay toplama ve İYS maliyetlerinin modele dahil edilmesi
- MVP kararı: e-posta zorunlu; SMS yalnızca sınırlı transactional kullanım için hazırlanır; WhatsApp sonraki faz pilotudur.

## PCI / ödeme güvenliği

- Kart verisini sistemden uzak tut (hosted/redirect)
- Redirect/checkout bütünlüğü ve unauthorized change monitoring

## Tenant izolasyonu ve kaynak gizliliği

- Platform yöneticisi dışındaki kullanıcılar yalnızca üyesi oldukları tenant kapsamına erişir.
- Tenant kapsamlı bir kaynağın kimliği tahmin edildiğinde dış kullanıcıya kaynağın varlığını doğrulamayacak yanıt tercih edilir (`404`).
- Yetkili tenant içinde rolü yetersiz kullanıcıya `403` dönülür.
- Background job, export ve admin araçları tenant kapsamını açıkça taşır ve auditlenir.

## Uygulama güvenliği minimumları

- State değiştiren endpoint'lerde CSRF stratejisi, güvenli cookie/token ayarları ve origin kontrolü mimari kararla sabitlenir.
- Input doğrulama, parametrik sorgu ve dosya yükleme sınırlamaları merkezi uygulanır.
- Dependency taraması, secret taraması ve temel SAST CI kapısı olarak planlanır.
- Audit log append-only yaklaşımıyla tutulur; uygulama kullanıcıları geçmiş audit kayıtlarını değiştiremez.
