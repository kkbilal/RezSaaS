# `e2e-smoke.py` — Uctan uca duman testi

Urunun **cekirdek dongusunu** gercek bir API'ye karsi bastan sona kosturur. "Derleniyor" ile
"urun gercekten calisiyor" arasindaki farki kapatmak icin var.

```
platform admin (bootstrap + 2FA + step-up)
  -> owner kaydi -> tenant provisioning
  -> salon kurulumu (sube, calisma saati, kaynak tipi, kaynak, personel, yetkinlik,
     hizmet, varyant, gerekli yetkinlik)
  -> musteri kaydi -> public slot arama
  -> randevu TALEBI -> salon ONAYI -> RANDEVU DOGAR -> dogrulanir
```

Bagimlilik **yok**: yalnizca Python 3 standart kutuphanesi (`urllib`, `hmac`, `hashlib`,
`base64`). `pip install` gerektirmez. TOTP (RFC 6238) script icinde hesaplanir.

---

## 1. API'yi baslat

Duman testi API'yi **kendisi baslatmaz**; zaten kosan bir API'ye baglanir.

Bootstrap token'i ve SHA256'si (script'in varsayilani):

| | |
|---|---|
| token | `rezsaas-local-e2e-bootstrap-token` |
| SHA256 (hex) | `35284c677780ce63893f7a60415777802ffc5c66ffcc0ee90a021fa03c3d91fd` |

> Baska bir token kullanacaksan hash'ini su sekilde uret:
> `python scripts/e2e-smoke.py --bootstrap-token <token> --print-token-hash`

**PowerShell — API'yi baslatma komutu (tam):**

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5299"
$env:Identity__Bootstrap__PlatformAdminBootstrapTokenSha256 = "35284c677780ce63893f7a60415777802ffc5c66ffcc0ee90a021fa03c3d91fd"

# TEST ORTAMI KOLAYLIGI -- asagidaki "E-posta dogrulama tuzagi" basligini oku
$env:Identity__RequireConfirmedEmail = "false"
$env:Identity__DeliveryMode          = "DevelopmentSink"

dotnet run --project src/Apps/RezSaaS.Api
```

PostgreSQL kosuyor olmali (`docker compose up -d postgres`) ve `.env` dolu olmali.

---

## 2. Testi kostur

```powershell
python scripts/e2e-smoke.py --api-url http://localhost:5299
```

Her adim numarali ve net yazilir; basarisiz olursa HTTP kodu **ve yanit govdesi** gosterilir,
sonunda ozet tablo basilir, cikis kodu **1** olur.

```
[ 1/24] API erisilebilir mi (/health) ... OK (HTTP 200)
[ 6/24] Admin step-up (parola + TOTP) ... OK (isSatisfied=true, method=mfa)
[24/24] RANDEVUNUN DOGDUGUNU dogrulama ... OK (status=Confirmed ...)
```

Her kosu **benzersiz** e-posta/slug uretir (timestamp + uuid), yani tekrar tekrar
kosturulabilir.

### Yararli bayraklar

| Bayrak | Ne yapar |
|---|---|
| `--api-url` | API adresi (varsayilan `http://localhost:5252`) |
| `--bootstrap-token` | Bootstrap token'i (SHA256'si API ayarinda olmali) |
| `--print-token-hash` | Token'in SHA256 hex'ini yazar ve cikar |
| `--seed-business` | **Bilinen urun boslugu** icin harness kestirmesi — asagiya bak |
| `--admin-email` / `--admin-password` | Mevcut bir platform admin'i kullan |
| `--admin-totp-secret` | Admin'in TOTP anahtari (normalde otomatik saklanir) |

---

## 3. Ne KANITLIYOR

Test gectiginde su zincirin **gercekten** calistigi kanitlanmis olur:

1. `/health` ayakta
2. Platform admin bootstrap (`POST /api/admin/bootstrap/platform-admin`)
3. Cookie ile giris (`POST /api/auth/login?useCookies=true`)
4. 2FA anahtari alma + TOTP ile 2FA acma (`POST /api/auth/manage/2fa`)
5. **Step-up** (`POST /api/session/step-up`) — tum `/api/admin/*` uclari icin ZORUNLU
6. `GET /api/session/bootstrap` -> `stepUp.isSatisfied == true`
7. Owner kaydi (`POST /api/auth/register`)
8. Owner'in `userAccountId`'sini alma (`GET /api/session/bootstrap`)
9. **Tenant + owner provisioning** (`POST /api/admin/tenants`)
10. Sube (`POST /api/business/branches`) — `X-RezSaaS-Tenant` header'i ile
11. Calisma saatleri (7 gun) (`PUT .../working-hours/{dayOfWeek}`)
12. Kaynak tipi + kaynak
13. Personel + yetkinlik (skill) atamasi
14. Hizmet + varyant + varyanta **gerekli yetkinlik** baglama
15. Musteri kaydi + girisi
16. **Public slot arama** (`GET /api/public/businesses/{slug}/slots`)
17. **Randevu talebi** (`POST .../appointment-requests`, `Idempotency-Key` ile)
18. **Onay** (`POST /api/business/appointment-requests/{id}/approve`)
19. **Randevunun dogdugunun dogrulanmasi** (`GET /api/business/appointments`)
20. **Musteri kendi randevusunu goruyor** (`GET /api/customer/appointment-history`)
21. **[GUVENLIK] BASKA bir musteri o randevuyu iptal EDEMEZ** -> `404 APPOINTMENT_NOT_FOUND`
    (403 **degil**: 403 randevunun var oldugunu sizdirirdi) + randevu **bozulmamis** kalir
22. **[POLITIKA] Cutoff penceresinde iptal reddedilir** -> `409 APPOINTMENT_CANCEL_TOO_LATE`,
    govdede `cancellationCutoffHours` **dolu**
23. **Musteri KENDI randevusunu iptal eder** (`POST .../appointments/{id}/cancel`) -> `Cancelled`
24. **[IDEMPOTENT]** ayni iptal tekrar cagrilir -> yine 200, **mukerrer etki yok**
25. **Isletme takviminde de `Cancelled` gorunur**
26. **[REGRESYON]** `appointment-history?status=` filtresi randevulara uygulaniyor mu

Yani slot motorunun personel + kaynak + yetkinlik + calisma saati + zaman dilimi
eslestirmesinin **gercekten** slot urettigi, talebin randevuya donustugu ve musterinin o
randevuyu **yalnizca kendisinin** ve **yalnizca politika izin verdiginde** iptal edebildigi
kanitlanir.

**Iptal politikasi nasil test ediliyor (kurulum secimi):** randevu ~**3 gun (72 saat)**
ileride secilir. Adim 29'da isletmenin `cancellationCutoffHours` degeri **168 saate**
(7 gun = `Business.MaxCancellationCutoffHours`) cekilir -> randevu **kesinlikle** pencerenin
icine duser -> iptal reddedilmeli. Sonra deger **0**'a (kural yok) geri cekilip gercek iptal
yapilir. Boylece ne randevuyu gecmise cekmek ne de sunucu saatini oynatmak gerekir; kural
tamamen **urun akisi uzerinden** (`PATCH /api/business/settings/profile`) sinanir.

---

## 3.1 Bulunan hatalar (canli API'ye karsi kosuldu — **35/36 gecti**)

Test **gercek API'ye karsi kosuldu**. Cekirdek dongu (talep -> onay -> **RANDEVU** ->
**musteri iptali**) **calisiyor**: 39 slot dondu, talep olustu, onaylandi, randevu
`status=Confirmed` olarak dogdu, musteri onu iptal etti, iki taraf da `Cancelled` gordu.

**Hata 1 ve Hata 2 DUZELTILDI** (commit `f757cee` ve `adcbd96`); ilgili adimlar artik
birer **regresyon testi**. **Hata 3 ACIK** — adim 36 onu yakaliyor ve test `exit 1` doner.

### Hata 3 (ACIK) — `appointment-history?status=` randevu statuslerini **kabul etmiyor**

**Yer:** `src/Apps/RezSaaS.Api/Customer/CustomerAppointmentHistoryComposer.cs:47`

```csharp
if (!AppointmentRequestStatusFilter.IsValidOrEmpty(status))   // -> 400
```

Bu kapi `status`'u **`AppointmentRequestStatus`** enum'una gore dogruluyor
(`PendingApproval|Approved|Declined|Expired|Superseded|CancelledByCustomer`). Ama ayni
`status` degeri, `ConfirmedAppointmentQueryService.GetOwnAsync` icinde
(`src/Modules/RezSaaS.Modules.Booking/Application/ConfirmedAppointmentQueryService.cs:75`)
**`AppointmentStatus`** olarak parse ediliyor
(`Confirmed|Cancelled|Completed|NoShow|Rebooked`).

**Iki enum'un KESISIMI BOS.** Sonuc:

| `?status=` | Sonuc |
|---|---|
| `Confirmed`, `Cancelled`, `Completed`, `NoShow`, `Rebooked` | **400** `CUSTOMER_APPOINTMENT_HISTORY_INVALID_STATUS` — kapiyi gecemez |
| `PendingApproval`, `Approved`, `Declined`, `Expired`, `Superseded`, `CancelledByCustomer` | 200, ama randevu sorgusu bu degeri taniyamaz -> **fail-closed BOS liste** |

Yani **hangi degeri verirseniz verin, randevular gecmiste GORUNMEZ.** `c7c9245`'te eklenen
"status artik randevulara da uygulaniyor" duzeltmesi **ULASILAMAZ** durumda: filtreyi
gercekten calistiracak degerler 400'e takiliyor, 400'u gecen degerler ise randevu listesini
her zaman bosaltiyor.

**Yan etki:** `?status=Approved`'da, randevuya donusmus TALEP tekrar ortaya cikiyor
(`talep=1 ['Approved']`) — cunku "randevusu olan talebi gizle" dedup'i
(`CustomerAppointmentHistoryComposer.cs:93`) **randevu listesinden** besleniyor ve o liste
filtre altinda bos. Filtresiz cagride ayni talep dogru sekilde gizleniyor.

**Etkisi:** `/hesabim` sekmelerinde musteri **hicbir filtrede randevusunu goremez**;
"Gecmis randevularim" / "Iptal ettiklerim" gibi her sekme ya 400 ya da bos doner.

**Onerilen duzeltme:** ucun `status`'u **her iki enum'un birlesimine** gore dogrulamasi
(ya da talep/randevu icin ayri parametreler); iki sorgu servisi taninmayan degeri zaten
kendi icinde bos liste donerek ele aliyor.

### Hata 4 (ACIK, dusuk siddet) — iptal politikasi **geri okunamiyor** (write-only)

**Yer:** `src/Apps/RezSaaS.Api/Business/BusinessSettingsComposer.cs:127` (`ToResponse`)

`BusinessProfileSettingsView` **`CancellationCutoffHours`'u tasiyor**, ama
`BusinessProfileSettingsResponse` (`BusinessProfileSettingsRequest.cs` yanindaki record)
bu alani **icermiyor** — `ToResponse` onu haritalamiyor. Yani:

- `GET /api/business/settings/profile` iptal politikasini **dondurmuyor**.
- `PATCH .../profile` de guncellenmis degeri **dondurmuyor**.
- Alan `PATCH` ile **yazilabiliyor** ama **hicbir sekilde okunamiyor**.

Isletme "iptal politikam kac saat?" sorusunu API'den **yanitlayamaz**; `PATCH`'in
"davranisi PUT" oldugu bir ucta, istemcinin GET->PATCH turu (round-trip) yapmasi imkansiz.
(Alan nullable oldugu icin veri kaybi olmuyor — bu yuzden dusuk siddet.) Web formu
(`business-profile-settings-form.tsx`) alani hic bilmiyor: **UI'dan da ayarlanamiyor.**

> Bu yuzden adim 29-31 politikayi **davranissal** dogruluyor: `PATCH` sonrasi degeri geri
> okuyamadigimiz icin, 409 `APPOINTMENT_CANCEL_TOO_LATE` yanitindaki
> `cancellationCutoffHours=168` alanina bakiyoruz.

### Hata 1 (DUZELTILDI) — `Business` kaydini kimse olusturmuyor (LANSMAN BLOKAJI)

Detay icin bkz. bolum 4. Ozet: tenant acildiktan sonra owner **sube bile acamiyordu**
(`BUSINESS_NOT_FOUND`), cunku `Organization.Business` kaydini olusturan **hicbir uretim
kod yolu yoktu**. Artik `POST /api/admin/tenants` Business'i da olusturuyor; adim 11 bunun
regresyon testi.

### Hata 2 (DUZELTILDI) — `GET /api/business/appointments` parametresiz cagrida **500** veriyordu

**Yer:** `src/Apps/RezSaaS.Api/Business/BusinessAppointmentComposer.cs:78`

```csharp
DateTimeOffset rangeStartUtc = fromUtc ?? DateTimeOffset.UtcNow.Date;
```

`DateTimeOffset.UtcNow.Date` bir **`DateTime` (Kind=Unspecified)** dondurur. Bunun
`DateTimeOffset`'e ortuk donusumu **YEREL saat dilimi offset'ini** uygular — Istanbul'da
`+03:00`. Npgsql ise `timestamp with time zone` icin **offset'i 0 olmayan** bir
`DateTimeOffset` yazmayi **reddeder** -> sorgu patlar -> **HTTP 500**.

**Etkisi:** Sunucunun yerel saat dilimi UTC **degilse** (or. Turkiye, UTC+3),
`fromUtc` verilmeyen **her** randevu listeleme cagrisi 500 doner. Panelin randevu ekrani
`fromUtc` gondermiyorsa **tamamen kirik** demektir.

**Neden testlerden kaciyor:** CI makineleri genelde **UTC**'dir; orada offset 0 olur ve
sorun gorunmez. Yerel gelistirme (UTC+3) ve Turkiye'deki bir sunucuda patlar.

**Kanit (script'in kendisi gosteriyor):**

```
[24/25] RANDEVUNUN DOGDUGUNU dogrulama ... OK          <- acik UTC araligi (offset=0) verildi
        (status=Confirmed start=2026-07-15T06:00:00+00:00 staff=Ayse Usta kaynak=Koltuk 1)
[25/25] Regresyon: parametresiz GET /api/business/appointments ... FAIL (HTTP 500)
```

**Onerilen duzeltme:**

```csharp
DateTimeOffset rangeStartUtc = fromUtc ?? new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
```

> Kod tabaninda `UtcNow.Date` kalibinin **tek** ornegi budur (`grep -rn "UtcNow\.Date" src/`).

---

## 4. BULUNAN URUN BOSLUGU (LANSMAN BLOKAJI) — `--seed-business`

> **Bu, duman testinin ortaya cikardigi en onemli sey.**

`Organization` modulundeki **`Business` kaydini olusturan hicbir uretim kod yolu yok.**

- `Business.Create(...)` **yalnizca testlerden** cagriliyor
  (`tests/RezSaaS.IdentityIntegrationTests/...`, `tests/RezSaaS.Phase1CoreIntegrationTests/...`).
- Ne bir API ucu, ne bir seeder, ne de tenant provisioning bunu yapiyor:
  `CreateTenantWithOwnerService` sadece `TenantManagement` semasina Tenant + uyelik yazar.
- Ama `BranchManagementService.CreateAsync`, tenant icin **aktif bir `Business` ARIYOR**;
  bulamazsa **`BUSINESS_NOT_FOUND`** doner.
- Ayrica tum public uclar (`/api/public/businesses/{slug}/...`) `Business.Slug` uzerinden
  cozumleniyor.

**Sonuc:** Tenant acildiktan sonra isletme kurulumu **API uzerinden BASLAYAMIYOR**.
Owner sube bile acamiyor. Salon public'te hic gorunmuyor.

Bu, `--seed-business` **VERILMEDEN** kosuldugunda testin dustugu yerdir (dogru davranis budur):

```
[11/24] Organization Business kaydi ... SKIP (--seed-business verilmedi; ... BEKLENIR)
[12/24] Sube olusturma (owner) ... FAIL
      -> BUSINESS_NOT_FOUND -- URUN BOSLUGU DOGRULANDI: ...
```

### `--seed-business` ne yapar

`organization."Businesses"` tablosuna satiri **dogrudan Postgres'e** yazar
(`docker compose exec postgres psql`, `.env`'den kimlik okur).

- Bu bir **URUN AKISI DEGILDIR**; bir **test kosum araci (harness) kestirmesidir**.
- Urun kodunu **degistirmez** (yalnizca `scripts/` altinda dosya var).
- Amaci: bosluga ragmen zincirin **geri kalanini** (sube -> ... -> randevu) uctan uca
  dogrulayabilmek.
- **Bosluk kapandiginda bu bayrak SILINMELIDIR.**

```powershell
python scripts/e2e-smoke.py --api-url http://localhost:5299 --seed-business
```

**Yapilmasi gereken (urun tarafi):** tenant provisioning sirasinda (ya da owner'in
kullanacagi bir `POST /api/business` ucuyla) `Business` kaydinin olusturulmasi.

---

## 5. E-posta dogrulama tuzagi (TEST ORTAMI KOLAYLIGI)

`appsettings.json` (production varsayilani):

```json
"Identity": { "RequireConfirmedEmail": true, "DeliveryMode": "Unconfigured" }
```

Bu ikisi birlikte olunca: **musteri kaydolur ama e-postasini DOGRULAYAMAZ** -> giris
yapamaz -> randevu talebi gonderemez. Yani cekirdek dongu production ayarlariyla
**baslamadan olur**.

- `appsettings.Development.json` bunu zaten `RequireConfirmedEmail: false` +
  `DeliveryMode: "DevelopmentSink"` ile eziyor, bu yuzden `ASPNETCORE_ENVIRONMENT=Development`
  ile testler gecer.
- Yukaridaki baslatma komutunda `Identity__RequireConfirmedEmail=false` ve
  `Identity__DeliveryMode=DevelopmentSink` env'lerini yine de **acikca** veriyoruz ki
  Development disi bir ortamda kosulsa da davranis ayni olsun.

> **Bu bir TEST ORTAMI KOLAYLIGIDIR, urunun gercek akisi DEGILDIR.**
>
> **LANSMAN BLOKAJI:** Gercek hayatta bir **SMTP/e-posta saglayicisi** yapilandirilmadan
> `RequireConfirmedEmail: true` ile hicbir musteri hesabini dogrulayamaz. Yani e-posta
> saglayicisi **lansman oncesi zorunlu** bir istir. `DeliveryMode: "Unconfigured"` ile
> production'a cikmak, musteri kaydini tamamen kirar.

---

## 6. Tekrar tekrar kosmak

- Owner / musteri / tenant / sube her kosuda **benzersizdir** (timestamp + uuid) — sinirsiz
  tekrar kosturulabilir.
- **Platform admin** boyle degil: bootstrap **tek seferliktir** (2. cagri `409`) ve rate
  limit'i cimridir (**IP basina 15 dakikada 5 istek**).
  - Bu yuzden script **once giris yapmayi dener**, yalnizca admin yoksa bootstrap'a dokunur.
    Tekrarli kosular bootstrap kotasini **hic** harcamaz.
  - Admin'de 2FA acildiktan sonra `POST /api/auth/login` artik `401 RequiresTwoFactor` doner.
    Script bu yuzden admin'in TOTP `sharedKey`'ini ilk kosuda
    `artifacts/local/e2e-smoke-state.json` dosyasina yazar (bu klasor `.gitignore`'da) ve
    sonraki kosularda giris icin oradan okur.

**Sifirdan baslamak istersen** (platform admin'i silip bootstrap'i yeniden calistirmak):

```powershell
docker compose exec -T postgres psql -U rezsaas -d rezsaas -c @'
DELETE FROM identity."AspNetUserRoles" ur USING identity."AspNetUsers" u
  WHERE ur."UserId"=u."Id" AND u."Email"='e2e-smoke-admin@rezsaas.test';
DELETE FROM identity."AspNetUserTokens" t USING identity."AspNetUsers" u
  WHERE t."UserId"=u."Id" AND u."Email"='e2e-smoke-admin@rezsaas.test';
DELETE FROM identity."AspNetUsers" WHERE "Email"='e2e-smoke-admin@rezsaas.test';
'@
Remove-Item artifacts/local/e2e-smoke-state.json -ErrorAction SilentlyContinue
```

> Rate limit bellekte tutulur: `429` yersen API'yi yeniden baslatmak da limiti sifirlar.

---

## 7. Test dusunce nereye bakmali

| Belirti | Olasi sebep |
|---|---|
| `[12] BUSINESS_NOT_FOUND` | **Bilinen urun boslugu** (bkz. 4). `--seed-business` ile devam et. |
| `[21] 0 SLOT dondu` | O gun calisma saati kapali; personelde varyantin **gerekli yetkinligi** yok; subede **aktif kaynak** yok; `TimeZoneId` IANA formatinda degil (`Europe/Istanbul` olmali); varyantin `RequiredResourceTypeId`'si ile subedeki kaynagin tipi tutmuyor. |
| `[22] APPOINTMENT_REQUEST_TOO_SOON` | `Booking:Security:DefaultResponseBuffer` = 2 saat. Talep edilen baslangic `now + 2sa`'ten sonra olmali (script 3 gun sonrasini hedefler). |
| `HTTP 403` (govde bos) | `UnsafeRequestOriginMiddleware` **fail-closed**: her POST/PUT/PATCH/DELETE icin `Origin` (ya da `Referer`) header'i SART. Script `Origin`'i otomatik gonderir. |
| `HTTP 400 "Invalid tenant context header."` | `X-RezSaaS-Tenant` gecerli bir GUID degil. |
| `[6] STEP_UP_MFA_REQUIRED` | Ayricalikli hesapta (PlatformAdmin/Support) 2FA acilmadan step-up **reddedilir**. |
| `429` | Rate limit. Bootstrap 5/15dk; admin islemleri 60/dk; randevu talebi 12/dk. |

---

## 8. Bilinen sinirlar

- **Odeme / bildirim / e-posta yok.** Zincir yalnizca rezervasyon cekirdegini kapsar.
- **Iptal / erteleme / no-show / tamamlama akislari** test edilmiyor (uclar var, kapsam disi).
- `--seed-business` **gercek urun akisini atlar** — bu bilincli bir kestirmedir ve bosluk
  kapaninca kaldirilmalidir.
- Test, **tek** sube / **tek** personel / **tek** kaynak / **tek** varyant ile kosar.
  Cakisma (double-booking), kaynak yetersizligi, coklu varyant sureleri gibi senaryolar
  kapsam disidir.
- Randevu **onaylandiktan sonra** slot'un artik dondurulmedigini (busy-time) **dogrulamaz**.
- Platform admin **silinmeden** ikinci bir admin olusturulamaz (urun kisiti).
